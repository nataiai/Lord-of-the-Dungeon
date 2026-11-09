# Loot.cs <!-- 4c6920 -->
Pickups on the floor: weapons, chat skills and instant items, and the code that drops them.

## Loot <!-- 65cec7 -->
A pickup: weapon, chat skill or instant item.
Dropped by enemies, chests and crates (`Drop`). A weapon pickup can be swapped with a full hand.

## Loot.Start <!-- 3f4785 -->
Makes sure it can be seen and touched: adds a sprite renderer and a trigger collider if missing (the collider after the sprite, so it fits it).

## Loot.UpdateImage <!-- 83b2ca -->
A weapon pickup shows the weapon's icon.

## Loot.OnTriggerEnter2D <!-- 59c784 -->
The player steps on it: it is picked up the way its type says, and the weapon icons in the UI refresh.

## Loot.PickWeaponHandle <!-- 32e05c -->
Fills the left hand, then the right (bare hands count as empty). The first weapon picked up also joins chat. With both hands full, hold that hand's attack button while stepping on it to swap.

## Loot.TryFill <!-- 263980 -->
Puts the weapon in an empty or bare hand, plays the pickup sound and removes the pickup.

## Loot.PickChatSkillHandle <!-- 16bcbe -->
Adds the chat skill to the available list (once) and removes the pickup. Nothing reads `availableSkill` yet.

## Loot.Drop <!-- 157775 -->
Drops a Loot prefab and/or a bare weapon at a position. A weapon gets a pickup made in code: its icon drawn just above the dropper's sprite, a trigger box, and a Loot of type weapon.
