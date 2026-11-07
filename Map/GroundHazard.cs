using System.Collections.Generic;
using UnityEngine;

// Something on the floor. Characters in the air (jumping or floating, Character.isAirborne) pass over it.
//   hole:   collider NOT a trigger. Blocks walking characters like a wall; landing on it hurts, then physics pushes them out
//   spikes: trigger collider + damage
//   trap:   trigger collider + buff = stun or slow
[RequireComponent(typeof(Collider2D))]
public class GroundHazard : MonoBehaviour
{
    public int damage = 1;
    public bool isMagical;
    [Tooltip("Pushes out from the middle of the hazard. 0 = none")]
    public float knockback;
    public BuffEffect.Buff buff; // optional, like a weapon's: type none = no effect
    public float cooldown = 1;   // seconds between hits on the same character
    public bool hurtsEnemies = true;
    readonly Dictionary<Character,float> nextHit = new();
    static readonly List<Collider2D> holes = new();
    Collider2D col;
    bool isHole => !col.isTrigger;

    void OnEnable(){ // also when its room becomes active again: Unity forgets IgnoreCollision while a collider is off
        col = GetComponent<Collider2D>();
        if(!isHole)return;
        holes.Add(col);
        foreach(var c in FindObjectsByType<Character>(FindObjectsSortMode.None))
            if(c.isAirborne && c.bodyCollider)Physics2D.IgnoreCollision(c.bodyCollider,col,true);
    }
    void OnDisable()=>holes.Remove(col);
    // Character calls this when it takes off or lands
    public static void SetAirborne(Collider2D who, bool inAir){foreach(var h in holes)if(h)Physics2D.IgnoreCollision(who,h,inAir);}

    void OnTriggerStay2D(Collider2D other)=>Hit(other); // spikes, traps. Stay, so landing on it after a jump also counts
    void OnCollisionEnter2D(Collision2D c){ // hole: only if it landed on it, not walked into its edge
        if(c.collider.TryGetComponent(out Character ch) && col.OverlapPoint(ch.center))Hit(c.collider);
    }

    void Hit(Collider2D other){
        if(!other.TryGetComponent(out Character c) || c.isDead || c.isAirborne)return;
        if(!hurtsEnemies && c.team==Team.Enemy)return;
        if(nextHit.TryGetValue(c,out float t) && t>Time.time)return;
        nextHit[c] = Time.time+cooldown;
        if(damage>0)c.recieveDamage(damage,isMagical);
        if(knockback>0)c.receiveKnockback(transform.position,knockback);
        if(buff!=null)buff.CopyTo(c.gameObject);
    }
}
