# Map/DungeonMap.cs <!-- 422552 -->
The dungeon: all rooms, which one is active, and how door triggers lead from one room to the next.

## DungeonMap <!-- 7046ed -->
All rooms; only the current one is active.
Each door trigger is paired with the nearest trigger of another room; walking out of a trigger toward its partner switches rooms.

## DungeonMap.Start <!-- 30a344 -->
Finds every door trigger (inactive rooms included), pairs them, and makes the active room that contains the player the current room.

## DungeonMap.fillTriggerMap <!-- 9961c8 -->
Pairs each door trigger with the closest trigger that belongs to a different room.

## DungeonMap.roomTriggerExit <!-- f501ce -->
The player left a door trigger. If they were moving toward its partner, they went through the door: switch rooms.

## DungeonMap.switchRoom <!-- c9862d -->
Turns the partner's room on and this one off, records where each player entered (falling in a hole brings them back there), changes the music, and clears the pathfinding cache.

## DungeonMap._findRoom <!-- c48cad -->
The first room whose bounds contain a point. The code marks it "buggy, use with care"; only TeleportSkill uses it.
