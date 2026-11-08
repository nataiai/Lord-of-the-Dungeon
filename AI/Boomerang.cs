using UnityEngine;

// A Boomerang-type weapon's attack comes back: its hitbox flies out at weapon.velocity, slows by weapon.boomerangDeceleration,
// and at 0 speed (or at a wall) flies back through walls to whoever threw it, hitting enemies again on the way.
// That hand can't attack or throw until it's back. ON/OFF: Experimental.boomerangReturns. Started by HoldingWeapons.ForceUse
public class Boomerang : MonoBehaviour
{
    const float maxTime = 10; // safety: gone after this many seconds anyway
    HitBox hitBox;
    HoldingWeapons hand;
    Character owner;
    Vector2 velocity;
    Rigidbody2D rb;
    float radius, born;
    bool returning;

    // call right after making the attack's hitbox. false = not a boomerang (or turned off): the hitbox flies as usual
    public static bool Launch(HitBox hb, HoldingWeapons hand, Character owner, Vector2 dir){
        if(!Experimental.I.boomerangReturns || hb==null || hb.weapon==null || hb.weapon.type!=WeaponType.Boomerang)return false;
        var b = hb.gameObject.AddComponent<Boomerang>();
        b.hitBox = hb; b.hand = hand; b.owner = owner; b.born = Time.time;
        hb.keepAlive = true; // the boomerang decides when it's gone, not the HitBox
        b.rb = Slide.Body(hb.gameObject);
        b.rb.bodyType = RigidbodyType2D.Kinematic; b.rb.useFullKinematicContacts = true; b.rb.velocity = Vector2.zero; // moved by AI/Slide.cs
        b.velocity = dir.normalized*hb.weapon.velocity;
        var col = hb.GetComponent<Collider2D>();
        b.radius = col!=null ? Mathf.Max(Mathf.Min(col.bounds.extents.x,col.bounds.extents.y),0.05f) : 0.25f;
        if(hand!=null)hand.busy = true;
        return true;
    }
    void FixedUpdate(){
        var w = hitBox.weapon;
        if(owner==null || !owner.isActiveAndEnabled || Time.time>born+maxTime){Destroy(gameObject); return;}
        if(!returning){
            Slide.Step(rb, ref velocity, radius, w.boomerangDeceleration, 0, false); // stops at a wall
            if(velocity==Vector2.zero){returning = true; hitBox.hitEnemy.Clear();} // turn back; it can hit the same enemies again
        }else if(Slide.Toward(rb, ref velocity, owner.center, w.boomerangDeceleration, Mathf.Max(w.velocity,1)) < radius+0.4f){Destroy(gameObject); return;} // caught
        if(w.spinSpeed!=0)Slide.Turn(rb, velocity, w.spinSpeed, w.velocity, w.spriteAngle);
    }
    void OnDestroy(){if(hand!=null)hand.busy = false;} // back (or gone some other way): the hand can use it again
}
