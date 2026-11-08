using System.Collections.Generic;
using UnityEngine;

// Characters of the ticked teams standing in this trigger heal 1 HP, HPHealPerSecond times a second, with a sound and the heal animation
public class HealingRoom : MonoBehaviour
{
    public Team heals = Team.Player;
    public float HPHealPerSecond = 3f;
    [SerializeField] AudioClip heal_sound;
    readonly List<Character> inside = new();
    float nextHeal;

    bool Heals(Character c) => heals.HasFlag(c.team);

    void OnTriggerEnter2D(Collider2D other){
        if(!other.TryGetComponent(out Character c) || !Heals(c))return;
        if(inside.Count==0)nextHeal = Time.time + 1/HPHealPerSecond;
        inside.Add(c);
        c.isHealing = true;
    }
    void OnTriggerExit2D(Collider2D other){
        if(!other.TryGetComponent(out Character c) || !inside.Remove(c))return;
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
