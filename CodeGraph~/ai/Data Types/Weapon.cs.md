# Data Types/Weapon.cs <!-- 6f21d3 -->
The weapon asset (Create → Weapon): everything an attack needs, in Database/Weapons.

## Weapon <!-- 5d4a27 -->
One weapon: damage, cooldown, hitbox, effect, sounds, buff.
`hitBox` is the prefab spawned in front of the user, `hitBoxDistance` away plus a per-direction offset. `velocity` 0 keeps it where it spawned (sword); more makes it fly (arrow, rod shot). `destroyTimer` and `maxHits` decide when it disappears. `anim` is the effect animation played on the user.
