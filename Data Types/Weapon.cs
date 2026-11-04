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
    public float knocback;

    [FormerlySerializedAs("range")] public float hitBoxDistance; // how far in front of the player the hitbox spawns
    public HitBox hitBox;
    public float velocity;
    public float destroyTimer;
    [FormerlySerializedAs("destroyOnHit")] public int maxHits; // hits before the hitbox is destroyed, 0 = unlimited
    public BaseWeapon anim;
    public Sprite uiWep;

}