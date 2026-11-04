using UnityEngine;
using UnityEngine.Serialization;

// Weapon data only. Logic: WeaponLogic.cs. Inspector headers, tooltips, hiding: Editor/WeaponEditor.cs
// FormerlySerializedAs = old field name, keeps values already saved in assets
[CreateAssetMenu(fileName = "WeaponSO", menuName = "Weapon")]
public partial class Weapon : ScriptableObject
{
    // Weapon
    public WeaponType type;
    public Sprite uiWep;
    public bool isMagical;
    public int damage;
    public float cooldown;
    public float knocback;
    public AudioClip useSound;
    public AudioClip hitSound;

    // Hitbox
    [FormerlySerializedAs("hitBox")] public HitBox hitBoxPrefab;
    public float hitBoxLength;
    public float hitBoxWidth;
    public Sprite hitBoxSprite;
    public LayerMask hitBoxLayer;
    [FormerlySerializedAs("range")] public float hitBoxDistance;
    public float velocity;
    public float destroyTimer;
    [FormerlySerializedAs("destroyOnHit")] public int maxHits;
    public HitBoxSide up, right, down, left;

    // Effect
    public Animator effectPrefab;
    public RuntimeAnimatorController effectController;
    public AnimationClip[] animations;
    public int effectOrder = 1;
}

[System.Serializable]
public class HitBoxSide
{
    public Vector2 offset;
    public Vector2[] shape;
}
