# Character.cs <!-- 0d85a0 -->
What the player and every enemy have in common: health, movement, facing, teams, knockback, stun and healing.

## CombatStat <!-- 95dc9e -->
Attack and defense numbers, physical and magic.
Weapons multiply their damage by the attack stat; enemies subtract their defense stat from damage taken.

## Character <!-- af1e67 -->
Base of Player and Enemy: HP, movement, team, hits.
Abstract: each subclass says how much HP it has (`maxHP`) and its combat stats. `team` decides who can hurt whom: hitboxes and touch damage only hurt other teams. `recieveDamage` does nothing here; Player, Enemy and Chest override it.

## Character.bodyCollider.get <!-- fdc69a -->
Finds the character's collider the first time it is asked for, then keeps it.

## Character._setAnimFacing <!-- 87638d -->
Turns a direction into the facing number used by the animations: right or left when it points more sideways than 45°, otherwise up or down, and down when there is no direction.

## Character.StuntMech <!-- 4cc5a2 -->
While stunned (after a knockback or a freeze) the character slides to a stop and ignores its own movement: callers skip moving when it returns true. The stun ends early once it is nearly still.

## Character.Heal <!-- fd16e7 -->
Adds HP, never above `maxHP`.

## Character.HealWithEffect <!-- ee15f8 -->
Heals and plays GameManager's heal animation on the character for half a second. Used by the healing room and the Health Bone.
