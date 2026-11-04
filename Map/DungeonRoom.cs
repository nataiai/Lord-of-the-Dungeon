using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public enum RoomType{
    rest,enemies,traps,boss
}

[System.Serializable]
public class DungeonRoom : MonoBehaviour
{
    // public Spawner[] spawners;
    [SerializeField] Spawner spawner;
    public RoomType roomType=RoomType.enemies; //rest, enemies, traps, boss

    public Bounds bounds => spawner.bounds;
    // public bool isLock;
    // public Collider2D coll;
    // public void Spawn(Enemy enemy){
    //     int index = Random.Range(0,spawners.Length);
    //     Spawn(enemy,index);
    // }
    // public void Spawn(Enemy enemy, int index){
    //     spawners[index].Spawn(enemy);
    // }
    void OnValidate(){
        // spawners = GetComponentsInChildren<Spawner>(true);
        // spawner = spawners[0];
    }
    void Start(){
        // var tmp = GetComponentsInChildren<Spawner>();
        // foreach(var i in tmp){
        //     if(i.sr!=null)spawner = i;
        // }
        // GenerateSpawn();
    }
    void Update(){
        // if(isLock)onLock();
    }
    // void GenerateSpawn(){
        
    //     var b = coll.bounds;
    //     var mn = b.min;
    //     var mx = b.max;
    //     float randX() => Random.Range(mn.y,mx.y);
    //     float randY() => Random.Range(mn.x,mx.x);
    //     var l = new [] {mn,mx};
    //     spawners = new Spawner[20];
    //     for(int i = 0;i<20;i++){
    //         if(i%2==0)spawners[i] = Instantiate(spawner, new Vector2(l[Random.Range(0,2)].x,randX()), Quaternion.identity);
    //         else spawners[i] = Instantiate(spawner, new Vector2(randY(),l[Random.Range(0,2)].y), Quaternion.identity);
    //     }


    // }
    // void onLock(){  // quick fix, proper wall later
    //     var b = coll.bounds;
    //     var mn = b.min;
    //     var mx = b.max;
    //     var p = GameManager.Instance.player.transform.position;
    //     if(p.x>mx.x)p.x=mx.x;
    //     if(p.y>mx.y)p.y=mx.y;
    //     if(p.x<mn.x)p.x=mn.x;
    //     if(p.y<mn.y)p.y=mn.y;
    //     GameManager.Instance.player.transform.position = p;
    // }
    
    public void Spawn(Enemy enemy, Vector2 oriPos,float range){
        Enemy spawned = spawner.Spawn(enemy,oriPos,range);
    // public void Spawn(Enemy enemy){
    //     Enemy spawned = spawner.Spawn(enemy);
        var enemies = transform.Find("Enemies");
        if(!enemies){
            enemies = new GameObject("Enemies").transform;
            enemies.SetParent(transform);
        }
        spawned.transform.parent = enemies;
    }
}