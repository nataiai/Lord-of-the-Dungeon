// [System.Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "WeaponSO",
    menuName = "Weapon"
)]
public class Weapon : ScriptableObject
{
    public WeaponType type;
    public AudioClip useSound;
    public AudioClip hitSound;
    public bool isMagical;
    public int damage;
    public float cooldown;
    [FormerlySerializedAs("knocback")] public float knockback;

    [FormerlySerializedAs("range")] public float hitBoxDistance; // how far in front of the player the hitbox spawns
    public HitBox hitBox;
    [FormerlySerializedAs("velocity")] public float projectileSpeed; // 0 = hitbox stays in place
    [FormerlySerializedAs("destroyTimer")] public float hitBoxLifetime; // 0 = destroyed on first touch
    [FormerlySerializedAs("destroyOnHit")] public int maxHits; // 0 = unlimited
    [FormerlySerializedAs("anim")] public BaseWeapon animPrefab;
    [FormerlySerializedAs("uiWep")] public Sprite icon;

}