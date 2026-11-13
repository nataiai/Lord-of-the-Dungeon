using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Throwing weapons: hold Throw (Q / gamepad X), then a weapon button. Settings: each Weapon's Throw Settings foldout. ON/OFF: Experimental.throwing
// The weapon flies (AI/Slide.cs), hurts the thrower's enemies, opens a Lock if it's a key (AI/Lock.cs),
// and lands as normal Loot where it stops. The hand calls Unequip() (back to PrefabDatabase.fist); fist can't be thrown.
public class ThrownWeapon : MonoBehaviour
{
    Weapon weapon;
    Character thrower;
    Vector2 velocity;
    Rigidbody2D rb;
    Transform shadow;
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
        if(w==null || !w.throwSettings.canThrow || hand.isOnCooldown || w==PrefabDatabase.Instance.fist || w.type==WeaponType.CQC || !button.isPressedOnce("throw"))return; // fist can't be thrown; a boomerang still out can't either
        Throw(w, p, p.isLooking ? p.looking : Direction.vec2MoveDict[p.animFacing], hand.lootScale);
        hand.Unequip(); // an empty hand is a fist
        hand.lastUse = Time.time; // no swing straight after
        GameManager.Instance.ui.UpdateWeap();
    }

    // scale: the size it had as loot (zero = 1), so it looks the same flying and landed
    public static ThrownWeapon Throw(Weapon weapon, Character by, Vector2 dir, Vector3 scale=default){
        var go = new GameObject(weapon.name+" (thrown)");
        go.transform.position = by.center;
        if(GameManager.Instance.currentRoom!=null)go.transform.parent = GameManager.Instance.currentRoom.transform; // pauses with its room
        go.transform.rotation = Quaternion.Euler(0,0,Slide.PointAngle(dir, weapon.throwSettings.spriteAngle));
        go.transform.localScale = scale==Vector3.zero ? Vector3.one : scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var bySr = by.GetComponentInChildren<SpriteRenderer>();
        if(bySr!=null){sr.sortingLayerID = bySr.sortingLayerID; sr.sortingOrder = bySr.sortingOrder+1;}
        var t = go.AddComponent<ThrownWeapon>();
        if(weapon.throwSettings.shadowHeight>0){
            var shadow = new GameObject("shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(go.transform, false);
            shadow.sprite = sr.sprite;
            shadow.color = new Color(0,0,0,.4f);
            shadow.sortingLayerID = sr.sortingLayerID;
            shadow.sortingOrder = 3; // Order in Layer 3, like the other shadows
            t.shadow = shadow.transform;
        }
        t.radius = Mathf.Max(weapon.throwSettings.size,0.05f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = t.radius/Mathf.Max(go.transform.lossyScale.x,0.01f); col.isTrigger = true; // throwSettings.size stays its real size
        t.rb = Slide.Body(go);
        t.weapon = weapon; t.thrower = by;
        t.damage = weapon.Damage(by, weapon.throwSettings.damage);
        t.velocity = dir.normalized*weapon.throwSettings.initSpeed;
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
        return t;
    }

    void FixedUpdate(){
        if(done)return;
        Slide.Step(rb, ref velocity, radius, weapon.throwSettings.deceleration, weapon.throwSettings.bounciness, weapon.throwSettings.isBounce, Touch);
        if(done)return;
        if(velocity==Vector2.zero){Land(); return;} // a wall, or out of speed
        Slide.Turn(rb, velocity, weapon.throwSettings.spinSpeed, weapon.throwSettings.initSpeed, weapon.throwSettings.spriteAngle);
    }
    void LateUpdate(){ // straight below, even when it spins
        if(shadow==null)return;
        float speed = Mathf.Clamp01(velocity.magnitude/Mathf.Max(weapon.throwSettings.initSpeed,0.01f));
        shadow.position = transform.position + Vector3.down*weapon.throwSettings.shadowHeight*speed;
    }
    void OnTriggerEnter2D(Collider2D other){if(!done)Touch(other);} // enemies it passes

    bool Touch(Collider2D other){ // what it does to `other`. false = stop right here
        if(weapon.isKey && Lock.TryOpen(other, weapon, out bool usedUp) && usedUp){done = true; Destroy(gameObject); return false;} // key used up
        var touched = AIHooks.Hit(other, thrower, weapon, damage, weapon.throwSettings.knockback, rb.position, hit);
        if(touched==AIHooks.Touched.Crate || touched==AIHooks.Touched.Enemy && weapon.throwSettings.maxHits>0 && weapon.throwSettings.maxHits<=++hitCount){Land(); return false;}
        return true;
    }
    void Land(){
        if(done)return;
        done = true;
        StartCoroutine(BecomeLoot());
    }
    IEnumerator BecomeLoot(){ // after this physics step, so it lands where it really stopped
        yield return new WaitForFixedUpdate();
        Loot.Drop(null, weapon, transform); // AIHooks.OnDrop: keeps its size, no slide
        Destroy(gameObject);
    }
}
