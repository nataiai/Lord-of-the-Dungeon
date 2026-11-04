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

    [Tooltip("How far in front of the player the hitbox spawns")]
    [FormerlySerializedAs("range")]
    public float hitBoxDistance;
    public HitBox hitBox;
    [Tooltip("Optional sprite for the hitbox (e.g. arrow). Empty = keep the prefab's sprite")]
    public Sprite hitBoxSprite;
    [Header("Hitbox per facing direction (x = right, y = up)")]
    public HitBoxSide up;
    public HitBoxSide right, down, left;
    public HitBoxSide GetSide(int facing) => new[]{up, right, down, left}[facing];
    public Vector2 HitBoxPos(Vector2 playerPos, int facing) =>
        playerPos + new Vector2(0,0.5f) + Direction.vec2MoveDict[facing] * hitBoxDistance + GetSide(facing).offset;
    [Tooltip("Hitbox flying speed. 0 = stays where it spawns")]
    public float velocity;
    [Tooltip("Seconds before the hitbox disappears. 0 = disappears on first touch")]
    public float destroyTimer;
    [Tooltip("Enemies it can hit before disappearing. 0 = unlimited")]
    [FormerlySerializedAs("destroyOnHit")]
    public int maxHits;
    public BaseWeapon anim;
    [Tooltip("Weapon effect clip per facing: up, right, down, left. Empty = use the anim prefab's own clips")]
    public AnimationClip[] animations;
    public Sprite uiWep;

}

[System.Serializable]
public class HitBoxSide
{
    [Tooltip("Extra hitbox spawn offset")]
    public Vector2 offset;
    [Tooltip("Optional hitbox shape, 3+ points around the spawn point. Empty = use the prefab's collider")]
    public Vector2[] shape;
}
