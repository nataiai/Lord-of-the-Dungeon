using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Throwing weapons: hold Throw (Q / gamepad X), then a weapon button. Settings are on each Weapon (Throw section). ON/OFF: Experimental.throwing
// The weapon flies (AI/Slide.cs), hurts the thrower's enemies, opens a Lock if it's a key (AI/Lock.cs),
// and lands as normal Loot where it stops. The hand left behind holds GameManager.fist; fist can't be thrown.
public class ThrownWeapon : MonoBehaviour
{
    Weapon weapon;
    Character thrower;
    public string keyId{get; private set;} // a key keeps its id when it lands (AIHooks.OnDrop)
    Vector2 velocity;
    Rigidbody2D rb;
    float radius;
    int damage, hitCount;
    bool done;
    readonly HashSet<Character> hit = new();

    static bool ThrowHeld(){ // like Controller: a connected gamepad replaces the keyboard
        if(Gamepad.current!=null)return Gamepad.current.xButton.isPressed;
        return Keyboard.current!=null && Keyboard.current.qKey.isPressed;
    }
    // Player.Update: true = Throw is held (it throws on a weapon button), so no attacks this frame
    public static bool TryThrow(Player p){
        if(!Experimental.I.throwing || !ThrowHeld())return false;
        var c = GameManager.Instance.controller;
        TryThrow(p, p.lWeapon, c.attackLeft);
        TryThrow(p, p.rWeapon, c.attackRight);
        return true;
    }
    static void TryThrow(Player p, HoldingWeapons hand, ControllerButton button){
        var w = hand.weapon;
        if(w==null || !w.canThrow || hand.isOnCooldown || w==GameManager.Instance.fist || w.type==WeaponType.CQC || !button.isPressedOnce("throw"))return; // fist can't be thrown; a boomerang still out can't either
        Throw(w, p, hand.keyId, p.isLooking ? p.looking : Direction.vec2MoveDict[p.animFacing], hand.lootScale);
        hand.weapon = GameManager.Instance.fist; // an empty hand is a fist
        hand.keyId = "";
        hand.lastUse = Time.time; // no swing straight after
        GameManager.Instance.ui.UpdateWeap();
    }

    // scale: the size it had as loot (zero = 1), so it looks the same flying and landed
    public static ThrownWeapon Throw(Weapon weapon, Character by, string keyId, Vector2 dir, Vector3 scale=default){
        var go = new GameObject(weapon.name+" (thrown)");
        go.transform.position = by.center;
        if(GameManager.Instance.currentRoom!=null)go.transform.parent = GameManager.Instance.currentRoom.transform; // pauses with its room
        go.transform.rotation = Quaternion.Euler(0,0,Slide.PointAngle(dir, weapon.spriteAngle));
        go.transform.localScale = scale==Vector3.zero ? Vector3.one : scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var bySr = by.GetComponentInChildren<SpriteRenderer>();
        if(bySr!=null){sr.sortingLayerID = bySr.sortingLayerID; sr.sortingOrder = bySr.sortingOrder+1;}
        var t = go.AddComponent<ThrownWeapon>();
        t.radius = Mathf.Max(weapon.throwSize,0.05f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = t.radius/Mathf.Max(go.transform.lossyScale.x,0.01f); col.isTrigger = true; // throwSize stays its real size
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
        var touched = AIHooks.Hit(other, thrower, weapon, damage, weapon.throwKnockback, rb.position, hit);
        if(touched==AIHooks.Touched.Crate || touched==AIHooks.Touched.Enemy && weapon.throwMaxHits>0 && weapon.throwMaxHits<=++hitCount){Land(); return false;}
        return true;
    }
    void Land(){
        if(done)return;
        done = true;
        StartCoroutine(BecomeLoot());
    }
    IEnumerator BecomeLoot(){ // after this physics step, so it lands where it really stopped
        yield return new WaitForFixedUpdate();
        Loot.Drop(null, weapon, transform); // AIHooks.OnDrop gives it the key id, and no slide
        Destroy(gameObject);
    }
}
