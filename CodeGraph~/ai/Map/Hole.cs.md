# Map/Hole.cs <!-- 6abbc5 -->
A hole in the floor: the player, enemies and loot fall in. Enemies and loot are gone; the player comes back where they entered the room. Flying enemies pass over.

## Hole <!-- 7415dc -->
Things fall in: enemies and loot vanish, the player respawns.
PathFinding steers walking enemies around holes, so mostly knocked-back enemies fall in.

## Hole.Awake <!-- 863edb -->
Gives the hole a kinematic Rigidbody2D, so loot (which has none) still triggers it.

## Hole.OnTriggerEnter2D <!-- 8d56f7 -->
A character or loot touches the hole: it falls, unless it is already falling (disabled) or is a flying enemy.

## Hole.Fall <!-- 854190 -->
A coroutine: the faller stops and can't act. It plays its fall clip if it has one, else shrinks away over half a second. Enemies and loot are then destroyed; the player gets its size back, respawns at `roomEntryPos` and can move again.
