using UnityEngine;

public enum RoomType{rest,enemies,traps,boss}

public class DungeonRoom : MonoBehaviour
{
    [SerializeField] Spawner spawner;
    public RoomType roomType=RoomType.enemies; // picks the music
    [PlayModeOnly] public int potionsSpawned;
    public Bounds bounds => spawner.bounds;

    public void Spawn(Enemy enemy, Vector2 oriPos,float range){
        var enemies = transform.Find("Enemies");
        if(!enemies){enemies = new GameObject("Enemies").transform; enemies.SetParent(transform);}
        var e = spawner.Spawn(enemy,oriPos,range);
        e.transform.parent = enemies;
        e.gameObject.SetActive(true);
    }
    public void SpawnPotion(InstantItemOnFloor potion, Vector2 oriPos,float range){
        spawner.Spawn(potion,oriPos,range).transform.parent = transform;
        potionsSpawned++;
    }
}
