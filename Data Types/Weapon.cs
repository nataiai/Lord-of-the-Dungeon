// [System.Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public float range;
    public HitBox hitBox;
    public float velocity;
    public float destroyTimer;
    public int destroyOnHit;
    public BaseWeapon anim;
    public Sprite uiWep;

}