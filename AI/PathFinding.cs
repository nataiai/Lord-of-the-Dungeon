using System.Collections.Generic;
using UnityEngine;

// Flow field on a 0.5 grid (the same grid as Spawner): one BFS from the target, shared by every enemy chasing it,
// so many summons cost the same as one. Each enemy steps to the neighbour cell closest to the target -> walks around walls.
// Also Separation(): keeps enemies from stacking on each other.
public static class PathFinding
{

    public const float cell = 0.5f;
    public static float refresh = 0.25f;  // seconds a field is reused (the target moves)
    public static float wallRefresh = 2f; // seconds walls are cached; Invalidate() when a wall appears or goes
    public static int lookAhead = 4;      // cells ahead an enemy may aim at directly, so it walks smooth lines, not grid steps
    static float margin => Experimental.I.wallSpacing; // space kept from walls when the room allows it; tight spots use the exact body
    public static float minBody = 0.4f;   // smallest body size checked, so tiny enemies still keep off walls a bit
    const float skin = 0.05f;             // the body is checked this much smaller, so one resting against a wall isn't "inside" it
    const int maxCells = 4000;            // BFS limit, for huge rooms
    static readonly Vector2Int[] dirs = {Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
                                         new(1,1), new(1,-1), new(-1,-1), new(-1,1)};
    class Grid{ // what one body size can walk
        public Vector2 body;
        public readonly Dictionary<Vector2Int,Dictionary<Vector2Int,int>> fields = new(); // target cell -> steps to it from each cell
        public readonly Dictionary<Vector2Int,bool> walls = new();
    }
    static readonly Dictionary<Vector2Int,Grid> grids = new(); // one per body size (0.1 steps): each enemy is checked with its own width and height
    static Grid g; // the grid of the walker asking now
    static readonly Collider2D[] hits = new Collider2D[16];
    static readonly RaycastHit2D[] castHits = new RaycastHit2D[16];
    static ContactFilter2D solid; // no Enemy layer (crowds would fill hits[] and hide the wall behind them). Triggers too: holes are triggers
    static bool solidSet;
    static float builtAt = -999, wallsAt = -999;
    static int dirtyFrame = -1;

    // call when walls change (crate broken, door opened, room switched). Applied next frame: Destroy() happens at the end of this one
    public static void Invalidate()=>dirtyFrame = Time.frameCount+1;

    public static Vector2Int ToCell(Vector2 p)=>new(Mathf.RoundToInt(p.x/cell),Mathf.RoundToInt(p.y/cell));
    public static Vector2 ToPos(Vector2Int c)=>(Vector2)c*cell;
    static Grid GridFor(Vector2 body){
        body = Vector2.Max(body - Vector2.one*skin, Vector2.one*minBody);
        var key = new Vector2Int(Mathf.RoundToInt(body.x*10), Mathf.RoundToInt(body.y*10));
        if(!grids.TryGetValue(key, out var grid))grids[key] = grid = new Grid{body = (Vector2)key/10f};
        return grid;
    }

    // the way to walk from `from` to reach `to`, inside bounds (normalized; straight line if there is no path)
    // walker: its collider. Its size: only ways its body fits through, tall or wide. Its contacts: walks along a wall it touches, not into it
    public static Vector2 Direction(Vector2 from, Vector2 to, Bounds bounds, Collider2D walker){
        if(!Experimental.I.pathFinding)return (to-from).normalized; // off: straight, like before pathfinding
        if(!solidSet){solid = new ContactFilter2D(); solid.SetLayerMask(~LayerMask.GetMask("Enemy")); solid.useTriggers = true; solidSet = true;}
        if(Time.time>wallsAt+wallRefresh || (dirtyFrame>=0 && Time.frameCount>=dirtyFrame)){foreach(var x in grids.Values){x.walls.Clear(); x.fields.Clear();} wallsAt=Time.time; dirtyFrame=-1;}
        if(Time.time>builtAt+refresh){foreach(var x in grids.Values)x.fields.Clear(); builtAt=Time.time;}
        Vector2 body = walker.bounds.size;
        int seed = walker.GetInstanceID(); // so stuck walkers don't all turn the same way
        var way = Way(from,to,bounds,body+Vector2.one*2*margin) // keeping space from walls
               ?? Way(from,to,bounds,body)                       // too tight: just the body
               ?? Steer(from,to,seed);                           // no path at all: turn until free
        return AlongWall(walker,way,seed);
    }
    static readonly ContactPoint2D[] touching = new ContactPoint2D[8];
    static Vector2 AlongWall(Collider2D walker, Vector2 way, int seed){ // pushing into a wall/rock it touches: walk along it instead, toward the side it leans
        int n = walker.GetContacts(touching);
        for(int i=0;i<n;i++){
            var p = touching[i];
            if(!IsWall(p.collider==walker ? p.otherCollider : p.collider))continue; // other characters don't count
            Vector2 away = Vector2.Dot(p.normal,(Vector2)walker.bounds.center-p.point)<0 ? -p.normal : p.normal;
            if(Vector2.Dot(way,away)>=-.1f)continue; // not pushing into it
            Vector2 along = Vector2.Perpendicular(away);
            float lean = Vector2.Dot(way,along);
            return (Mathf.Abs(lean)>.1f ? Mathf.Sign(lean) : (seed&1)==0 ? 1 : -1) * along;
        }
        return way;
    }
    static Vector2? Way(Vector2 from, Vector2 to, Bounds bounds, Vector2 body){ // null if this body size has no path
        g = GridFor(body);
        if(IsClear(from,to,g.body))return (to-from).normalized; // nothing in the way of its body: straight there
        var t = ToCell(to);
        if(!g.fields.TryGetValue(t,out var dist))g.fields[t] = dist = Build(t,bounds);
        var c = ToCell(from);
        var aim = Step(dist,c);
        if(aim==c)return null;
        for(int i=0;i<lookAhead;i++){ // aim at the farthest cell down the path that can be walked to straight
            var n = Step(dist,aim);
            if(n==aim || !IsClear(from,ToPos(n),g.body))break;
            aim = n;
        }
        return (ToPos(aim)-from).normalized;
    }
    static Vector2 Steer(Vector2 from, Vector2 to, int seed){ // no path found: turn left/right from straight, 30 degrees at a time, until a short step is free
        Vector2 d = (to-from).normalized;
        float side = ((seed + (int)(Time.time/2)) & 1)==0 ? 1 : -1; // first side differs per walker and flips every 2s, so a stuck one tries the other way
        for(int a=30;a<=180;a+=30)foreach(float s in new[]{side,-side}){
            Vector2 v = Quaternion.Euler(0,0,a*s)*d;
            if(IsClear(from, from+v*cell*2, g.body))return v;
        }
        return d;
    }
    static Vector2Int Step(Dictionary<Vector2Int,int> dist, Vector2Int c){ // the neighbour one step closer to the target (c if none)
        int best = dist.TryGetValue(c,out int d0)?d0:int.MaxValue; // not on the field: hugging a wall, so step back onto it
        var next = c;
        foreach(var d in dirs)if(dist.TryGetValue(c+d,out int v)&&v<best){best=v;next=c+d;}
        return next;
    }
    public static bool IsClear(Vector2 a, Vector2 b, Vector2 body){ // a box this big can slide from a to b without touching a wall
        Vector2 d = b-a;
        if(d.sqrMagnitude<1e-6f)return true;
        int n = Physics2D.BoxCast(a, body, 0, d.normalized, solid, castHits, d.magnitude);
        for(int i=0;i<n;i++)if(IsWall(castHits[i].collider))return false;
        return true;
    }

    static Dictionary<Vector2Int,int> Build(Vector2Int target, Bounds bounds){ // BFS from the target
        var dist = new Dictionary<Vector2Int,int>{{target,0}};
        var q = new Queue<Vector2Int>(); q.Enqueue(target);
        while(q.Count>0 && dist.Count<maxCells){
            var c = q.Dequeue();
            foreach(var d in dirs){
                var n = c+d;
                if(dist.ContainsKey(n) || !Walkable(n,bounds))continue;
                if(d.x!=0 && d.y!=0 && (!Walkable(new(n.x,c.y),bounds) || !Walkable(new(c.x,n.y),bounds)))continue; // no cutting corners
                dist[n] = dist[c]+1;
                q.Enqueue(n);
            }
        }
        return dist;
    }
    static bool Walkable(Vector2Int c, Bounds bounds){
        if(g.walls.TryGetValue(c,out bool w))return !w;
        Vector2 p = ToPos(c);
        w = !bounds.Contains(p) || IsWall(p);
        g.walls[c] = w;
        return !w;
    }
    static bool IsWall(Vector2 p){ // the body standing here touches a solid collider that isn't someone walking around (chests count as walls), or a hole
        int n = Physics2D.OverlapBox(p, g.body, 0, solid, hits);
        for(int i=0;i<n;i++)if(IsWall(hits[i]))return true;
        return false;
    }
    static bool IsWall(Collider2D col){
        if(col.isTrigger)return col.GetComponent<Hole>(); // walkers go around holes; other triggers (doors, loot, hitboxes) are open. Flyers don't pathfind
        var ch = col.GetComponentInParent<Character>(); return ch==null || ch is Chest;
    }

    public static void DrawGizmos(Collider2D walker){ // Scene view: cells blocked for this walker's size (with the margin) in red
        if(!walker)return;
        Gizmos.color = new Color(1,0,0,.35f);
        foreach(var w in GridFor((Vector2)walker.bounds.size+Vector2.one*2*margin).walls)if(w.Value)Gizmos.DrawCube(ToPos(w.Key),Vector3.one*cell*.9f);
    }

    // ---------- Separation ----------
    // Pushes `self` away from nearby colliders on `layers`, so characters don't stack. Add it to where it wants to walk:
    //     rb.velocity = Vector2.ClampMagnitude(way + PathFinding.Separation(col, LayerMask.GetMask("Enemy")), 1) * speed;
    // spacing: the gap kept between collider edges, the radius of the circle around the collider: 1/√2 for a 1x1 box (bigger bodies keep more room).
    // Each neighbour pushes 0 at `spacing` or farther, 1 when touching, up to 2 when overlapping.
    static readonly Collider2D[] near = new Collider2D[8];
    public static Vector2 Separation(Collider2D self, int layers){
        if(!Experimental.I.separation)return Vector2.zero;
        Vector2 push = Vector2.zero;
        Bounds me = self.bounds;
        float spacing = me.extents.magnitude * Experimental.I.enemySpacing; // half the diagonal, times the Experimental setting
        float searchRadius = 2*spacing; // its own radius + the gap
        int count = Physics2D.OverlapCircleNonAlloc(me.center, searchRadius, near, layers);

        for(int i=0;i<count;i++){
            Collider2D other = near[i];
            if(other == self || (self.attachedRigidbody != null && other.attachedRigidbody == self.attachedRigidbody)) continue; // itself

            float gap = self.Distance(other).distance; // edge to edge; negative = overlapping
            if(gap >= spacing) continue; // far enough

            Vector2 away = me.center - other.bounds.center;
            if(away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle; // exactly on top: split any way

            float strength = Mathf.Min((spacing - gap) / spacing, 2);
            push += away.normalized * strength;
        }
        return push;
    }
}
