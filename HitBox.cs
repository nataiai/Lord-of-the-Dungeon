using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    [PlayModeOnly] public Weapon weapon;
    [PlayModeOnly] public int damage;
    [PlayModeOnly] public GameObject hitEffect;
    [PlayModeOnly] public float knockback;
    [PlayModeOnly] public Character owner;
    [PlayModeOnly] public Vector2 playerPos;
    [PlayModeOnly] public float startTime;
    public HashSet<Character> hitEnemy = new();
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
        if(weapon.destroyTimer==0 && weapon.velocity==0)Destroy(gameObject, Time.fixedDeltaTime*2);
    }
    void Update(){if(weapon.destroyTimer!=0 && Time.time-startTime>weapon.destroyTimer)Destroy(gameObject);}

    void OnTriggerEnter2D(Collider2D other){
        if(other.TryGetComponent(out Character enemy) && CanHit(enemy)){
            if(!hitEnemy.Add(enemy))return;
            SoundPlayer.PlaySound(weapon.hitSound);
            if(hitEffect!=null)PlayOnce.Spawn(hitEffect, enemy.center, enemy.transform);
            enemy.recieveDamage(damage,weapon.isMagical);
            enemy.receiveKnockback(playerPos,knockback);
            if(weapon.maxHits>0 && weapon.maxHits<=++hitCount)Destroy(gameObject);
        }else if(!weapon.isMagical && other.TryGetComponent(out Destructable des)){
            des.recieveDamage(damage,weapon.isMagical);
            Destroy(gameObject);
        }
        if(weapon.destroyTimer==0)Destroy(gameObject);
    }
}
