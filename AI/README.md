# AI folder: quick doc

Each file has an **ON/OFF line at the top** (`public static bool use`):

| File | `use = false` means |
|---|---|
| `PathFinding.cs` | enemies walk straight at the player (`useSeparation = false`: they may stack) |
| `ThrownWeapon.cs` | no throwing; the Throw button does nothing |
| `Yoink.cs` | dropped loot stays where it falls |
| `Lock.cs` | every Lock starts unlocked (no keys needed) |
| `UnifiedChat.cs` | chat uses Lexone TwitchChat instead |

`Slide.cs` is shared physics (slide, bounce, fly back, spin) and has no switch of its own.

## Weapon attributes (designers)

Throw: hold **Q** (keyboard) or **X** (gamepad), then press a weapon button.

| Attribute | What it does | Include / exclude |
|---|---|---|
| `canThrow` | the weapon can be thrown | untick to exclude this weapon from throwing |
| `throwDamage` | damage per enemy hit (× attack stat) | 0 = harmless throw |
| `throwInitSpeed` | starting speed | |
| `throwDeceleration` | speed lost per second | 0 = flies until it hits something solid |
| `throwSize` | radius of the flying weapon | |
| `throwKnockback` | knockback on enemies it hits | 0 = none |
| `throwMaxHits` | enemies it can hit before it stops | 0 = no limit |
| `isBounce` + `bounciness` | bounces off walls, keeping that much speed | off = stops at the first wall |
| `isLoyal` | boomerang: flies back through walls into a fist hand | off = lands where it stops |
| `spinSpeed` | sprite spin, degrees per second | 0 = flies point first |
| `spriteAngle` | which way the art points (0 = right, 90 = up) | only matters when `spinSpeed` is 0 |
| `isKey` | opens a Lock when swung at it or thrown into it | off = not a key |
| `keyId` | default key id | per key: set **Key Id** on its Loot, or **Weapon Loot Key Id** on the chest / enemy / crate that drops it |

Notes: fist (GameManager → **Fist**) is what a hand holds after a throw, and can't be thrown.
A thrown weapon lands as normal loot. Locks: add **Lock** to a door (put the door in **Hide On Unlock**) or a chest.
