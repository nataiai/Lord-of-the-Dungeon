using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonMap : MonoBehaviour // only currentRoom is active
{
    public DungeonRoom currentRoom;
    [HideInInspector] public DungeonRoom[] allRooms;
    public int width,height; // pixel of each cell
    [HideInInspector] public RoomTrigger[] roomTrigger;
    public Dictionary<RoomTrigger,RoomTrigger> triggerMap = new();

    void OnValidate()=>allRooms = FindObjectsByType<DungeonRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None);

    void Start(){
        roomTrigger = FindObjectsByType<RoomTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        fillTriggerMap();
        foreach(var r in allRooms)if(r.bounds.Contains(GameManager.Instance.playerPos)&&r.gameObject.activeInHierarchy)currentRoom = r;
    }
    void fillTriggerMap(){
        foreach(var a in roomTrigger){
            float m = float.MaxValue;
            RoomTrigger mr = null;
            foreach(var b in roomTrigger){
                float d = (a.transform.position - b.transform.position).magnitude;
                if(a.transform.parent != b.transform.parent && d<m){m=d; mr = b;}
            }
            triggerMap[a] = mr;
        }
    }
    public void roomTriggerExit(RoomTrigger trigger){
        Vector2 toPartner = triggerMap[trigger].transform.position - trigger.transform.position;
        if(Vector2.Dot(toPartner,GameManager.Instance.player.rb.velocity)>0)switchRoom(trigger);
    }
    public void switchRoom(RoomTrigger trigger){
        currentRoom = triggerMap[trigger].transform.parent.GetComponent<DungeonRoom>();
        currentRoom.gameObject.SetActive(true);
        trigger.transform.parent.gameObject.SetActive(false);
        foreach(var p in FindObjectsByType<Player>(FindObjectsSortMode.None))p.roomEntryPos = p.transform.position;
        GameManager.Instance.UpdateMusic();
        PathFinding.Invalidate();
    }
    public DungeonRoom _findRoom(Vector2 pos){ // buggy, use with care
        foreach(var r in allRooms)if(r.bounds.Contains(pos))return r;
        return null;
    }
}
