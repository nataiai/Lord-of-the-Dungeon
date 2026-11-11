# Weapons update: throwing, boomerang, keys & locks, loot slide

Changes from main. All new logic is in AI/. Files outside it only got the word
`partial` or a one-line hook (search "AIHooks." to find them).


## On/off switches  (Experimental > Weapons)

    throwing          off: the Throw button does nothing
    boomerangReturns  off: a Boomerang attack flies like a normal shot
    yoink             off: dropped loot stays where it falls
    locks             off: every Lock starts unlocked


## Throwing  (AI/ThrownWeapon.cs)

Hold Q / gamepad X (Controller.throwHold) + a weapon button. The weapon flies,
hurts enemies and lands as loot. The hand calls Unequip() (back to Fist, from
Database/Resources/PrefabDatabase). Fist can't be thrown.

Weapon:
    canThrow, throwDamage, throwInitSpeed, throwKnockback, isBounce
    throwDeceleration  speed lost per second (0 = flies until it hits a wall)
    throwSize          radius of the flying weapon
    throwMaxHits       enemies before it stops (0 = no limit)
    bounciness         speed kept per bounce (with isBounce)
    spinSpeed          degrees per second (0 = flies point first)
    spriteAngle        which way the art points (0 = right, 90 = up)


## Boomerang  (AI/Boomerang.cs)

A weapon of type Boomerang: its normal attack flies out at `velocity`, slows
down (or stops at a wall), then flies back through walls. That hand stays on
cooldown until it's back.

Weapon:
    boomerangDeceleration  speed lost per second going out; speeds up the same coming back


## Keys & locks  (AI/Lock.cs)

A key is any weapon with isKey. It opens a Lock when swung at it or thrown into
it. A locked Chest ignores hits until a key opens it.
Door: add Lock and put the door object in hideOnUnlock.

Weapon:
    isKey
    keyId         opens Locks with the same keyId (one key weapon per id)
Lock:
    isLocked, unlockSound, onUnlock
    keyId         empty = any key opens it
    consumeKey    the key is used up
    hideOnUnlock  objects hidden when it opens (e.g. the closed door)


## Loot slide  (AI/Yoink.cs)

Dropped loot (enemy, chest, crate, swapped-out weapon) slides away and bounces
off walls. Loot placed in a level stays put, and loot still falls into Holes.
Tweak in code:
    LootYoink.speed, deceleration (per second), bounciness (speed kept per bounce)


## Other

- Thrown weapons keep the size they had as loot.
- AI/Slide.cs: shared slide / bounce / fly-back / spin physics, reusable.
- Door.OpenDoor(), PrefabDatabase (holds Fist), HoldingWeapons.Unequip().
