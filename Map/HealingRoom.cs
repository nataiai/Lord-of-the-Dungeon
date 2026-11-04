using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealingRoom : MonoBehaviour
{
    public Collider2D healingArea;
    public float HPHealPerSecond = 3f;
    public GameObject healingEffect;
    public float animLengthSec = .5f;
    [SerializeField] AudioClip heal_sound;
    // public float healingEffectPerSecond;
    // private bool isPlayerInside;
    float healingEffectTime;
    float stayTime;
    GameObject healingEf;
    // void Start(){isPlayerInside=false;}
    void OnTriggerEnter2D(Collider2D other){
        if(other.TryGetComponent(out Player player)){
            stayTime=0;
            player.isHealing=true;
        }
    }
    // void OnTriggerStay2D(Collider2D other){
    //     if(other.TryGetComponent(out Player player)){
    void Update(){
        var player=GameManager.Instance.player;
        if(player.isHealing){
            stayTime+=Time.deltaTime;
            if(stayTime*HPHealPerSecond>1){
                player.Heal();
                stayTime=0;
                // if(healingEffect!=null&&healingEf==null)
                SoundPlayer.PlaySound(heal_sound);
                healingEf=Instantiate(healingEffect,player.transform.position, Quaternion.identity,player.transform);
                Destroy(healingEf,animLengthSec);
            }
            // print(stayTime);
            // healingEffectTime+=Time.deltaTime;
            // if(healingEffectTime*healingEffectPerSecond>1){
            //     if(healingEffect!=null)var healingEf=Instantiate(healingEffect,player.transform.position, Quaternion.identity,player.transform);
            //     healingEffectTime=0;
            // }
        }
    }
    void OnTriggerExit2D(Collider2D other){
        if(other.TryGetComponent(out Player player)){
            player.isHealing=false;
        }
    }

}