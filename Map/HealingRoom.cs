using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingRoom : MonoBehaviour
{
    public Collider2D healingArea;
    public float HPHealPerSecond = 3f;
    public GameObject healingEffect;
    public float animLengthSec = .5f;
    [SerializeField] AudioClip heal_sound;
    float stayTime;

    void OnTriggerEnter2D(Collider2D other){if(other.TryGetComponent(out Player player)){stayTime=0;player.isHealing=true;}}
    void OnTriggerExit2D(Collider2D other){if(other.TryGetComponent(out Player player))player.isHealing=false;}
    void Update(){
        if(!Player.all.Exists(p=>p.isHealing))return;
        stayTime+=Time.deltaTime;
        if(stayTime*HPHealPerSecond<=1)return;
        stayTime=0;
        SoundPlayer.PlaySound(heal_sound);
        foreach(var player in Player.all)if(player.isHealing){ // every player standing in the water
            player.Heal();
            Destroy(Instantiate(healingEffect,player.transform.position,Quaternion.identity,player.transform),animLengthSec);
        }
    }
}
