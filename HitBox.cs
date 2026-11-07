using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    public Weapon weapon;
    public int damage;
    public Character owner; // who used it: hurts only other teams. null = hurts everyone
    public Vector2 playerPos; // the user's position: knockback pushes away from it
    public float startTime;
    public HashSet<Character> hitEnemy = new();
    [System.NonSerialized] public HoldingWeapons hand; // the hand that swung it: a key used up on a lock leaves this hand
    [System.NonSerialized] public string keyId;     // the key's own id (empty = weapon.keyId)
    [System.NonSerialized] public Loot thrownLoot;     // on a thrown weapon (Loot.Throw): the Loot moves it, and ending it stops the throw
    bool isThrow => thrownLoot!=null;
    string key => string.IsNullOrEmpty(keyId) ? weapon.keyId : keyId;
    private Rigidbody2D rb;
    int hitCount;
    static readonly Collider2D[] touching = new Collider2D[8];

    public bool CanHit(Character c) => owner==null ? c!=null : owner.IsEnemyOf(c);
    public void setVelocity(Vector2 v){
        if(v.magnitude==0)return;
        if(!rb)rb = GetComponent<Rigidbody2D>();
        rb.velocity = v;
    }
    void Start(){
        startTime = Time.time;
        if(isThrow)return; // a throw lasts until it stops
        if(weapon.isKey)OpenLocksTouching(); // a swing at a door: a static door may send no trigger event
        if(weapon.destroyTimer==0 && weapon.velocity==0)Destroy(gameObject, Time.fixedDeltaTime*2); // instant hit (sword): gone even if it touches nothing, after physics has found what it overlaps
    }
    void Update(){if(!isThrow && weapon.destroyTimer!=0 && Time.time-startTime>weapon.destroyTimer)Destroy(gameObject);}

    void OnTriggerEnter2D(Collider2D other){
        if(!enabled)return; // a weapon lying on the floor: its HitBox is off (Unity still calls this when off)
        Touch(other);
        if(!isThrow && weapon.destroyTimer==0)Destroy(gameObject);
    }

    // what a swing, shot or thrown weapon does to `other`. Loot also calls it for the solid things a throw runs into
    public void Touch(Collider2D other){
        if(weapon.isKey && other.TryGetComponent(out Lock lk) && lk.Fits(weapon,key))Unlock(lk); // then a chest still gets the hit, which opens it
        if(other.TryGetComponent(out Character enemy) && CanHit(enemy)){
            if(!hitEnemy.Add(enemy))return; // each enemy once
            SoundPlayer.PlaySound(weapon.hitSound);
            enemy.recieveDamage(damage,weapon.isMagical);
            enemy.receiveKnockback(playerPos, isThrow ? weapon.throwKnockback : weapon.knocback);
            int maxHits = isThrow ? weapon.throwMaxHits : weapon.maxHits;
            if(maxHits>0 && maxHits<=++hitCount)End();
        }else if(!weapon.isMagical && other.TryGetComponent(out Destructable des)){
            des.recieveDamage(damage,weapon.isMagical);
            End();
        }
    }
    void End(){
        if(isThrow)thrownLoot.StopThrow();
        else Destroy(gameObject);
    }
    void OpenLocksTouching(){
        int n = GetComponent<Collider2D>().OverlapCollider(new ContactFilter2D().NoFilter(), touching);
        for(int i=0;i<n;i++)if(touching[i].TryGetComponent(out Lock lk) && lk.Fits(weapon,key))Unlock(lk);
    }
    void Unlock(Lock lk){
        lk.Unlock();
        if(!lk.consumeKey)return;
        if(isThrow){thrownLoot.ConsumeKey(); return;} // the thrown key is gone
        if(hand!=null && hand.weapon==weapon){hand.weapon = GameManager.Instance.fist; hand.keyId = ""; GameManager.Instance.ui.UpdateWeap();} // the swung key leaves the hand
    }
}
