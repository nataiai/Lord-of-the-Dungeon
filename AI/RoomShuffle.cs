using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Experimental.shuffleRooms: the scene's rooms stay where they are, but their doors are paired at random
// (a right door with a left door, an up door with a down door), so every run leads somewhere else.
// Every room can still be reached from the start. Walking through a door moves you to its new partner.
// Not yet: rotating rooms, keys placed before their locks.
public static class RoomShuffle
{
    static readonly Dictionary<RoomTrigger,RoomTrigger> near = new(); // the door really next to it in the scene
    public static RoomTrigger lastDoor; // the door the player last walked through (for the mini map)

    public static RoomTrigger Near(RoomTrigger t) => near.TryGetValue(t, out var n) ? n : null;
    static DungeonRoom Room(RoomTrigger t) => t.transform.parent.GetComponent<DungeonRoom>(); // like DungeonMap.switchRoom
    static Vector2 Out(RoomTrigger t) => Direction.vec2MoveDict[Direction.facing(((Vector2)(Near(t).transform.position - t.transform.position)).normalized)]; // which side it's on

    // DungeonMap.Start, after fillTriggerMap
    public static void Shuffle(DungeonMap map){
        near.Clear();
        foreach(var p in map.triggerMap)if(p.Value)near[p.Key] = p.Value;
        if(!Experimental.I.shuffleRooms)return;
        var rnd = Experimental.I.roomSeed!=0 ? new System.Random(Experimental.I.roomSeed) : new System.Random();
        for(int tries=0;tries<100;tries++){
            var pairs = new Dictionary<RoomTrigger,RoomTrigger>();
            if(!Pair(Vector2.right, rnd, pairs) || !Pair(Vector2.up, rnd, pairs) || !Reaches(map.currentRoom, pairs))continue;
            foreach(var p in pairs)map.triggerMap[p.Key] = p.Value;
            return;
        }
        Debug.LogWarning("RoomShuffle: no other layout found, keeping the scene's");
    }
    // every door on that side gets a random door on the other side, in another room
    static bool Pair(Vector2 side, System.Random rnd, Dictionary<RoomTrigger,RoomTrigger> pairs){
        var a = new List<RoomTrigger>(); var b = new List<RoomTrigger>();
        foreach(var t in near.Keys){
            if(Out(t)==side)a.Add(t);
            else if(Out(t)==-side)b.Add(t);
        }
        if(a.Count!=b.Count)return false;
        for(int i=b.Count-1;i>0;i--){int j = rnd.Next(i+1); (b[i],b[j]) = (b[j],b[i]);}
        for(int i=0;i<a.Count;i++){
            if(Room(a[i])==Room(b[i]))return false;
            pairs[a[i]] = b[i]; pairs[b[i]] = a[i];
        }
        return true;
    }
    static bool Reaches(DungeonRoom start, Dictionary<RoomTrigger,RoomTrigger> pairs){ // every room, from the start
        var seen = new HashSet<DungeonRoom>{start};
        var todo = new Queue<DungeonRoom>(seen);
        while(todo.Count>0){
            var r = todo.Dequeue();
            foreach(var p in pairs)if(Room(p.Key)==r && seen.Add(Room(p.Value)))todo.Enqueue(Room(p.Value));
        }
        foreach(var t in near.Keys)if(!seen.Contains(Room(t)))return false;
        return true;
    }

    // DungeonMap.switchRoom: through `door`, to its partner. Not shuffled: no move
    public static void Warp(DungeonMap map, RoomTrigger door){
        lastDoor = door;
        var n = Near(door);
        if(n==null)return;
        Vector3 shift = map.triggerMap[door].transform.position - n.transform.position;
        if(shift==Vector3.zero)return;
        var player = GameManager.Instance.player.transform;
        player.position += shift;
        foreach(var cam in Object.FindObjectsByType<Cinemachine.CinemachineVirtualCamera>(FindObjectsSortMode.None))cam.OnTargetObjectWarped(player, shift); // no camera sweep
    }
}
