using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomTrigger : MonoBehaviour
{

    // void OnTriggerEnter2D(Collider2D other){
    //     if (other.TryGetComponent(out Player player)){
    //         GameManager.Instance.map.roomTriggerEnter(this);
    //     }
    // }
    void OnTriggerExit2D(Collider2D other){
        if (other.TryGetComponent(out Player player)){
            GameManager.Instance.map.roomTriggerExit(this);
        }
    }
}