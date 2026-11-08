using System.Collections.Generic;
using UnityEngine;

// Characters standing in this trigger heal 1 HP per tick, each team at its own rate, with a sound and the heal animation.
// A team with no tick (or 0 per second) is not healed. Default: only the player.
public class HealingRoom : MonoBehaviour
{
    [System.Serializable]
    public class Tick{
        public Team team;
        public float HPHealPerSecond = 3f;
        [HideInInspector] public float next;
    }
    public Tick[] ticks = {new Tick{team = Team.Player}};
    [SerializeField] AudioClip heal_sound;
    readonly List<Character> inside = new();

    Tick TickFor(Character c) => System.Array.Find(ticks, t => t.team==c.team && t.HPHealPerSecond>0);

    void OnTriggerEnter2D(Collider2D other){
        if(!other.TryGetComponent(out Character c) || TickFor(c) is not Tick tick)return;
        if(tick.next<Time.time)tick.next = Time.time + 1/tick.HPHealPerSecond; // first of its team inside: a full tick before healing
        inside.Add(c);
        c.isHealing = true;
    }
    void OnTriggerExit2D(Collider2D other){
        if(!other.TryGetComponent(out Character c) || !inside.Remove(c))return;
        c.isHealing = false;
    }
    void Update(){
        inside.RemoveAll(c => c==null); // destroyed while inside
        foreach(var tick in ticks){
            if(tick.HPHealPerSecond<=0 || Time.time<tick.next)continue;
            tick.next = Time.time + 1/tick.HPHealPerSecond;
            bool healed = false;
            foreach(var c in inside)if(c.team==tick.team && !c.isDead){c.HealWithEffect(1); healed = true;} // dying enemies stay dead
            if(healed)SoundPlayer.PlaySound(heal_sound);
        }
    }
}
