# Lord of the Dungeon — game scripts

C# scripts for a Unity 2D top-down dungeon game that **Twitch chat plays against the
streamer**. The streamer fights through rooms. Viewers type `summon <enemy>` to send a monster,
or `teleport <x> <y>` to move the player.

> **This repo holds only the `.cs` files.** Scenes, prefabs, ScriptableObject assets,
> `.meta` files, project settings, packages and the `Data` class live in the Unity project.
> The code does not compile on its own.

## Folder map

| Folder | Contents |
|---|---|
| (root) | Core gameplay: `GameManager` (the hub), `Player`, `Enemy`, `Character`, `Controller` (input), `HitBox`, `Loot`, `Spawner`, `UI`, `SoundPlayer`, the title screen |
| `Connector/` | Twitch chat → chat commands → summon queue |
| `Map/` | Rooms and room switching, room events, chest, healing room |
| `Data Types/` | ScriptableObject data: `Weapon`, `EnemySO`, `Armor`, `UsableItem`, and `WeaponType` |
| `Player/Weapons/` | Weapon effect animation (`BaseWeapon`) |

## Before you change any code

1. **Saved names.** Unity saves `public` and `[SerializeField]` fields by name. Renaming one
   loses its values in every scene and asset, unless you add `[FormerlySerializedAs("oldName")]`.
2. **Files and classes.** Unity links scripts through `.meta` files, which are not in this
   repo. Do not rename, move or delete scripts outside the Unity editor.
3. **Public methods** may be wired to Buttons or UnityEvents in the Inspector, by name.
   Renaming one breaks that link with no compile error.
4. **Enums** are saved as numbers. Add new values at the end only. Never renumber `WeaponType`.
5. **The owner's style is part of the project.** Keep commented-out code, unused members,
   `if(true)` placeholders and typos such as `recieveDamage`. Change only what you were asked
   to change.
6. **The original**, human-written code is commit `c470dd7`. See every change since then with
   `git diff c470dd7 -- '*.cs'`.

## Documentation

| Page | What it covers |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | What the code does, what it is trying to hold, why it is built this way, what the Unity project must provide, and how to add enemies, weapons, rooms and chat commands |
| [docs/STYLE.md](docs/STYLE.md) | The original code style, with examples, and a review checklist |
| [docs/TECH_DEBT.md](docs/TECH_DEBT.md) | Known bugs, risks and debt (documented, not fixed) |
| [docs/HISTORY.md](docs/HISTORY.md) | The original snapshot, what AI has changed so far, and the open AI branches |
| [CLAUDE.md](CLAUDE.md) | Short rules for AI assistants |

These pages were written by Claude (AI) on 2026-11-04 from the code and the git history.
They were not checked in the Unity editor. If this folder sits inside `Assets/`, Unity imports
the `.md` files as text assets, which is harmless.
