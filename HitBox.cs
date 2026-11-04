using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour // spawned by Weapon.Use, hit rules are in Weapon.OnHit
{

    public Weapon weapon;
    public int damage;
    public Vector2 playerPos;
    public HashSet<Enemy> hitEnemy = new();
    [System.NonSerialized] public int hitCount;

    void OnTriggerEnter2D(Collider2D other) => weapon.OnHit(this, other);
}
