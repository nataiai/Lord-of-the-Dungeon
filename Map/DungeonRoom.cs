using UnityEngine;

public enum RoomType{rest,enemies,traps,boss}

public class DungeonRoom : MonoBehaviour
{
    [SerializeField] Spawner spawner;
    public RoomType roomType=RoomType.enemies; // picks the music
    public Bounds bounds => spawner.bounds;

    public void Spawn(Enemy enemy, Vector2 oriPos,float range){
        var enemies = transform.Find("Enemies");
        if(!enemies){enemies = new GameObject("Enemies").transform; enemies.SetParent(transform);}
        var e = spawner.Spawn(enemy,oriPos,range);
        e.transform.parent = enemies;
        e.gameObject.SetActive(true);
    }
}
