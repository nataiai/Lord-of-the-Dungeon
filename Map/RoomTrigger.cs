using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomTrigger : MonoBehaviour
{
    void OnTriggerExit2D(Collider2D other){
        if (other.TryGetComponent(out Player player)){
            GameManager.Instance.map.roomTriggerExit(this);
        }
    }
    void OnTriggerStay2D(Collider2D other){ // a knockback can't push the player through: like a wall
        if(!other.TryGetComponent(out Player player) || !player.isStunned)return;
        var to = GameManager.Instance.map.triggerMap[this];
        if(to==null)return;
        Vector2 way = (to.transform.position - transform.position).normalized;
        float v = Vector2.Dot(player.rb.velocity, way);
        if(v>0)player.rb.velocity -= way*v;
    }
}
