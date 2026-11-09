# Map/EventAllEnemiesDefeated.cs <!-- fd08e2 -->
Fires a UnityEvent once, when no enemy is left (for example to open the doors).

## EventAllEnemiesDefeated <!-- e59243 -->
Runs its event once no enemy is left.
What happens is set in the Inspector (`onAllEnemiesDefeated`), so it is not visible in code.

## EventAllEnemiesDefeated.Update <!-- 4185eb -->
When the condition is met: runs the event, tells pathfinding that doors may have opened, plays the door sound, and removes itself so it fires only once.

## EventAllEnemiesDefeated.IsConditionTrue <!-- 122ed4 -->
Cheap check first: while the enemy it found last time is still alive, the answer is no. Otherwise it searches the scene again, which also finds new summons.
