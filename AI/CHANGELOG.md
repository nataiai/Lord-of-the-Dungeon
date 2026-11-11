# Weapons update: throwing, boomerang, keys & locks, loot slide

Changes from `main`. All new logic is in `AI/`; files outside it only got the word `partial` or a one-line hook (search `AIHooks.`).

## On/off switches (Experimental → Weapons)

| Setting | Off means |
|---|---|
| throwing | Throw button does nothing |
| boomerangReturns | Boomerang attack flies like a normal shot |
| yoink | dropped loot stays where it falls |
| locks | every Lock starts unlocked |

## Throwing (`AI/ThrownWeapon.cs`)

Hold **Q / gamepad X** (`Controller.throwHold`) + a weapon button. The weapon flies, hurts enemies, and lands as loot. The hand calls `Unequip()` (back to Fist, from `Database/Resources/PrefabDatabase`). Fist can't be thrown.

| Weapon attribute | |
|---|---|
| canThrow, throwDamage, throwInitSpeed, throwKnockback, isBounce | |
| throwDeceleration | speed lost per second. 0 = flies until it hits something solid |
| throwSize | radius of the flying weapon |
| throwMaxHits | enemies before it stops. 0 = no limit |
| bounciness | speed kept per bounce (with isBounce) |
| spinSpeed | degrees per second. 0 = flies point first |
| spriteAngle | which way the art points (0 = right, 90 = up), for point-first |

## Boomerang (`AI/Boomerang.cs`)

A weapon of type **Boomerang**: its normal attack flies out at `velocity`, slows down (or stops at a wall), then flies back through walls. That hand stays on cooldown until it's back.

| Weapon attribute | |
|---|---|
| boomerangDeceleration | speed lost per second going out; it speeds up the same way coming back |

## Keys & locks (`AI/Lock.cs`)

A key is any weapon with **isKey**. It opens a **Lock** by swinging at it or by being thrown into it. A locked Chest ignores hits until a key opens it. Door: put the door object in `hideOnUnlock`.

| Attribute | |
|---|---|
| Weapon: isKey | |
| Weapon: keyId | opens Locks with the same keyId. One key weapon per id |
| Lock: isLocked, unlockSound, onUnlock | |
| Lock: keyId | empty = any key opens it |
| Lock: consumeKey | the key is used up |
| Lock: hideOnUnlock | objects hidden when it opens (e.g. the closed door) |

## Loot slide (`AI/Yoink.cs`)

Dropped loot (enemy, chest, crate, swapped-out weapon) slides away and bounces off walls. Loot placed in a level stays put. It still falls into Holes.
Tweak in code: `LootYoink.speed`, `deceleration` (per second), `bounciness` (speed kept per bounce).

## Other

- Thrown weapons keep the size they had as loot.
- `AI/Slide.cs`: shared slide / bounce / fly-back / spin physics, reusable.
- `Door.OpenDoor()`, `PrefabDatabase` (holds Fist), `HoldingWeapons.Unequip()`.
