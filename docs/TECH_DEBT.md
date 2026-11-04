# Technical debt and risks

**Documented, not fixed.** The owner's rule is to change only what is asked, so nothing on
this page has been changed in the code. If you fix an item, do it as its own small change,
and update this page.

Line numbers refer to commit `8c77b4d` (`main`).

> Written by Claude (AI) on 2026-11-04 from reading the code. It was not tested in Unity.
> Items marked **verify** need a check in the Unity project.

**Severity**

- **High:** can lose data or break the game when it happens.
- **Medium:** a visible bug in normal play, or a likely mistake for a new developer.
- **Low:** small, rare, or only a surprise.

---

## Summary

| ID | Sev | Area | Problem |
|---|---|---|---|
| A1 | High | project | The repo holds only scripts. It cannot be compiled, run or tested on its own. |
| A2 | High | project | Saved names (fields, enum numbers, public methods) break silently when renamed. |
| A3 | High | project | Two files do not match their class names (**verify**). |
| A4 | High | project | No `.meta` files: renaming, moving or deleting a script outside Unity breaks references. |
| A5 | Medium | project | Unity API versions are mixed; the API Updater may rewrite many files. |
| A6 | Medium | project | Needs *Active Input Handling* = Both. |
| A7 | Low | project | Package versions are not recorded here (Cinemachine 2 vs 3). |
| A8 | Low | project | No tests, no CI, no namespaces. |
| B1 | Medium | gameplay | A Chest counts as an Enemy, so "all enemies defeated" never fires while a chest is active. |
| B2 | Medium | gameplay | Every enemy and chest "dies" 360 s after it first became active, even while its room is off. |
| B3 | Medium | input | With a gamepad connected, the keyboard and mouse are ignored. |
| B4 | Medium | chat | Teleport has no cooldown, and summons have no per-viewer limit. |
| B5 | Medium | debug | The `;` reload fights with the `DontDestroyOnLoad` GameManager. |
| B6 | Low | chat | The summon queue resets at 300 and drops waiting commands. |
| B7 | Low | spawn | With no free spot, an enemy spawns on top of the player. |
| B8 | Low | spawn | Trigger areas also block spawn spots and teleport targets. |
| B9 | Low | enemy | A dying enemy can still hurt the player for 0.5 s. |
| B10 | Low | sound | Entering a room type with no music throws an error. |
| B11 | Low | UI | A new text message can be hidden early by an older one. |
| B12 | Low | debug | The L key only skips one frame of mock spawning. |
| B13 | Low | chat | An empty chat message would throw an error. |
| B14 | Low | chat | `Skill.Start()` never runs (kept on purpose). |
| B15 | Low | player | Player death is still a test version. |
| B16 | Low | input | Mouse aim now times out after 1 s (new on `main`). Playtest it. |
| B17 | Low | combat | A hitbox with `destroyTimer = 0` dies on any trigger contact. |
| B18 | Low | enemy | Enemy facing uses a non-normalized vector (no visible effect yet). |
| B19 | Low | loot | Weapon swap only checks the button at the moment you step on the loot. |
| B20 | Low | healing | The heal sound and effect repeat even at full HP. |
| B21 | Low | chat | The chat handler is never removed. |
| C1–C7 | — | setup | Traps in the scene and prefab setup (see section C). |
| D1 | Medium | speed | Every enemy searches the whole scene for the player, every frame. |
| D2–D6 | Low | speed | Smaller per-frame costs. |
| E | — | features | Unfinished features and unused members (keep them). |
| F | — | health | Coupling, magic numbers, duplicates: accepted style. |

---

## A. Project and process

**A1 (High) — The repo holds only scripts.** There are no scenes, prefabs, ScriptableObject
assets, `.meta` files, `ProjectSettings/` or `Packages/`, and the `Data` class (`Data.cn`)
is missing. So the code cannot compile, run or be tested from this repo, and the scene
wiring is only in the Unity project. Every change must be checked in the Unity editor.
For a quick compile check without Unity, you can compile against small stub Unity types,
but that proves only that it compiles.

**A2 (High) — Saved names break silently.** Unity stores field values by **field name**,
enums by **number**, and Inspector event links by **method name**. None of this gives a
compile error when it breaks:

- Renaming a `public` or `[SerializeField]` field loses its values in every scene and asset.
  Use `[FormerlySerializedAs("oldName")]` (main already does this for `Weapon.hitBoxDistance`
  and `Weapon.maxHits`). After Unity saves the assets again they use the new name, so going
  back would need `[FormerlySerializedAs]` the other way.
- Adding an enum value in the middle shifts the numbers. Example: a new `ChatType` before
  `invalid` turns every saved `Loot.skill = invalid` into the new command.
- Renaming a public method breaks Buttons and UnityEvents that call it by name.

Full list: [ARCHITECTURE.md §6](ARCHITECTURE.md#6-what-the-unity-project-must-provide).

**A3 (High, verify) — File name ≠ class name.** Unity needs a MonoBehaviour or
ScriptableObject class to be in a file with the same name, or it cannot attach or load it.
Two files break this rule in the repo as committed:

| File | Class |
|---|---|
| `DestructableObj.cs` | `Destructable` |
| `Data Types/Enemy.cs` | `EnemySO` |

Either the files have other names in the Unity project, or these classes depend on something
that can break. Also, `Enemy.cs` and `Data Types/Enemy.cs` share a file name, which is
confusing. Check this in Unity before you touch these files. Do not "fix" it by renaming
here (see A4).

**A4 (High) — No `.meta` files.** Unity links every script to scenes and prefabs by the GUID
in its `.meta` file. Those files are not in this repo. If a script is renamed, moved or
deleted outside the Unity editor, every component that uses it becomes *Missing (Mono
Script)*. Copy changed files over the old ones in the Unity project (so the old `.meta`
stays), and do renames, moves and deletes inside the editor. Note: the unmerged branch
`claude/work` deletes `BaseWeapon.cs`, `SwingWeapon.cs` and `UseRod.cs` (see
[HISTORY.md](HISTORY.md)).

**A5 (Medium) — Mixed Unity API versions.** The code uses newer APIs
(`FindFirstObjectByType`, `FindObjectsByType`) next to older ones that recent Unity versions
mark obsolete (`FindObjectOfType`, `FindObjectsOfType`, and `Rigidbody2D.velocity`, which is
`linearVelocity` in Unity 6). A comment in `GameManager.cs:59` links the Unity 6 (6000.0)
docs. Check `ProjectSettings/ProjectVersion.txt`. If Unity's API Updater offers to rewrite
these, it changes many files at once. Accept it only as its own commit, so the owner's diff
stays readable.

**A6 (Medium) — Input handling.** `Controller` and `GameManager` use the old Input Manager
(`Input.GetAxisRaw`, `Input.GetKeyDown`), and `Controller` also uses the new Input System
package (`Gamepad.current`, `Keyboard.current`, `Mouse.current`). *Player Settings → Active Input
Handling* must be **Both**, or input throws errors or does nothing.

**A7 (Low) — Package versions are not recorded.** `FollowPlayer` uses the Cinemachine 2.x
API (`using Cinemachine;`, `CinemachineVirtualCamera`). Cinemachine 3 renamed both. The
Lexone *Unity-Twitch-Chat* package (`Lexone.UnityTwitchChat`) must be installed. Neither
version is written down in this repo.

**A8 (Low) — No tests, no CI, no namespaces or assembly definitions.** Everything is in the
global namespace, so names like `Data`, `UI`, `Skill`, `Chat`, `Loot` or `Door` could clash
with a future package.

---

## B. Gameplay bugs and surprises

**B1 (Medium) — A Chest counts as an Enemy.** `Chest` extends `Enemy` and is never destroyed
(it only opens). `EventAllEnemiesDefeated` waits until no active `Enemy` is left
(`Map/EventAllEnemiesDefeated.cs:29-38`), so it **never fires while a chest is active**.
Do not put a chest in a room that uses this event, or make the event skip chests.

**B2 (Medium) — Enemies expire after 360 s.** `Enemy.lifeSpan = 360` (`Enemy.cs:19`). The
timer starts when the enemy first becomes active (`born` in `Start`) and uses `Time.time`, so
it keeps running while the room is switched off. Effects:

- hand-placed enemies in a room you come back to after 6 minutes die at once and drop loot;
- **chests open by themselves** after 6 minutes (`Died()` is the chest's "open");
- an expiring enemy also calls `_Connect()`, like a normal death.

For summoned enemies this cleanup is probably wanted.

**B3 (Medium) — A gamepad turns off the keyboard and mouse.** In `Controller.UpdateInput`
(`Controller.cs:57-65`), the gamepad branch overwrites `movement` and then `return`s. With
any gamepad connected (even one that is not used), keyboard movement and mouse aim and
attacks do nothing.

**B4 (Medium) — Chat spam.** `TeleportSkill` has no cooldown (`GameManager.cs:99-104`, the
`if(true){ // TODO: check cooldown` placeholder waits for it), so chat can move the player
every message. Summons have only one global cooldown and no per-viewer limit
(`limitPerPlayer` is not used), so one viewer can fill the queue.

**B5 (Medium) — The `;` debug reload.** `;` reloads `"DungeonRoom"` (`GameManager.cs:88-89`).
But the GameManager survives scene loads (`DontDestroyOnLoad`) and keeps pointing at the old
scene's player, UI, map and so on, while the new scene's GameManager only removes its own
component (`Destroy(this)`). Unless all those objects are children of the GameManager,
expect missing-reference errors after a reload. Use `;` only for quick tests.

**B6 (Low) — The summon queue resets at 300.** `SimpleSummonDB.Add` calls `Reset()` when the
list has more than 300 entries (`Connector/SummonDB.cs:38-41`). This also drops commands that
have not been spawned yet.

**B7 (Low) — Spawning on the player.** If the BFS finds no free spot, `pos` stays
`oriPos`, the player's position (`Spawner.cs:72,86-88`). The enemy appears on top of the
player and can hit them at once.

**B8 (Low) — Triggers block spawning and teleports.** `Physics2D.OverlapPoint` also returns
trigger colliders (when *Queries Hit Triggers* is on, the default). So healing pools, door
triggers and loot also count as "not free" for the spawner (`Spawner.cs:83`) and for
`TeleportSkill` (`GameManager.cs:102`).

**B9 (Low) — A dying enemy can still hurt.** During the 0.5 s "death breath",
`OnTriggerStay2D` (`Enemy.cs:92-100`) does not check `isDead`, so contact damage still works.
The enemy can also keep chasing if it is not stunned.

**B10 (Low) — Missing room music throws an error.** If `getClip` finds no clip for a room
type, `UpdateMusic` sets the clip to `null` and then reads `clip.name`
(`SoundPlayer.cs:34-42`): a `NullReferenceException` right after the room switch. Give every
room type in use a clip.

**B11 (Low) — Text box timing.** Each `UI.ShowText` starts a new 2-second coroutine
(`UI.cs:25-34`). The older coroutine still hides the box, so a newer message can disappear
early.

**B12 (Low) — The L key.** `if(testWithoutChat && !Input.GetKeyDown(KeyCode.L))`
(`GameManager.cs:81`). `GetKeyDown` is true for one frame only, so L skips at most one frame.
If the idea was "hold L to pause the mock spawns", it should be `GetKey`.

**B13 (Low) — Empty message.** `Chat.Categorize` reads `words[0]` without checking that there
is a word (`Connector/Chat.cs:37-38`). Twitch does not send empty messages, but a direct call
with `""` or only spaces throws.

**B14 (Low) — `Skill.Start()` never runs.** `Skill` is a plain class, not a MonoBehaviour,
so Unity never calls its `Start()` (`Skill.cs:18-20`). `lastUse` starts at 0, so the summon
skill is ready as soon as `Time.time > cooldown`. The owner asked to keep this method.

**B15 (Low) — Player death is a test version.** `Player.Died()` (`Player.cs:135-140`,
marked `//test`): full HP, move to (0, 0), switch to `initRoom`. There is no game over, the
summon queue keeps going, and (0, 0) must be inside `initRoom`.

**B16 (Low) — Mouse aim timeout (new on `main`).** Since commit `8c77b4d`, mouse mode turns
off after `mouseTimeDeactivation` (1 s) without mouse movement. Then aim follows the movement
direction and the cursor hides. This is what the field names say, but it changes the feel for
players who aim with a still mouse. Playtest it, or raise `mouseTimeDeactivation` in the
Inspector.

**B17 (Low) — `destroyTimer = 0` means "any contact".** `HitBox.OnTriggerEnter2D` destroys a
hitbox with `destroyTimer == 0` on its first trigger contact with **anything**
(`HitBox.cs:49-50`). Depending on the layer collision matrix, that can be the player's own
collider, a door trigger or a loot item.

**B18 (Low) — Enemy facing.** `Enemy.Update` calls `setAnimFacing(movement)` with the full
distance to the player (`Enemy.cs:48,51`). The 0.7071 limits in `_setAnimFacing` assume a
length-1 vector, so far-away enemies almost always "face" left or right. There is no visible
effect today, because enemy animations are never played. Normalize the vector if you hook
them up.

**B19 (Low) — Weapon swap timing.** The swap code runs from `OnTriggerEnter2D`
(`Loot.cs:41-76`), so the button is only checked at the moment you step onto the loot. You
must already hold the button (which also attacks). The commented-out `isPressedOnce()` line
shows another idea.

**B20 (Low) — Healing at full HP.** `HealingRoom.Update` plays the sound and creates the
effect on every heal tick, also when HP is already full (`Map/HealingRoom.cs:31-38`).

**B21 (Low) — The chat handler is never removed.** `TwitchChat.Connect` adds
`OnChatMessage` to the IRC event (`Connector/TwitchChat.cs:24-28`) and nothing removes it.
`_Connect()` calls it only once, so this matters only if `TwitchChat` is ever destroyed or
reconnected.

---

## C. Setup traps

The full list of what the Unity project must provide is in
[ARCHITECTURE.md §6](ARCHITECTURE.md#6-what-the-unity-project-must-provide).
These are the ones that look like they should work, but do not:

**C1 — `BaseWeapon.anim` must be assigned in the prefab.** `HoldingWeapons.ForceUse` calls
`anim.anim.Play(...)` right after `Instantiate` (`Player.cs:43`). `BaseWeapon.Start` (which
does `GetComponent<Animator>()`) has not run yet at that moment.

**C2 — The collider that `Loot` adds is not a trigger.** `Loot.Start` adds a
`BoxCollider2D` when there is none (`Loot.cs:26`). Its `isTrigger` is false, so pickup does
not work against a solid player collider. Give loot prefabs their own trigger collider.

**C3 — `DungeonMap.allRooms` is filled only in the editor** (`OnValidate`,
`Map/DungeonMap.cs:19-21`). After adding or removing a room, make the list refresh (change a
value on `DungeonMap`) and save the scene, or builds will not know the room.

**C4 — Door triggers are paired by distance** (`Map/DungeonMap.cs:32-45`). Each trigger is
paired with the nearest trigger of another room. A door without a partner gets paired with a
far-away trigger, and with only one room the partner is `null` (error on exit).

**C5 — Components must be on the same GameObject as their collider.** Hits and pickups use
`other.TryGetComponent(...)`, which does not look at parents or children.

**C6 — Room tilemaps at z = 0.** 2D points (z = 0) are tested against 3D `Bounds`
(`Spawner.cs:83`, `Map/DungeonMap.cs:27,81`).

**C7 — Room switching uses the player's velocity when leaving a door trigger**
(`Map/DungeonMap.cs:51-56`). Unusual exits can pick the wrong room: for example being knocked
back, or a chat teleport while standing in a door.

---

## D. Performance

Fine for a small scene. Watch it when chat summons many enemies during a long stream.

**D1 (Medium) — `FindObjectOfType<Player>()` in every enemy, every frame** (`Enemy.cs:47`).
This searches the whole scene, once per enemy per frame, so the cost grows quickly with the
number of enemies. `GameManager.Instance.player` gives the same object for free. This is the
cheapest, safest fix on this page.

**D2 (Low) — `FindObjectsOfType<Enemy>()` every frame** in `EventAllEnemiesDefeated`, once
its first list is empty (`Map/EventAllEnemiesDefeated.cs:35`).

**D3 (Low) — `transform.Find("Fill").GetComponent<Image>()` twice per frame** in `UI.Update`
(`UI.cs:72-73`).

**D4 (Low) — Spawner BFS.** Each spawn calls `Physics2D.OverlapPoint` about once per grid
point in range (plus a thin ring just outside it): roughly π × (range / 0.5 + 1)², so about
150 calls at range 3. Fine.

**D5 (Low) — Empty `Update()` and `Start()` methods** (in `Chat`, `Loot`, `SoundPlayer`,
`ChannelBtn`, `FollowPlayer`, `Controller`, and others). Unity still calls an empty
`Update()` every frame. The owner keeps them.

**D6 (Low) — Logs.** `Spawner.Awake` logs every spawner. `SoundPlayer.UpdateMusic` prints on
every music change. Logging is slow in builds.

---

## E. Unfinished features and unused members — keep them

The owner keeps unused code on purpose: these are plans. The plans are listed in
[ARCHITECTURE.md §3.10](ARCHITECTURE.md#310-planned-but-not-wired-yet). Members with no live
use today:

- `Controller.attack`, `Controller.switchWeapon` (written, never read),
  `ControllerButton.isPressedOnce()`
- `Player.activeArmor`, `Player.speedBoost`, `playerShooting` clips,
  `HoldingWeapons.StopAnim()`
- `Enemy.initSpeed` (alias), `enemyIdle` / `enemyWalk` / `enemyAttack` clips,
  `Chest.closedSprite`
- `Character.isLooking`, `Character.setAnimFacingLegacy()`
- `Weapon.hitSound`
- `GameManager.dungeonMap`, `GameManager.summonableEnemy` (aliases)
- `DungeonMap.width` / `height`, `roomTriggerEnter()` (empty), `roomTriggerExitLegacy()`,
  `roomTriggerExitLegacy2()`
- `HealingRoom.healingArea`, `HealingRoom.healingEffectTime`
- `Title.myAudio`, `Title.titleMusic`
- `SimpleSummonDB.limitPerPlayer`, `SummonCommand.spawnableLocation`,
  `Direction.strMoveDict`
- `ChatMsg`, `TwitchChatMsg`, `Armor`, `UsableItem`, `UsableItemType`, `Door`
- `PlayerControl.cs` (an empty file)

---

## F. Code health — accepted style, listed so you know

These are part of how the owner writes code ([STYLE.md](STYLE.md)). Do not change them
unless you are asked.

- **Tight coupling through the hub.** Almost everything calls `GameManager.Instance.*`
  directly. The `Player.HP` setter needs both the GameManager and the UI, so no class can be
  tested on its own.
- **Polling instead of events** (summon queue, room events, UI cooldowns).
- **Magic numbers:** `0.7071f` (facing), `0.5f` (hitbox height offset, BFS step, death
  breath), `.9f` (animation end), `360` (enemy life), `300` (queue size), `0.1f` (red
  flash), `1` (stun), `10` (knockback spread in degrees), `2` (text seconds), `999999` and
  `9999f` (sentinels).
- **Several versions kept side by side:** three room-switch methods, two facing methods, a
  commented `SummonDB`, a commented copy of the Lexone library.
- **Aliases and duplicates:** `dungeonMap`/`map`, `Enemy.initSpeed`/`enemyData.initSpeed`,
  `precReady`/`getReloadPercent()`.
- **Mixed naming, and typos in names** (`recieveDamage`, `knocback`, `StuntMech`).
- **About 16% of the lines are commented-out code.**

---

## If you are asked to fix things — a suggested order

1. **D1** — use `GameManager.Instance.player` in `Enemy.Update`. Small, safe, and it helps a
   lot when there are many enemies.
2. **B4** — a teleport cooldown. The `if(true)` placeholder is waiting for it; a `Skill`
   (like `spawnSkill`) would fit.
3. **B1 / B2** — decide how chests and enemy lifetime should work. This is a design choice,
   so ask the owner first.
4. **B10, B13** — small guards against crashes.
5. **B3** — decide whether keyboard and gamepad should work at the same time.

Always: one item per change, keep the owner's style, and test in the Unity editor.
