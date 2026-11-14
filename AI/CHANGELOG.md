** KeyBronze keyId ("bronze") must match door/chest keyId **

# Weapons update: throwing, boomerang, keys, loot slide

>> Fields and small hooks sit next to code like it (UI in UI.cs, summon fields in SummonDB.cs); the logic is in AI/.
>> [TWEAK] = a setting designers can change.  [SETUP] = assign once or it won't work.

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
             Spin    spinCount (8 = every 45 degrees, 3 = every 120), spinAnim: the facing swing anim, turned toward every hit (default on),
                     spinRandom: the first hit in front, the others in random directions
           Set now: Fist, KeyBronze, GameBreaker autoAttack. Sword1 Spin, Zweihander Big, Rapier Flurry,
           Spear1 Lunge, Arrow Volley, RodFire1 Big, RodIce1 Spin.

Hit effect Weapon > hitEffect: a prefab made on each enemy it hits (swung, shot, thrown), plays its animation once.
           Each weapon its own prefab (or prefab variant) = its own hit animation.

Chat       no internet, a drop or Twitch down never stops the game: chat tries again by itself (about every 20 s).
           Quiet for 10 s: it pings Twitch; no answer for 20 s: it reconnects (Lexone, Twitch, WebGL, YouTube).
[TWEAK]    IRC object: untick connectIRCOnStart, or the game can freeze at load when Twitch can't be reached.

Buffs      Weapon > Buff > Type is a mask: tick several at once (burn + slow ...). power = burn / heal HP per second

Boomerang  a weapon of type Boomerang: its attack comes back through walls. The hand waits until it's back.
[TWEAK]    Weapon > boomerangDeceleration  slow-down going out

Keys       >> a weapon with a keyId is a key. A Door / Chest / Lock with a keyId is locked.
           The key with the same keyId opens it, swung or thrown.
[TWEAK]    keyId on the weapon and on the Door / Chest;  consumeKey = key is used up (default off)

Loot       dropped loot slides and bounces (placed loot stays put; still falls in Holes).
[TWEAK]    in code: LootYoink.speed / deceleration / bounciness (AI/Yoink.cs)

PlayOnce   AI/PlayOnce.cs: play an animation once, then hide / destroy. Use it anywhere.
           Door opens with it (plays once, then gone); the heal effect lasts as long as its animation.

Summon     each cooldown, the chatter with the fewest turns goes first, then the oldest request (AI/ChatPotion.cs).
           e.g. A A . A B . A B . A B  (. = cooldown done)  spawns  A A B B A ...  One spammer can't make the others wait.
           A new room clears the summon and potion lines (the spawn counts stay).

Potions    chat types "potion" (the first one) or "potion health" (how its name starts): it appears near the player.
           A room gets a few, ever (coming back doesn't refill). Full: the request is dropped.
[SETUP]    TwitchChat > Summondb > Potions: Loot prefabs (Loot Type = Instant Item, Instant Item = HealthBone, a sprite).
           It slides out like dropped loot
[TWEAK]    Summondb > Potions Per Room (3)
           UI: skill slot 2 (the 2nd child of Skill Slot) shows the potion cooldown, like slot 1 shows summon
[TWEAK]    Experimental > Chat > potionSamePool
             off  own line, Summondb > Potion Skill > Cooldown (10)
             on   waits in the summon line with the enemies, uses Spawn Skill's cooldown


# Dev (code only)

ShowIf     AI/ShowIf.cs: [ShowIf("skill", WeaponSkill.Big)] on a field = Inspector shows it only for those values. Use it anywhere.
           Several on one field: all must match. Experimental > Inspector > useShowIf off: every field shows.

Chat skill a new one: a ChatType at the END of the enum (saved as numbers), a row in Chat.commands,
           a case in Chat.addToDB. Queued ones: like potions (AI/ChatPotion.cs AddPotion / GetPotionToSpawn).
