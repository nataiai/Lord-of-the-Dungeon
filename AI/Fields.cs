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
    // damage x times x the user's attack stat, rounded down: swings, charge skills, throws
    public int Damage(Character by, float times=1) => (int)(damage*times*(isMagical ? by.combatStat.attackMagic : by.combatStat.attackPhysic));

    [Tooltip("Charge skill (AI/WeaponCharge.cs): hold this weapon's button, let go to use the skill")]
    public ChargeSettings chargeSettings = new();
}

[System.Serializable]
public class ChargeSettings{ // Weapon.chargeSettings
    [Tooltip("On: holding the button keeps attacking (no charge skill)")]
    public bool autoAttack;
    [Tooltip("Seconds to hold before the skill is ready (the player glows)")]
    public float chargeTime = 1;
    public WeaponSkill skill = WeaponSkill.Multi;
    [ShowIf("skill", WeaponSkill.Big, WeaponSkill.Spin, WeaponSkill.Volley, WeaponSkill.Flurry, WeaponSkill.Lunge)]
    [Tooltip("Damage per hit, x the weapon's damage")]
    public float damage = 2;
    [ShowIf("skill", WeaponSkill.Spin, WeaponSkill.Volley, WeaponSkill.Flurry, WeaponSkill.Multi)]
    [Tooltip("Hits for Spin / Volley / Flurry, attacks for Multi")]
    public int count = 2;
    [ShowIf("skill", WeaponSkill.Multi)]
    [Tooltip("Multi: seconds between attacks, after the weapon's cooldown. 0 = right after, below 0 = sooner")]
    public float interval;
    [ShowIf("skill", WeaponSkill.Multi)]
    [Tooltip("Multi: its hits don't push enemies back, so the next hit lands too")]
    public bool noKnockback;
    [ShowIf("skill", WeaponSkill.Multi)]
    [Tooltip("Multi: an enemy it hits is stunned until the next hit (not with a Boomerang)")]
    public bool stun;
    [ShowIf("skill", WeaponSkill.Volley)]
    [Tooltip("Volley: degrees between shots")]
    public float spread = 20;
    [ShowIf("skill", WeaponSkill.Big)]
    [Tooltip("Big: hitbox size, x normal")]
    public float size = 2;
    [ShowIf("skill", WeaponSkill.Lunge)]
    [Tooltip("Lunge: dash speed")]
    public float dashSpeed = 10;
}

[System.Serializable]
public class ThrowSettings{ // Weapon.throwSettings: a foldout in the weapon's Inspector
    [Tooltip("Untick to keep this weapon from being thrown")]
    public bool canThrow = true;
    [Tooltip("x the weapon's damage: 1 = same as a swing, 0.5 = half, 0 = none. Then x the thrower's attack, rounded down (can be 0)")]
    public float damage=.5f;
    public float initSpeed = 10;    // starting speed
    [Tooltip("Speed lost per second. 0 = flies until it hits something solid")]
    public float deceleration;
    [Tooltip("Radius of the thrown weapon: what it hits and bumps into")]
    public float size = 0.25f;
    public float knockback = 3;
    [Tooltip("Enemies it can hit before it stops. 0 = no limit")]
    public int maxHits;
    [Tooltip("Bounces off solid things instead of stopping there")]
    public bool isBounce;
    [Range(0,1)] public float bounciness = 0.1f; // speed kept per bounce
    [Tooltip("Sprite spin, degrees per second at full speed (slows with it). 0 = flies point first. A boomerang spins with this too")]
    public float spinSpeed;
    [Tooltip("Which way the sprite art points (0 = right, 90 = up), so a non-spinning throw flies point first")]
    public float spriteAngle = 45;
    [Tooltip("How high it flies above its shadow. 0 = no shadow")]
    public float shadowHeight = 0.5f;
}

public partial class HoldingWeapons{
    [System.NonSerialized] public Vector3 lootScale; // size of the loot it was picked up from: a throw keeps that size (zero = 1)
    [System.NonSerialized] public bool charging;
    [System.NonSerialized] public float chargeStart;
}
