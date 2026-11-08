using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Throwing weapons: hold Throw (Q / gamepad X), then a weapon button. Settings are on each Weapon (Throw section). ON/OFF: Experimental.throwing
// The weapon flies (AI/Slide.cs), hurts the thrower's enemies, opens a Lock if it's a key (AI/Lock.cs),
// and lands as normal Loot where it stops. The hand left behind holds GameManager.fist; fist can't be thrown.
public class ThrownWeapon : MonoBehaviour
{
    Weapon weapon;
    Character thrower;
    string keyId;
    Vector2 velocity;
    Rigidbody2D rb;
    float radius;
    int damage, hitCount;
    bool done;
    readonly HashSet<Character> hit = new();

    // Player.Update: true = Throw is held (it throws on a weapon button), so no attacks this frame
    public static bool TryThrow(Player p){
        var c = GameManager.Instance.controller;
        if(!Experimental.I.throwing || !c.throwHold.isPressed)return false;
        TryThrow(p, p.lWeapon, c.attackLeft);
        TryThrow(p, p.rWeapon, c.attackRight);
        return true;
    }
    static void TryThrow(Player p, HoldingWeapons hand, ControllerButton button){
        var w = hand.weapon;
        if(w==null || !w.canThrow || hand.busy || w==GameManager.Instance.fist || w.type==WeaponType.CQC || !button.isPressedOnce("throw"))return; // fist can't be thrown; a boomerang still out can't either
        Throw(w, p, hand.keyId, p.isLooking ? p.looking : Direction.vec2MoveDict[p.animFacing]);
        hand.weapon = GameManager.Instance.fist; // an empty hand is a fist
        hand.keyId = "";
        hand.lastUse = Time.time; // no swing straight after
        GameManager.Instance.ui.UpdateWeap();
    }

    public static ThrownWeapon Throw(Weapon weapon, Character by, string keyId, Vector2 dir){
        var go = new GameObject(weapon.name+" (thrown)");
        go.transform.position = by.center;
        if(GameManager.Instance.currentRoom!=null)go.transform.parent = GameManager.Instance.currentRoom.transform; // pauses with its room
        go.transform.rotation = Quaternion.Euler(0,0,Slide.PointAngle(dir, weapon.spriteAngle));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var bySr = by.GetComponentInChildren<SpriteRenderer>();
        if(bySr!=null){sr.sortingLayerID = bySr.sortingLayerID; sr.sortingOrder = bySr.sortingOrder+1;}
        var t = go.AddComponent<ThrownWeapon>();
        t.radius = Mathf.Max(weapon.throwSize,0.05f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = t.radius; col.isTrigger = true;
        t.rb = Slide.Body(go);
        t.weapon = weapon; t.thrower = by; t.keyId = keyId;
        t.damage = weapon.throwDamage * (weapon.isMagical ? by.combatStat.attackMagic : by.combatStat.attackPhysic);
        t.velocity = dir.normalized*weapon.throwInitSpeed;
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
        return t;
    }

    void FixedUpdate(){
        if(done)return;
        Slide.Step(rb, ref velocity, radius, weapon.throwDeceleration, weapon.bounciness, weapon.isBounce, Touch);
        if(done)return;
        if(velocity==Vector2.zero){Land(); return;} // a wall, or out of speed
        Slide.Turn(rb, velocity, weapon.spinSpeed, weapon.throwInitSpeed, weapon.spriteAngle);
    }
    void OnTriggerEnter2D(Collider2D other){if(!done)Touch(other);} // enemies it passes

    bool Touch(Collider2D other){ // what it does to `other`. false = stop right here
        if(weapon.isKey && Lock.TryOpen(other, weapon, keyId, out bool usedUp) && usedUp){done = true; Destroy(gameObject); return false;} // key used up
        if(thrower!=null && other.TryGetComponent(out Character c) && thrower.IsEnemyOf(c)){ // a chest too: opens it if it's unlocked
            if(!hit.Add(c))return true; // each once
            SoundPlayer.PlaySound(weapon.hitSound);
            c.recieveDamage(damage, weapon.isMagical);
            c.receiveKnockback(rb.position, weapon.throwKnockback);
            if(weapon.buff!=null)weapon.buff.CopyTo(c.gameObject);
            if(weapon.throwMaxHits>0 && weapon.throwMaxHits<=++hitCount){Land(); return false;}
        }else if(!weapon.isMagical && other.TryGetComponent(out Destructable d)){
            d.recieveDamage(damage, weapon.isMagical);
            Land(); return false;
        }
        return true;
    }
    void Land(){
        if(done)return;
        done = true;
        StartCoroutine(BecomeLoot());
    }
    IEnumerator BecomeLoot(){ // after this physics step, so it lands where it really stopped
        yield return new WaitForFixedUpdate();
        Loot.Drop(null, weapon, transform, keyId, false);
        Destroy(gameObject);
    }
}
