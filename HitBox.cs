using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    public Weapon weapon;
    public int damage;
    public Vector2 playerPos;
    public float startTime;
    public HashSet<Enemy> hitEnemy = new();
    private Rigidbody2D rb;
    int hitCount;

    public void setVelocity(Vector2 v){
        if(v.magnitude==0)return;
        if(!rb)rb = GetComponent<Rigidbody2D>();
        rb.velocity = v;
    }
    void Start(){startTime = Time.time;}
    void Update(){if(weapon.destroyTimer!=0 && Time.time-startTime>weapon.destroyTimer)Destroy(gameObject);}

    void OnTriggerEnter2D(Collider2D other){
        if(other.TryGetComponent(out Enemy enemy)){
            if(!hitEnemy.Add(enemy))return; // each enemy once
            enemy.recieveDamage(damage,weapon.isMagical);
            enemy.receiveKnockback(playerPos,weapon.knocback);
            if(weapon.maxHits>0 && weapon.maxHits<=++hitCount)Destroy(gameObject);
        }else if(!weapon.isMagical && other.TryGetComponent(out Destructable des)){
            des.recieveDamage(damage,weapon.isMagical);
            Destroy(gameObject);
        }
        if(weapon.destroyTimer==0)Destroy(gameObject);
    }
}
