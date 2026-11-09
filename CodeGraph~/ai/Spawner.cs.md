# Spawner.cs <!-- fa2c82 -->
Sits on a room's floor tilemap: measures the room and finds free spots to spawn enemies on.

## Spawner <!-- d7d7b9 -->
A room's floor: its bounds, and free spots to spawn.
`bounds` is the room's area for everything else too (`DungeonRoom.bounds`, pathfinding limits).

## Spawner.Awake <!-- a9180d -->
Shrinks the tilemap to its painted tiles and takes the renderer's bounds as the room's area.

## Spawner.Spawn(Enemy,Vector2,float) <!-- 3a3ad5 -->
Searches outward from a point on a 0.5 grid (the same grid as PathFinding) over free cells inside the room, up to `range` away, and spawns the enemy on a random one more than 0.5 away. With no free cell it spawns on the point itself.
