# GameManager.cs <!-- 68e590 -->
The one object everything else talks to: player, UI, input, map, chat, and the spawning of what chat summons.

## GameManager <!-- e42057 -->
Hub: holds the main objects, spawns chat's summons.
Reached through `GameManager.Instance` and kept across scene loads. Its fields connect the parts: `player`, `ui`, `controller`, `chat`, `map`.

## GameManager.Awake <!-- 85d3b8 -->
Keeps only one GameManager, finds the cameras, uses the UI in the scene or makes one, and sets the frame rate (vSync off so it applies).

## GameManager.Update <!-- 9fbde2 -->
Testing without chat (Experimental.testWithoutChat): spawns `mockEnemy` every few seconds unless L is held. Otherwise spawns whatever chat summoned once the cooldown allows it.

## GameManager._Connect <!-- b7273f -->
Joins chat once, on the first kill or weapon pickup: the channel from Data.cn goes to UnifiedChat when Experimental.unifiedChat is on, otherwise to the Lexone TwitchChat. Not in test mode.

## GameManager.TeleportSkill <!-- 1dd4a2 -->
The chat teleport command: moves the player to (x, y) if that point is in the current room and nothing stands there. The cooldown check is still a TODO (`if(true)`).
