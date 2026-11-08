using System.Collections.Generic;
using UnityEngine;

// A Boomerang-type weapon's attack comes back: its hitbox flies out at weapon.velocity, slows by weapon.boomerangDeceleration,
// and at 0 speed (or at a wall) flies back through walls to whoever threw it, hitting enemies again on the way.
// That hand stays on cooldown (empty reload bar) until it's back. ON/OFF: Experimental.boomerangReturns. Started by AIHooks.OnAttack
public class Boomerang : MonoBehaviour
{
    const float maxTime = 10; // safety: gone after this many seconds anyway
    Weapon weapon;
    HoldingWeapons hand;
    Character owner;
    Vector2 velocity, from;
    Rigidbody2D rb;
    float radius, born;
    int damage;
    bool returning;
    readonly HashSet<Character> hit = new();

    // false = not a boomerang (or turned off): the hitbox flies as usual
    public static bool Launch(HitBox hb, HoldingWeapons hand, Character owner, Vector2 dir){
        if(!Experimental.I.boomerangReturns || hb==null || hb.weapon==null || hb.weapon.type!=WeaponType.Boomerang)return false;
        var b = hb.gameObject.AddComponent<Boomerang>();
        b.weapon = hb.weapon; b.damage = hb.damage; b.hand = hand; b.owner = owner; b.born = Time.time;
        Destroy(hb); // this flies and hits instead of the HitBox (gone before its Start, so it never destroys itself)
        b.rb = Slide.Body(hb.gameObject);
        b.rb.bodyType = RigidbodyType2D.Kinematic; b.rb.useFullKinematicContacts = true; b.rb.velocity = Vector2.zero; // moved by AI/Slide.cs
        b.velocity = dir.normalized*b.weapon.velocity;
        var col = hb.GetComponent<Collider2D>();
        b.radius = col!=null ? Mathf.Max(Mathf.Min(col.bounds.extents.x,col.bounds.extents.y),0.05f) : 0.25f;
        if(hand!=null)hand.lastUse = float.MaxValue/2; // on cooldown until it's back
        return true;
    }
    void FixedUpdate(){
        if(owner==null || !owner.isActiveAndEnabled || Time.time>born+maxTime){Destroy(gameObject); return;}
        from = rb.position;
        if(!returning){
            Slide.Step(rb, ref velocity, radius, weapon.boomerangDeceleration, 0, false); // stops at a wall
            if(velocity==Vector2.zero){returning = true; hit.Clear();} // turn back; it can hit the same enemies again
        }else if(Slide.Toward(rb, ref velocity, owner.center, weapon.boomerangDeceleration, Mathf.Max(weapon.velocity,1)) < radius+0.4f){Destroy(gameObject); return;} // caught
        if(weapon.spinSpeed!=0)Slide.Turn(rb, velocity, weapon.spinSpeed, weapon.velocity, weapon.spriteAngle);
    }
    void OnTriggerEnter2D(Collider2D other)=>AIHooks.Hit(other, owner, weapon, damage, weapon.knocback, from, hit);
    void OnDestroy(){if(hand!=null && hand.weapon!=null)hand.lastUse = Time.time - hand.cooldown;} // back (or gone some other way): ready again
}
