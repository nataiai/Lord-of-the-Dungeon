** KeyBronze keyId ("bronze") must match door/chest keyId **

# Weapons update: throwing, boomerang, keys, loot slide

>> All logic is in AI/. Outside it, files only got `partial` or a one-line hook (search "AIHooks.").
>> [TWEAK] = a setting designers can change.  [SETUP] = assign once or it won't work.

[SETUP]  Database/Resources/PrefabDatabase > fist = Database/Weapons/Fist  (keep it in a Resources folder)
[TWEAK]  Experimental > Weapons: throwing, boomerangReturns, yoink, locks  (untick = feature off)


Throw      hold Q / pad X + weapon button. Lands as loot; the hand goes back to Fist (Unequip).
[TWEAK]    Weapon > Throw Settings (foldout): initSpeed, knockback, canThrow, isBounce, and
             damage        x the weapon's damage (0.5 = half, 0 = none), x attack, rounded down
             deceleration  0 = flies until a wall
             size          radius it hits with
             maxHits       0 = no limit
             bounciness    speed kept per bounce
             spinSpeed     0 = flies point first (a boomerang spins with this too)
             spriteAngle   which way the art points (0 = right)
             shadowHeight  how high it flies above its shadow (0 = no shadow)

Charge     hold a weapon button: one normal attack, then it charges (player glows when ready). Let go: the skill.
[TWEAK]    Experimental > Player > chargeSkills (untick = feature off)
[SETUP]    Charge UI (off): give LeftWeapon / RightWeapon a "Charge" child Image (Filled, like "Fill"),
           then set showCharge = true in UI.cs
[TWEAK]    Weapon > Charge Settings (foldout): autoAttack (holding keeps attacking, the old way), chargeTime,
             skill   Multi (normal attacks in a row, the default; interval 0 = one per cooldown) / Big (one big hit) / Spin (hits all around) / Volley (fan of shots) / Flurry (quick hits) / Lunge (dash, then hit)
             damage  x the weapon's damage;  count, interval, spread, size, dashSpeed  show only for the skills that use them
             Multi   noKnockback (hits don't push back), stun (stunned until the next hit): ticks
           Set now: Fist, KeyBronze, GameBreaker autoAttack. Sword1 Spin, Zweihander Big, Rapier Flurry,
           Spear1 Lunge, Arrow Volley, RodFire1 Big, RodIce1 Spin.

Chat       no internet, a drop or Twitch down never stops the game: chat tries again by itself (about every 20 s).
           Quiet for 10 s: it pings Twitch; no answer for 20 s: it reconnects (Lexone, Twitch, WebGL, YouTube).
[SETUP]    IRC object (Lexone): useAnonymousLogin on (or oauth + username), joinChannelOnStart on.
[TWEAK]    IRC object: untick connectIRCOnStart, or the game can freeze at load when Twitch can't be reached.

Boomerang  a weapon of type Boomerang: its attack comes back through walls. The hand waits until it's back.
[TWEAK]    Weapon > boomerangDeceleration  slow-down going out

Keys       >> a weapon with a keyId is a key. A Door / Chest / Lock with a keyId is locked.
           The key with the same keyId opens it, swung or thrown.
[TWEAK]    keyId on the weapon and on the Door / Chest;  consumeKey = key is used up (default off)

Loot       dropped loot slides and bounces (placed loot stays put; still falls in Holes).
[TWEAK]    in code: LootYoink.speed / deceleration / bounciness (AI/Yoink.cs)

PlayOnce   AI/PlayOnce.cs: play an animation once, then hide / destroy. Use it anywhere.
           Door opens with it (plays once, then gone); the heal effect lasts as long as its animation.


# Dev (code only)

ShowIf     AI/ShowIf.cs: [ShowIf("skill", WeaponSkill.Big)] on a field = Inspector shows it only for those values. Use it anywhere.
