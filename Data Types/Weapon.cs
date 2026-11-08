using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Weapon")]
public partial class Weapon : ScriptableObject // Throw / Boomerang / Key settings: AI/Fields.cs
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
}
