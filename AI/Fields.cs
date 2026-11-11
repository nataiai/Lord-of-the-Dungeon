using UnityEngine;

// Fields the AI/ features add to existing classes. `partial`: these are the same classes, shown in the same Inspector,
// so the files outside AI/ only needed the word `partial`.

public partial class Experimental{
    [Header("Weapons")]
    [Tooltip("Off: no throwing, the Throw button (Q / gamepad X) does nothing (AI/ThrownWeapon.cs)")]
    public bool throwing = true;
    [Tooltip("Off: a Boomerang's attack flies like a normal shot instead of coming back (AI/Boomerang.cs)")]
    public bool boomerangReturns = true;
    [Tooltip("Off: dropped loot stays where it falls instead of sliding away (AI/Yoink.cs)")]
    public bool yoink = true;
    [Tooltip("Off: every Lock (doors too) starts unlocked, no keys needed (AI/Lock.cs)")]
    public bool locks = true;
}

public partial class Weapon{
    [Tooltip("Throw (AI/ThrownWeapon.cs): hold Throw (Q / gamepad X) + this weapon's button. Fist can't be thrown")]
    public ThrowSettings throwSettings = new();

    [Header("Boomerang (AI/Boomerang.cs): type Boomerang. Its attack flies out at `velocity` and comes back")]
    [Tooltip("Speed lost per second on the way out; at 0 (or at a wall) it flies back through walls, speeding up the same way")]
    public float boomerangDeceleration = 10;

    [Header("Key (AI/Lock.cs)")]
    [Tooltip("Not empty: this weapon is a key. It opens a Lock / Door / Chest with the same keyId, swung or thrown")]
    public string keyId;
    public bool isKey => !string.IsNullOrEmpty(keyId);
}

[System.Serializable]
public class ThrowSettings{ // Weapon.throwSettings: a foldout in the weapon's Inspector
    [Tooltip("Untick to keep this weapon from being thrown")]
    public bool canThrow = true;
    public int damage = 1;          // per enemy hit, x the thrower's attack stat (like a swing)
    public float initSpeed = 10;    // starting speed
    [Tooltip("Speed lost per second. 0 = flies until it hits something solid")]
    public float deceleration = 8;
    [Tooltip("Radius of the thrown weapon: what it hits and bumps into")]
    public float size = 0.25f;
    public float knockback = 3;
    [Tooltip("Enemies it can hit before it stops. 0 = no limit")]
    public int maxHits;
    [Tooltip("Bounces off solid things instead of stopping there")]
    public bool isBounce;
    [Range(0,1)] public float bounciness = 0.6f; // speed kept per bounce
    [Tooltip("Sprite spin, degrees per second at full speed (slows with it). 0 = flies point first. A boomerang spins with this too")]
    public float spinSpeed;
    [Tooltip("Which way the sprite art points (0 = right, 90 = up), so a non-spinning throw flies point first")]
    public float spriteAngle = 45;
}

public partial class HoldingWeapons{
    [System.NonSerialized] public Vector3 lootScale; // size of the loot it was picked up from: a throw keeps that size (zero = 1)
}
