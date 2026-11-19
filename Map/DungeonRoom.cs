using UnityEngine;

public enum RoomType{rest,enemies,traps,boss,weapons}

public class DungeonRoom : MonoBehaviour
{
    [SerializeField] Spawner spawner;
    public RoomType roomType=RoomType.enemies; // picks the music
    [PlayModeOnly] public int potionsSpawned;
    [ShowIf("roomType", RoomType.weapons)]
    [Tooltip("Chat votes the player's right hand (AI/WeaponRoom.cs)")]
    public WeaponRoom weaponRoom = new();
    public Bounds bounds => spawner.bounds;
    void OnEnable(){if(roomType==RoomType.weapons)weaponRoom.Enter();}
    void OnDisable(){weaponRoom.Leave();}

    public void Spawn(Enemy enemy, Vector2 oriPos,float range){
        var enemies = transform.Find("Enemies");
        if(!enemies){enemies = new GameObject("Enemies").transform; enemies.SetParent(transform);}
        var e = spawner.Spawn(enemy,oriPos,range);
        e.transform.parent = enemies;
        e.gameObject.SetActive(true);
    }
    public void SpawnPotion(Loot potion, Vector2 oriPos,float range){
        var p = spawner.Spawn(potion,oriPos,range);
        p.transform.parent = transform;
        AIHooks.OnDrop(p, transform); // slides like dropped loot
        potionsSpawned++;
    }
}
