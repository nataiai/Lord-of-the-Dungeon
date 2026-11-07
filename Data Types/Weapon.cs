using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public WeaponType type;
    public AudioClip useSound;
    public AudioClip hitSound;
    public bool isMagical;
    public int damage;
    public float cooldown;
    public float knocback;

    [Tooltip("How far in front of the player the hitbox spawns")]
    [FormerlySerializedAs("range")]
    public float hitBoxDistance;
    public HitBox hitBox;
    [Tooltip("Extra hitbox offset per facing direction (x = right, y = up)")]
    public Vector2 upOffset, rightOffset, downOffset, leftOffset;
    public Vector2 GetOffset(int facing) => new[]{upOffset, rightOffset, downOffset, leftOffset}[facing];
    [Tooltip("Hitbox flying speed. 0 = stays where it spawns")]
    public float velocity;
    [Tooltip("Seconds before the hitbox disappears. 0 = disappears on first touch (if it doesn't fly: right away, touching or not)")]
    public float destroyTimer;
    [Tooltip("Enemies it can hit before disappearing. 0 = unlimited")]
    [FormerlySerializedAs("destroyOnHit")]
    public int maxHits;
    public BaseWeapon anim;
    public Sprite uiWep;
    public BuffEffect.Buff buff; // Buff section: type none = no effect; copied to each hitbox (BuffEffect.cs)

    [Header("Throw: hold Throw (Q / gamepad X) + this weapon's button. Fist can't be thrown")]
    public int throwDamage = 1;          // per enemy hit, x the thrower's attack stat (like a swing)
    public float throwInitSpeed = 10;    // starting speed
    [Tooltip("Speed lost per second. 0 = flies until it hits something solid")]
    public float throwDeceleration = 8;
    [Tooltip("Radius of the thrown weapon: what it hits and bumps into")]
    public float throwSize = 0.25f;
    public float throwKnockback = 3;
    [Tooltip("Enemies it can hit before it stops (loyal: turns back). 0 = no limit")]
    public int throwMaxHits;
    [Tooltip("Bounces off solid things instead of stopping there")]
    public bool isBounce;
    [Range(0,1)] public float bounciness = 0.6f; // speed kept per bounce
    [Tooltip("Boomerang: when it stops, it flies back to the thrower through walls, into a fist hand")]
    public bool isLoyal;
    [Tooltip("Sprite spin, degrees per second at full speed (slows with it). 0 = flies point first")]
    public float spinSpeed;
    [Tooltip("Which way the sprite art points (0 = right, 90 = up), so a non-spinning throw flies point first")]
    public float spriteAngle = 45;

    [Header("Key: opens a Lock it hits, swung or thrown")]
    public bool isKey;
    [Tooltip("Default id for this key. Each key's Loot (or hand) can set its own, so one key weapon makes many keys. A Lock with an empty keyId takes any key")]
    public string keyId;
}
