using System.Collections.Generic;
using UnityEngine;

// Every Character standing in this trigger heals 1 HP, HPHealPerSecond times a second, with a sound and its heal animation
public class HealingRoom : MonoBehaviour
{
    public float HPHealPerSecond = 3f;
    [SerializeField] AudioClip heal_sound;
    readonly List<Character> inside = new();
    float nextHeal;

    void OnTriggerEnter2D(Collider2D other){
        if(!other.TryGetComponent(out Character c))return;
        if(inside.Count==0)nextHeal = Time.time + 1/HPHealPerSecond;
        inside.Add(c);
        c.isHealing = true;
    }
    void OnTriggerExit2D(Collider2D other){
        if(!other.TryGetComponent(out Character c))return;
        inside.Remove(c);
        c.isHealing = false;
    }
    void Update(){
        if(inside.Count==0 || Time.time<nextHeal)return;
        nextHeal = Time.time + 1/HPHealPerSecond;
        inside.RemoveAll(c => c==null); // destroyed while inside
        foreach(var c in inside)if(!c.isDead)c.HealWithEffect(1); // dying enemies stay dead
        SoundPlayer.PlaySound(heal_sound);
    }
}
