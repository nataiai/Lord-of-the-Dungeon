using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
// public class RoomRow{
//     public DungeonRoom[] rooms;
// }
public class DungeonMap : MonoBehaviour
{
    // public DungeonRoom currentRoom=>_findRoom(GameManager.Instance.playerPos);
    public DungeonRoom currentRoom;
    public DungeonRoom[] allRooms;
    public int width,height; // pixel of each cell
    public RoomTrigger[] roomTrigger;
    public Dictionary<RoomTrigger,RoomTrigger> triggerMap = new();
    // public RoomRow[] rooms;

    void OnValidate(){
        allRooms = FindObjectsByType<DungeonRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    }

    void Start(){
        roomTrigger = FindObjectsByType<RoomTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        fillTriggerMap();
        foreach(var r in allRooms){
            if(r.bounds.Contains(GameManager.Instance.playerPos)&&r.gameObject.activeInHierarchy)currentRoom = r;
        }
        // if(allRooms.Length>0)currentRoom = _findRoom(); 
    }

    void fillTriggerMap(){
        foreach(var a in roomTrigger){
            float m = 999999;
            RoomTrigger mr = null;
            foreach(var b in roomTrigger){
                if(a.transform.parent == b.transform.parent)continue;
                float d = (a.transform.position - b.transform.position).magnitude;
                if(d<m){
                    m=d; mr = b;
                }
            }
            triggerMap[a] = mr;
        }
    }
    
    public void roomTriggerEnter(RoomTrigger roomTrigger){
        // when player step on some trigger: it will trigger the closest trigger
        // triggerMap[roomTrigger].transform.parent.gameObject.SetActive(true);
    }
    public void roomTriggerExit(RoomTrigger roomTrigger){
        var playerV = GameManager.Instance.player.rb.velocity;
        var a = ((Vector2)roomTrigger.transform.position);
        var b = ((Vector2)triggerMap[roomTrigger].transform.position);
        if(Vector2.Dot(b-a,playerV)>0)switchRoom(roomTrigger);
    }
    public void roomTriggerExitLegacy2(RoomTrigger roomTrigger){
        var playerPos = GameManager.Instance.playerPos;
        var a = (playerPos - (Vector2)roomTrigger.transform.position).magnitude;
        var b = (playerPos - (Vector2)triggerMap[roomTrigger].transform.position).magnitude;
        if(b<a)switchRoom(roomTrigger);
    }
    public void switchRoom(RoomTrigger roomTrigger){
        currentRoom = triggerMap[roomTrigger].transform.parent.GetComponent<DungeonRoom>();
        currentRoom.gameObject.SetActive(true);
        DungeonRoom triggerRoom = roomTrigger.transform.parent.GetComponent<DungeonRoom>();
        triggerRoom.gameObject.SetActive(false);
        GameManager.Instance.UpdateMusic();
    }
    public void roomTriggerExitLegacy(RoomTrigger roomTrigger){
        DungeonRoom room = _findRoom(GameManager.Instance.playerPos);
        DungeonRoom triggerRoom = roomTrigger.transform.parent.GetComponent<DungeonRoom>();
        if(room != triggerRoom){
            switchRoom(roomTrigger);
            // currentRoom.gameObject.SetActive(true);
        }
    }

    public DungeonRoom _findRoom(Vector2 pos){ // buggy, use with care
        foreach(var r in allRooms){
            if(r.bounds.Contains(pos))return r;
        }return null;
    }


    // public DungeonRoom getRoom(Vector2 pos){
    //     var origin = transform.position;
    //     int x = Mathf.FloorToInt((pos.x-origin.x)/width);
    //     int y = Mathf.FloorToInt((pos.y-origin.y)/height);
    //     return getRoom(x,y);
    // }
    // public bool isRoomInRange(int x, int y){return rooms.Length>x && rooms[x].rooms.Length>y;}
    // public DungeonRoom getRoom(int x, int y){return isRoomInRange(x,y)?rooms[x].rooms[y]:null;}

}