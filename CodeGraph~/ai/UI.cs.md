# UI.cs <!-- 630486 -->
The HUD: hearts, the two weapon icons with their reload, the summon cooldown, the enemy name text, and the dialogue box.

## UI <!-- 6b205e -->
HUD: hearts, weapons, reloads, name text, dialogue box.

## UI.Awake <!-- 4bdd1c -->
Collects the pieces from the hierarchy: hearts under the health bar, skill slots and their reload images, the text labels, and each weapon icon's Fill image.

## UI.Start <!-- 0b1089 -->
Draws the canvas through the UI camera and shows the weapon icons.

## UI.Update <!-- d8af7b -->
Each frame: weapon reload fills, the summon cooldown fill, and the dialogue.

## UI.ShowText <!-- 116f48 -->
Shows a text (an enemy's name when it is hit) for 2 seconds; a new text restarts the 2 seconds.

## UI.UpdateMaxHP <!-- a71d42 -->
Copies the first heart until there are enough, then shows exactly `maxHP` hearts (the first is always shown).

## UI.UpdateHP <!-- ee57a2 -->
Each heart is full or empty depending on HP.

## UI.UpdateSkill <!-- 33a333 -->
The first skill slot's reload fill shows the summon cooldown.

## UI.UpdateWeap <!-- f7dbc9 -->
Each hand's weapon icon, if the hand holds a weapon.

## UI.UpdateDialogue <!-- 10299a -->
While there are lines: the player is frozen, invincible and can't attack. The first line types itself out at `dialogueSpeed` letters per second. A left-attack press shows the whole line if it is still typing, otherwise goes to the next line, closing the box after the last.
