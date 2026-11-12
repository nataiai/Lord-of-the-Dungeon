using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Spawner : MonoBehaviour // on a room's floor tilemap
{
    public TilemapRenderer tilemapRenderer;
    [PlayModeOnly] public Bounds bounds;

    void Awake(){
        GetComponent<Tilemap>().CompressBounds();
        if(tilemapRenderer==null)tilemapRenderer = GetComponent<TilemapRenderer>();
        bounds = tilemapRenderer.bounds;
    }
    public Vector3 Rand(Bounds b)=>new Vector3(Random.Range(b.min.x,b.max.x),Random.Range(b.min.y,b.max.y),Random.Range(b.min.z,b.max.z));
    public Enemy Spawn(Enemy enemy, Vector2 pos)=>Instantiate(enemy, pos, Quaternion.identity);
    public Enemy Spawn(Enemy enemy, Bounds b)=>Spawn(enemy, Rand(b));
    public Enemy Spawn(Enemy enemy)=>Spawn(enemy,bounds);
    public Enemy Spawn(Enemy enemy, Vector2 oriPos,float range){
        Queue<Vector2> q = new();q.Enqueue(oriPos);
        HashSet<Vector2> seen = new();
        List<Vector2> result = new();
        while(q.Count>0){
            var f = q.Dequeue();
            float dist = (f-oriPos).magnitude;
            if(dist>range)continue;
            if(dist>.5)result.Add(f);
            foreach(var d in Direction.vec2MoveDict){
                var n = f+d*0.5f;
                if(seen.Add(n)&&Physics2D.OverlapPoint(n)==null&&bounds.Contains(n))q.Enqueue(n);
            }
        }
        return Instantiate(enemy, result.Count>0?result[Random.Range(0,result.Count)]:oriPos, Quaternion.identity);
    }
}
