# Map/Chest.cs <!-- d547ab -->
A chest is an enemy that never moves or attacks: any hit opens it and it drops its loot.

## Chest <!-- b9caf8 -->
An "enemy" that opens when hit and drops its loot.
Being an Enemy gives it HP, loot and the hit handling for free. It overrides the parts that don't fit: no touch damage, no hurt color, no facing, never destroyed. PathFinding treats chests as walls.

## Chest.Died <!-- 8062f8 -->
Called every frame once HP is 0, so only the first call opens it: open sound, loot, open sprite. The enemy lifespan timeout also calls it, but a chest only opens when it was really hit.
