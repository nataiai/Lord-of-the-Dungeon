using System.Collections.Generic;
using UnityEngine;

// Flow field on a 0.5 grid (the same grid as Spawner): one BFS from the target, shared by every enemy chasing it,
// so many summons cost the same as one. Each enemy steps to the neighbour cell closest to the target -> walks around walls.
// Also Separation(): keeps enemies from stacking on each other.
public static class PathFinding
{
    public static bool use = true;            // ON/OFF: false = no pathfinding, enemies walk straight at the player
    public static bool useSeparation = true; // ON/OFF: false = no separation, enemies may stack

    public const float cell = 0.5f;
    public static float refresh = 0.25f;  // seconds a field is reused (the target moves)
    public static float wallRefresh = 2f; // seconds walls are cached; Invalidate() when a wall appears or goes
    public static int lookAhead = 4;      // cells ahead an enemy may aim at directly, so it walks smooth lines, not grid steps
    public static float clearance = 0.2f; // smallest gap kept from walls; bigger enemies keep their own size (radius)
    const int maxCells = 4000;            // BFS limit, for huge rooms
    static readonly Vector2Int[] dirs = {Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
                                         new(1,1), new(1,-1), new(-1,-1), new(-1,1)};
    static readonly Dictionary<Vector2Int,Dictionary<Vector2Int,int>> fields = new(); // target cell -> steps to it from each cell
    static readonly Dictionary<Vector2Int,bool> walls = new();
    static readonly Collider2D[] hits = new Collider2D[16];
    static readonly RaycastHit2D[] castHits = new RaycastHit2D[16];
    static ContactFilter2D solid; // no triggers, no Enemy layer (crowds would fill hits[] and hide the wall behind them)
    static bool solidSet;
    static float gridClearance; // the grid leaves room for the biggest enemy that asked so far
    static float warnedAt = -999;
    static float builtAt = -999, wallsAt = -999;
    static int dirtyFrame = -1;

    // call when walls change (crate broken, door opened, room switched). Applied next frame: Destroy() happens at the end of this one
    public static void Invalidate()=>dirtyFrame = Time.frameCount+1;

    public static Vector2Int ToCell(Vector2 p)=>new(Mathf.RoundToInt(p.x/cell),Mathf.RoundToInt(p.y/cell));
    public static Vector2 ToPos(Vector2Int c)=>(Vector2)c*cell;

    // the way to walk from `from` to reach `to`, inside bounds (normalized; straight line if there is no path)
    // halfSize: half the walker's collider (collider.bounds.extents), so it only takes ways its body fits through
    // stuck: it is pushing into something, so skip the straight line and follow the path
    public static Vector2 Direction(Vector2 from, Vector2 to, Bounds bounds, Vector2 halfSize=default, bool stuck=false){
        if(!use)return (to-from).normalized; // off: straight, like before pathfinding
        if(!solidSet){solid = new ContactFilter2D(); solid.SetLayerMask(~LayerMask.GetMask("Enemy")); solidSet = true;}
        float r = Mathf.Max(clearance, halfSize.x, halfSize.y); // grid: room for its width
        float corner = Mathf.Max(clearance, halfSize.magnitude); // straight line: a box's corners stick out farther
        if(r>gridClearance){gridClearance = r; walls.Clear(); fields.Clear();} // a bigger enemy: rebuild the grid with room for it
        if(Time.time>wallsAt+wallRefresh || (dirtyFrame>=0 && Time.frameCount>=dirtyFrame)){walls.Clear(); fields.Clear(); wallsAt=Time.time; dirtyFrame=-1;}
        if(Time.time>builtAt+refresh){fields.Clear(); builtAt=Time.time;}
        if(!stuck && IsClear(from,to,corner))return (to-from).normalized; // nothing in the way of its body: straight there
        var t = ToCell(to);
        if(!fields.TryGetValue(t,out var dist))fields[t] = dist = Build(t,bounds);
        var c = ToCell(from);
        var aim = Step(dist,c);
        if(aim==c){ // no path: straight, like before
            if(Application.isEditor && Time.time>warnedAt+3){warnedAt = Time.time;
                Debug.LogWarning($"PathFinding: no path from {from} to {to}. Select an enemy in play mode: red cells = blocked. All red = the room bounds or a floor collider is the problem.");}
            return (to-from).normalized;
        }
        for(int i=0;i<lookAhead;i++){ // aim at the farthest cell down the path that can be walked to straight
            var n = Step(dist,aim);
            if(n==aim || !IsClear(from,ToPos(n),corner))break;
            aim = n;
        }
        return (ToPos(aim)-from).normalized;
    }
    static Vector2Int Step(Dictionary<Vector2Int,int> dist, Vector2Int c){ // the neighbour one step closer to the target (c if none)
        var next = c;
        if(dist.TryGetValue(c,out int best)){
            foreach(var d in dirs)if(dist.TryGetValue(c+d,out int v)&&v<best){best=v;next=c+d;}
            return next;
        }
        float near = float.MaxValue; // not on the field (pressed against a wall): the closest field cell up to 2 cells away
        for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++){
            var n = c+new Vector2Int(x,y);
            float d = x*x+y*y;
            if(dist.TryGetValue(n,out int v) && (d<near || (d==near && v<best))){near=d; best=v; next=n;}
        }
        return next;
    }
    public static bool IsClear(Vector2 a, Vector2 b, float radius){ // a circle this big can slide from a to b without touching a wall
        Vector2 d = b-a;
        if(d.sqrMagnitude<1e-6f)return true;
        int n = Physics2D.CircleCast(a, radius, d.normalized, solid, castHits, d.magnitude);
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
        if(walls.TryGetValue(c,out bool w))return !w;
        Vector2 p = ToPos(c);
        w = !InRoom(bounds,p) || IsWall(p);
        walls[c] = w;
        return !w;
    }
    static bool InRoom(Bounds b, Vector2 p)=>p.x>=b.min.x && p.x<=b.max.x && p.y>=b.min.y && p.y<=b.max.y; // 2D: a tilemap's z must not matter
    static bool IsWall(Vector2 p){ // a solid collider that isn't someone walking around (chests count as walls)
        int n = Physics2D.OverlapCircle(p, gridClearance, solid, hits);
        for(int i=0;i<n;i++)if(IsWall(hits[i]))return true;
        return false;
    }
    static bool IsWall(Collider2D col)=>Slide.IsSolid(col); // same rule as sliding loot (AI/Slide.cs)

    public static void DrawGizmos(){ // Scene view: blocked cells in red
        Gizmos.color = new Color(1,0,0,.35f);
        foreach(var w in walls)if(w.Value)Gizmos.DrawCube(ToPos(w.Key),Vector3.one*cell*.9f);
    }

    // ---------- Separation ----------
    // Pushes `self` away from nearby colliders on `layers`, so characters don't stack. Add it to where it wants to walk:
    //     rb.velocity = Vector2.ClampMagnitude(way + PathFinding.Separation(col, 0.2f, LayerMask.GetMask("Enemy")), 1) * speed;
    // spacing: the gap to keep between collider edges. Each neighbour pushes 0 at `spacing` or farther, 1 when touching, up to 2 when overlapping.
    static readonly Collider2D[] near = new Collider2D[8];
    public static Vector2 Separation(Collider2D self, float spacing, int layers){
        if(!useSeparation)return Vector2.zero;
        Vector2 push = Vector2.zero;
        Bounds me = self.bounds;
        float searchRadius = me.extents.magnitude + spacing;
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
