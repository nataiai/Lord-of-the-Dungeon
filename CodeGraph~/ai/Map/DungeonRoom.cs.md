# Map/DungeonRoom.cs <!-- 386dba -->
One room: its area (from its floor's Spawner), its type (which picks the music), and spawning enemies into it.

## DungeonRoom <!-- a13e28 -->
A room: its bounds, its music type, its enemies.

## DungeonRoom.Spawn <!-- 82ecb4 -->
Spawns near a point through the room's Spawner and parents the enemy under an "Enemies" child (made on demand), so enemies pause with their room. The copy is activated, since summon templates are inactive.
