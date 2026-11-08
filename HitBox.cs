using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    [PlayModeOnly] public Weapon weapon;
    [PlayModeOnly] public int damage;
    [PlayModeOnly] public Character owner; // who used it: hurts only other teams. null = hurts everyone
    [PlayModeOnly] public Vector2 playerPos; // the user's position: knockback pushes away from it
    [PlayModeOnly] public float startTime;
    public HashSet<Character> hitEnemy = new();
    [System.NonSerialized] public bool keepAlive; // a boomerang (AI/Boomerang.cs) decides when it's gone
    private Rigidbody2D rb;
    int hitCount;

    public bool CanHit(Character c) => owner==null ? c!=null : owner.IsEnemyOf(c);
    public void setVelocity(Vector2 v){
        if(v.magnitude==0)return;
        if(!rb)rb = GetComponent<Rigidbody2D>();
        rb.velocity = v;
    }
    void Start(){
        startTime = Time.time;
        if(!keepAlive && weapon.destroyTimer==0 && weapon.velocity==0)Destroy(gameObject, Time.fixedDeltaTime*2); // instant hit (sword): gone even if it touches nothing, after physics has found what it overlaps
    }
    void Update(){if(!keepAlive && weapon.destroyTimer!=0 && Time.time-startTime>weapon.destroyTimer)Destroy(gameObject);}

    void OnTriggerEnter2D(Collider2D other){
        if(other.TryGetComponent(out Character enemy) && CanHit(enemy)){
            if(!hitEnemy.Add(enemy))return; // each enemy once
            SoundPlayer.PlaySound(weapon.hitSound);
            enemy.recieveDamage(damage,weapon.isMagical);
            enemy.receiveKnockback(playerPos,weapon.knocback);
            if(weapon.maxHits>0 && weapon.maxHits<=++hitCount)End();
        }else if(!weapon.isMagical && other.TryGetComponent(out Destructable des)){
            des.recieveDamage(damage,weapon.isMagical);
            End();
        }
        if(weapon.destroyTimer==0)End();
    }
    void End(){if(!keepAlive)Destroy(gameObject);}
}
