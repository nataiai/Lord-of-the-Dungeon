using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomTrigger : MonoBehaviour // a door trigger, a direct child of its DungeonRoom
{
    void OnTriggerExit2D(Collider2D other){
        if (other.TryGetComponent(out Player player)){
            GameManager.Instance.map.roomTriggerExit(this);
        }
    }
}
