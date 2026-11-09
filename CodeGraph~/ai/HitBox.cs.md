# HitBox.cs <!-- a302de -->
The area an attack spawns: it hurts what it touches, flies if the weapon says so, and disappears by time, by hits, or on touch.

## HitBox <!-- 58ceb3 -->
An attack's damage area: hurts other teams, breaks crates.
Spawned and filled in by `HoldingWeapons.ForceUse`. Hurts each target once (`hitEnemy`). With no owner it hurts everyone.

## HitBox.setVelocity <!-- ea5ab7 -->
Makes it fly at that velocity; 0 leaves it in place.

## HitBox.Start <!-- 4b0411 -->
An instant hit (no timer and no speed, like a sword swing) lasts two physics steps: long enough for physics to find what it overlaps, gone even if it touches nothing.

## HitBox.Update <!-- 5e708e -->
Disappears after the weapon's `destroyTimer`, if it has one.

## HitBox.OnTriggerEnter2D <!-- d50be5 -->
A character it may hit: hit sound, damage and knockback away from the user, once per character; gone after `maxHits`. A Destructable takes the hit from non-magic weapons and the hitbox is gone. With no timer, touching anything at all removes it.
