# DestructableObj.cs <!-- 14d9e2 -->
Breakable objects such as crates and pots. The file is DestructableObj.cs but the class is `Destructable`; Unity normally wants a MonoBehaviour's file and class names to match.

## Destructable <!-- 3b8e85 -->
Breakable object: changes sprite per hit, can drop loot.

## Destructable.Died <!-- 6e1571 -->
Tells pathfinding the way may be open now, removes the object if `destroyAfterDead`, and drops its loot.

## Destructable.recieveDamage <!-- 094056 -->
Magic does nothing; any other hit counts as one, whatever its damage. Each hit shows the next sprite, and it breaks after `hitUntillBreak` hits.
