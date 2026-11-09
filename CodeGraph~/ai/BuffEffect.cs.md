# BuffEffect.cs <!-- 1d4b4f -->
Buffs and debuffs (burn, heal, slow, stun) that a weapon passes to whoever its hitbox touches.

The buff travels: **Weapon** (its Buff section) → **HitBox** (copied in `HoldingWeapons.ForceUse`) → each **Character** the hitbox can hit (copied in `OnTriggerEnter2D`) → applied every frame until it runs out.

## BuffEffect <!-- 60bd7f -->
Carries a buff: spreads it from a hitbox, applies it on a character.
The same component does both jobs, depending on where it sits. `end` is set only when a buff is copied, so copies that were never handed on (summon templates) expire at once.

## BuffEffect.OnTriggerEnter2D <!-- 358ce8 -->
Only does something on a hitbox: each character the hitbox is allowed to hit (another team) gets a copy of the buff.

## BuffEffect.LateUpdate <!-- b00b0a -->
Only does something on a character: removes the buff when the character is dead or the time is up, otherwise applies it. It runs in LateUpdate, after the character set its own velocity, so the slow wins.

## BuffEffect.Apply <!-- 29a972 -->
Slow multiplies velocity by 0.9 every frame (each stacked copy slows again); stun keeps the character frozen. Burn and heal tick once a second, by `power` HP.

## BuffEffect.Buff <!-- 0d251f -->
The Buff section of a weapon: type, duration, power, stackable.
Serialized inside `Weapon`, so it is edited on the weapon asset. Type `none` means no buff.

## BuffEffect.Buff.CopyTo <!-- 40c4b3 -->
Puts this buff on a GameObject. If it is not stackable and the target already has the same buff, that one's timer restarts instead; otherwise a new BuffEffect is added that ends after `duration`.
