# Notes for AI assistants

Read [README.md](README.md) first. Before you edit code, read [docs/STYLE.md](docs/STYLE.md).

**Project:** Unity 2D C# scripts only. There are no scenes, prefabs, assets, `.meta` files,
ProjectSettings or packages here, and the `Data` class (`Data.cn` = the Twitch channel name,
set on the title screen) is missing. You cannot open Unity here. If you need a compile check,
compile against small stub Unity types, and say clearly that nothing was tested in Unity.

**Game:** Twitch viewers type `summon <enemy>` or `teleport x y` to mess with the player.
Chat flow: `TwitchChat` → `Chat.addToDB` → `SimpleSummonDB` queue (`spawnSkill` is its
cooldown) → `GameManager.Update` → `DungeonRoom.Spawn` → `Spawner`.
`GameManager.Instance` is the hub. More in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## The owner's rules

These come from the owner's review of earlier AI work (see [docs/HISTORY.md](docs/HISTORY.md)).

- Keep the diff against the first commit (`c470dd7`) small. The owner reviews it with
  `git diff`. Change only what was asked. No "while I'm here" fixes: mention them in your
  reply instead.
- Match the original style: compact one-liners, the brace style of the code around you,
  `d_` ("dirty") names.
- Keep the default usings, empty `Start`/`Update` methods and their template comments,
  commented-out code, `if(true){ // TODO ... }` placeholders, and `...Legacy` methods.
- Do not remove unused fields or methods. The owner will use them later.
- Rename only names that really mislead (as `range` did). Keep typos such as
  `recieveDamage`, `knocback`, `StuntMech`, `hitUntillBreak`.
- Renaming a saved field (`public` or `[SerializeField]`) needs
  `[FormerlySerializedAs("oldName")]`, or Inspector and asset values are lost.
- Put attributes such as `[Tooltip]` and `[FormerlySerializedAs]` on their own line above
  the field, so the field name stays easy to see.
- Do not rename, move or delete files or classes. Unity links scripts by the GUID in `.meta`
  files.
- Bow and Boomerang are not implemented yet. Leave them.

## Unity traps

- Enums are saved as numbers. Add values at the end only (for `ChatType`, after `invalid`).
- Public methods may be wired in the Inspector (Buttons, UnityEvents) by name. Do not rename them.
- `WeaponType` numbers (900+) also serve as Player animation states. Never renumber them.
- The facing order is always up, right, down, left (0–3).
- If your change makes a line in `docs/` wrong, fix that line too.
