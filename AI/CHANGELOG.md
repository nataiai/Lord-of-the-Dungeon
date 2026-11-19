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
           stopAnim: the target's animation stops. Effect: a prefab shown on the target while the buff lasts (loop it)

Boomerang  a weapon of type Boomerang: its attack comes back through walls. The hand waits until it's back.
[TWEAK]    Weapon > boomerangDeceleration  slow-down going out

Keys       >> a weapon with a keyId is a key. A Door / Chest / Lock with a keyId is locked.
           The key with the same keyId opens it, swung or thrown.
[TWEAK]    keyId on the weapon and on the Door / Chest;  consumeKey = key is used up (default off)

Loot       dropped loot slides and bounces (placed loot stays put; still falls in Holes).
           Loot from a chest slides out of it.
[TWEAK]    in code: LootYoink.speed / deceleration / bounciness (AI/Yoink.cs)

PlayOnce   AI/PlayOnce.cs: play an animation once, then hide / destroy. Use it anywhere.
           Door opens with it (plays once, then gone); the heal effect lasts as long as its animation.

Enemies    Experimental > Enemy reset, while you're out of their room (worked out when you come back):
             resetPosition  back to where they started: walkBack (along the path, as far as their speed gets them) or straight there
             resetHealth    heal enemyRegen HP per second (0 = back to full)
             forgetPlayer, clearBuffs, pauseLifeSpan (their 24 hour life span stops)

Mini map   top right all the time. M (gamepad Select): the big map, moved with the move keys / stick (player can't move or attack).
           Shows rooms you've been in (the one you're in brighter), doors (yellow = leads to a room you haven't been in),
           a ? just past each door to a room you haven't entered, and you (red). AI/MiniMap.cs, made by UI.Start.
           The room you're in is filmed live (enemies, you); a room you left keeps its last picture.
[TWEAK]    Experimental > miniMap (off: no corner map, M still works), bigMapPauses (on: the big map pauses the game). In Play mode: UI > MiniMap scales, colors, panSpeed

Chat buffs chat types berserk / invincible / magicshield / physicalshield / floating (3+ letters, "ber" works): the buff goes on the player.
             berserk     attack x2 at full HP up to x4, no healing (15 s). The player looks red (for now; nicer: an aura prefab in Berserk > Buff > Effect)
             invincible  no damage, no knockback (10 s)
             shields     block magic / physical damage (15 s). Enemy data > isMagical: its touch is magic (Ghost)
             floating    holes don't take the player (10 s); if it ends over a hole, the player falls.
                         The player floats up (Summondb > Floating > Height, Rise Time), a shadow stays below like a
                         thrown weapon's, and lands as it ends. Friction: how fast knockback slows down in the air
                         (1 = like the ground, 0 = slides on)
           earthquake / quake: every enemy of the room and the player are stunned (3 s) and shaken: pushed all at once,
             each a random way, 30 times in 1.5 s. Not the player while invincible (buff, or the frames after a hit)
             or floating. No enemies: no cast. An enemy that hasn't seen you walks back to where it was
           A vote: it casts when need chatters typed the word within 20 s of the first one. Typing it again does nothing.
             need = crowdNeed x sqrt(chatters with a command in the last 2 min), at least 1 (1-2: 1, 3-6: 2, 7-12: 3, 100: 10)
             Time up: nothing, no cooldown used. On cooldown: no votes. Several skills can be voted at once
[TWEAK]    Summondb > Berserk / Invincible / Magic Shield / Physical Shield / Floating / Earthquake: Cooldown,
             Buff (duration, effect prefab). Earthquake: Knockback, Shakes, Shake Time too
             For testing now: duration 3 (floating 5), cooldown 5; then the times above, cooldown 60 (floating 30, earthquake 45)
[TWEAK]    Experimental > Chat > chatSkills (off: the words do nothing), crowdNeed (1; 0 = one chatter is enough),
             skillKeys: keys 1-6 cast them (test, one voice: "keys")
[SETUP]    UI > Chat Text: a TMP text. It shows the votes ("berserk 1/2 +Ann 9s") and what chat did ("Ann +1: berserk!")
           UI: skill slots 3 on (children of Skill Slot, like 1 and 2) show the chat skills' cooldowns, in Summondb order

Summon     each cooldown, the chatter with the fewest turns goes first, then the oldest request (ChatLine in Connector/SummonDB.cs).
           e.g. A A . A B . A B . A B  (. = cooldown done)  spawns  A A B B A ...  One spammer can't make the others wait.
           A new room clears the summon and potion lines (the spawn counts stay).
[TWEAK]    Summondb (and > Potion) > Limit Per Player (10 waiting each), Limit Total (300 waiting): more are turned away

Potions    chat types "potion" (the first one) or "potion health" (how its name starts): it appears near the player.
           A room gets a few, ever (coming back doesn't refill). Full: the request is dropped.
[SETUP]    TwitchChat > Summondb > Potion > Potions: Loot prefabs (Loot Type = Instant Item, Instant Item = HealthBone, a sprite).
           It slides out like dropped loot
[TWEAK]    Summondb > Potion > Potions Per Room (3)
           UI: skill slot 2 (the 2nd child of Skill Slot) shows the potion cooldown, like slot 1 shows summon
[TWEAK]    Experimental > Chat > potionSamePool
             off  own line, Summondb > Potion > Spawn Skill > Cooldown (5 for testing, then 10)
             on   waits in the summon line with the enemies, uses Spawn Skill's cooldown

Weapons    chat votes the player's right hand: "vote 2" or "vote spear" (again = move your vote), 30 s, while you're in the room.
room       Time up: the streamer's vote is last (the spot they stand at). Tie or no votes: random. The winner stays in the
           right hand (no swap, no throw) until the next weapons room, also through death. The old one drops on its spot.
           UI > Chat Text shows it: RIGHT HAND  1 sword 4 · 2 spear 2  22s
[SETUP]    DungeonRoom > Room Type = weapons, then its Weapon Room: Weapons (not keys), Spots (optional: a SpriteRenderer per
           weapon, on pedestal art with its number), On Voted (e.g. the exit Door's OpenDoor). Music: SoundPlayer's lists
[TWEAK]    Weapon Room > Vote > Method: plurality (the most votes) or runoff (ranked "vote 2 3": the fewest drop out until
           one has over half), Time (30)


# Dev (code only)

ShowIf     AI/ShowIf.cs: [ShowIf("skill", WeaponSkill.Big)] on a field = Inspector shows it only for those values. Use it anywhere.
           Several on one field: all must match. Experimental > Inspector > useShowIf off: every field shows.

Chat skill a new one: a ChatType at the END of the enum (saved as numbers), a row in Chat.commands,
           a case in Chat.addToDB. Queued ones: a ChatLine child, like AI/ChatPotion.cs.
           Voted ones: a ChatSkill child (AI/ChatSkill.cs: Set() in its constructor, override Cast for more than its buff),
           a Summondb field, in Summondb.skills. No case in addToDB: ChatSkill.Read votes.

Sprite     Character.bodySprite: the SpriteRenderer you see (it skips empty ones). Use it, not GetComponentInChildren.
Vote       AI/ChatVote.cs: every chat vote (skills, weapons room). Open(title, options, need, onEnd), Ballot, Winner by
           Method (new methods: VoteMethod at the end + a case in Winner). UI: ChatVote.open, each vote's fields or Text().
