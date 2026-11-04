# Lord of the Dungeon — notes for Claude

Unity 2D C# scripts only: no scenes, `.meta` files, or the `Data` class (`Data.cn` = Twitch channel, set on the title screen). Unity can't be opened here, so check changes by compiling against stub Unity APIs.

**Game:** Twitch viewers type `summon <enemy>` / `teleport x y` to mess with the player.
Chat flow: `TwitchChat` → `Chat.addToDB` → `SimpleSummonDB` queue (`spawnSkill` = its cooldown) → `GameManager.Update` → `DungeonRoom.Spawn` → `Spawner`.
`GameManager.Instance` is the hub (player, ui, map, chat, controller).

**Weapon fields:** `hitBoxDistance` (spawn offset in front of player), `velocity` (0 = hitbox stays put), `destroyTimer` (0 = gone on first touch), `maxHits` (0 = unlimited).

**Owner's rules**
- Keep the diff against the first commit small (owner reviews it with git diff). Change only what was asked; no extra "while I'm here" fixes.
- Keep usings, default `Start`/`Update`, commented-out code, the compact style, and `d_` ("dirty") names.
- Don't remove unused functions or fields; the owner will use them later.
- Rename only names that mislead (like `range` was). Keep typos such as `recieveDamage`, `knocback`, `StuntMech`.
- Renaming a serialized field (public or `[SerializeField]`) needs `[FormerlySerializedAs("oldName")]`, or inspector/asset values are lost.
- Don't rename files or classes; Unity links scripts by file.
- Put attributes (`[Tooltip]`, `[FormerlySerializedAs]`) on their own lines above the field, so field names stay visible.
- Bow/Boomerang are not implemented. Leave them.
