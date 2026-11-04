using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Spawner : MonoBehaviour
{
    public TilemapRenderer tilemapRenderer;

    // public SpriteRenderer sr;
    public Bounds bounds;


    // public Vector3Int[,] spots;
    // public HashSet<Vector2> walkable = new();
    // List<Vector2> walkableL;
    // // Start is called before the first frame update
    void Awake()
    {
        // if(sr==null)TryGetComponent<SpriteRenderer>(out sr);
        // if(sr==null)return; // wrong obj
        // bounds = sr.bounds;
        // sr.enabled = false;
        Debug.Log($"Parent is: {transform.parent.name}", transform.parent);
        GetComponent<Tilemap>().CompressBounds();
        if (tilemapRenderer==null)tilemapRenderer = GetComponent<TilemapRenderer>();
        bounds = tilemapRenderer.bounds;
        
        // https://discussions.unity.com/t/getting-and-storing-tilemap-positions-into-a-2d-array/732079
        // updateWalkable();
    }
    
    // void updateWalkable(){

    //     walkable.Clear();

    //     var tilemap = GetComponent<Tilemap>();
    //     var bounds = tilemap.cellBounds;
    //     spots = new Vector3Int[bounds.size.x, bounds.size.y];
    //     for (int x = 0; x < bounds.size.x; x++)
    //     {
    //         for (int y = 0; y < bounds.size.y; y++)
    //         {
    //             var px = bounds.xMin + x;
    //             var py = bounds.yMin + y;
    //             if (tilemap.HasTile(new Vector3Int(px, py, 0)))
    //             {
    //                 spots[x, y] = new Vector3Int(px, py, 0);
    //                 var vtmp = new Vector2(px,py);
    //                 if(Physics2D.OverlapPoint(vtmp) == null)
    //                     walkable.Add(vtmp);
    //             }
    //             else
    //             {
    //                 spots[x, y] = new Vector3Int(px, py, 1);
    //             }
    //         }
    //     }
    //     walkableL = new List<Vector2>(walkable);
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }
    public Vector3 Rand(Bounds b)=>new Vector3(Random.Range(b.min.x,b.max.x),Random.Range(b.min.y,b.max.y),Random.Range(b.min.z,b.max.z));
    public Enemy Spawn(Enemy enemy, Vector2 pos)=>Instantiate(enemy, pos, Quaternion.identity);
    public Enemy Spawn(Enemy enemy, Bounds b)=>Spawn(enemy, Rand(b));
    public Enemy Spawn(Enemy enemy)=>Spawn(enemy,bounds);
    public Enemy Spawn(Enemy enemy, Vector2 oriPos,float range){
        Vector2 pos = oriPos;
        Queue<Vector2> q = new();q.Enqueue(pos);
        HashSet<Vector2> seen = new();
        List<Vector2> result = new();
        while(q.Count>0){
            var f = q.Dequeue();
            float dist = (f-oriPos).magnitude;
            if(dist>range)continue;
            else if(dist>.5)result.Add(f);
            foreach(var _d in Direction.vec2MoveDict){
                var d=_d*0.5f;
                if(seen.Add(f+d)&&(Physics2D.OverlapPoint(f+d)==null)&&bounds.Contains(f+d))q.Enqueue(f+d);
            }
        }
        if(result.Count>0)pos = result[Random.Range(0,result.Count)];
        // print(string.Join(", ", result));
        return Instantiate(enemy, pos, Quaternion.identity);
    }
    // public Enemy Spawn(Enemy enemy){
    //     var rand = Random.Range(0,walkable.Count);
    //     return Spawn(enemy,walkableL[rand]);
    // }
}
