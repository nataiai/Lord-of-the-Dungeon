# History: the original code, and what AI has changed

This page keeps a record of the code **as the owner wrote it**, and of every change that AI
has made since. Read it before you merge more AI work.

> Written by Claude (AI) on 2026-11-04 from the git history of all branches.

---

## Timeline

All on 2026-11-04. Times are UTC (the owner's clock is UTC+1).

| Time (UTC) | Who | Branch | What |
|---|---|---|---|
| 10:27 | owner (natapat) | `main` | `c470dd7` "first commit": 35 scripts, 1,930 lines. **The original.** |
| 10:57–11:44 | Claude | `claude/relaxed-babbage-llhs4x` | 7 commits: clean-up, bug fixes, renames, a `CLAUDE.md`. After the owner's feedback, most renames and removals were undone. |
| 11:48 | owner | `claude/relaxed-babbage-llhs4x` | `aa1707c`: put `if(true){ // TODO: check cooldown` back into `TeleportSkill` by hand |
| 11:49 | Claude | `main`, `dev`, `ccr-47eab149-sfd1rr` | `8c77b4d`: the same code as one commit. The `CLAUDE.md` was **not** included. |
| 11:55–14:19 | Claude | `claude/work` | 16 commits: a weapon-system rewrite. **Not merged.** |
| 14:23 | Claude | `claude/hitbox-offset-loot` | 1 commit. **Not merged.** |
| 15:41 | Claude | `claude/hitbox-offset` | 1 commit. **Not merged.** |

---

## The original: `c470dd7`

The whole project arrived in a single commit, so there is no earlier history in git. The
commented-out code and the `...Legacy` methods inside the files are the earlier history
(see [STYLE.md](STYLE.md#commented-out-code-is-the-history-and-a-switch)).

How to look at it:

```sh
git show c470dd7:Player.cs                 # one file, as the owner wrote it
git diff c470dd7 -- '*.cs'                 # everything changed since, on this branch
git diff c470dd7 origin/claude/work -- '*.cs'  # what a branch would change
```

**Suggestion:** give the original a tag, so it is easy to find even if branches are cleaned up
or squashed later:

```sh
git tag original c470dd7
git push origin original
```

---

## Changes already on `main` (`8c77b4d`)

9 files, +22/−21 lines. Three of the changes alter behavior. They are marked **behavior**.

**1. `Connector/Chat.cs` — `summon` with no enemy name** (**behavior**: a crash is fixed)

```csharp
// before
if(len<=3 && w0len<=6 && words[0] == "summon"[..w0len])return ChatType.summon;
// after (the same for "spawn")
if(len>=2 && len<=3 && w0len<=6 && words[0] == "summon"[..w0len])return ChatType.summon;
```

A message like `summon` or `s` on its own used to pass, and then `addToDB` read `words[1]`:
`IndexOutOfRangeException`. Now it counts as `invalid`.

**2. `Controller.cs` — the mouse-mode timeout** (**behavior**: the feel changes)

```csharp
// before
if((mouseLastPos-mousePos).magnitude>0)mouseLastTime=Time.time;
// after
if((mouseLastPos-mousePos).magnitude>0){mouseLastTime=Time.time;mouseLastPos=mousePos;}
```

Before, `mouseLastPos` was never updated while mouse mode was on, so mouse mode effectively
never turned off. Now it turns off after `mouseTimeDeactivation` (1 s) without mouse movement.
Then aim follows the movement direction and the cursor hides. Playtest this (TECH_DEBT B16).

**3. `Loot.cs` — chat-skill pickup** (**behavior**, but data only)

```csharp
// before
if(skill!=ChatType.invalid && GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
// after
if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
```

Before, a skill was only added if it was already in the list, so nothing was ever added. Now
it is added once. No gameplay changes yet, because nothing reads `availableSkill`.

**4. `Data Types/Weapon.cs` — two renames, and tooltips**

```csharp
// before
    public float range;
    public HitBox hitBox;
    public float velocity;
    public float destroyTimer;
    public int destroyOnHit;
// after
    [Tooltip("How far in front of the player the hitbox spawns")]
    [FormerlySerializedAs("range")]
    public float hitBoxDistance;
    public HitBox hitBox;
    [Tooltip("Hitbox flying speed. 0 = stays where it spawns")]
    public float velocity;
    [Tooltip("Seconds before the hitbox disappears. 0 = disappears on first touch")]
    public float destroyTimer;
    [Tooltip("Enemies it can hit before disappearing. 0 = unlimited")]
    [FormerlySerializedAs("destroyOnHit")]
    public int maxHits;
```

Plus `using UnityEngine.Serialization;`. `range` and `destroyOnHit` were renamed because the
names misled: `range` sounds like attack reach but is where the hitbox spawns, and
`destroyOnHit` sounds like a yes/no switch but is a number of hits. `[FormerlySerializedAs]`
keeps the values already saved in weapon assets. After Unity saves those assets again, they
store the new names. `HitBox.cs:43` and `Player.cs:35` follow the rename.

**5. `GameManager.cs` — `channelName` removed**

```csharp
// before
    public string channelName;
    ...
        channelName = Data.cn;
        chat.channel = channelName;
// after
        chat.channel = Data.cn;
```

The GameManager no longer shows the channel name in the Inspector. The `channel` field on the
`TwitchChat` component still shows it. A value typed into `channelName` in the scene was
always overwritten by `Data.cn` anyway.

**6. `Map/EventAllEnemiesDefeated.cs` — the `if(true)` wrapper removed**

```csharp
// before
        if (true){ // this include newly spawned enemies too
            var instances = GameObject.FindObjectsOfType<Enemy>();
            allEnemies = new List<Enemy>(instances);
            if(allEnemies.Count>0)return false;}
        return true;
        // if (allEnemies.Count == 0)
        //     return true;
        // else return false;
// after
        // this include newly spawned enemies too
        var instances = GameObject.FindObjectsOfType<Enemy>();
        allEnemies = new List<Enemy>(instances);
        return allEnemies.Count == 0;
```

Same behavior. Note that three commented-out lines were deleted here. That goes against the
owner's later rule "keep commented-out code".

**7. `Map/HealingRoom.cs`** — the indentation of two closing braces was fixed. Same behavior.

**8. `Player.cs` — `ChangeAnimation(AnimationClip[] toAnim, int index)`** checks
`index<toAnim.Length` instead of `animFacing<toAnim.Length`. Same behavior: every caller passes
`animFacing`.

---

## What the owner pushed back on

The branch `claude/relaxed-babbage-llhs4x` shows what the owner did **not** want. Its commits
were made after the owner's feedback:

- **"Shrink the diff."** Renames that did not mislead were undone: `knocback`, `velocity`,
  `destroyTimer`, `anim`, `uiWep`, `initSpeed`, `cooldown`, `knocbackRecovery`,
  `invincibleCooldownTime`, `hitUntillBreak`, `tmp`. Only `range` → `hitBoxDistance` and
  `destroyOnHit` → `maxHits` stayed.
- **"Restore unused members the owner wants to keep":** `GameManager.dungeonMap`,
  `GameManager.summonableEnemy`, `Enemy.initSpeed`, `Skill.precReady`, `Skill.Start()`.
- **"Restore `GameManager.soundPlayer` as originally written."**
- **Attributes on their own lines** above the field, so the field names stay easy to read.
- **The owner's own commit `aa1707c`** put `if(true){ // TODO: check cooldown` back into
  `TeleportSkill`. An `if(true)` placeholder marks where a future condition goes.
- The branch's `CLAUDE.md` wrote down these rules. That file never reached `main`. The
  `CLAUDE.md` at the root of this branch carries the same rules forward.

**The lesson:** the owner reviews AI work with `git diff` against the original, and wants
small, targeted diffs that keep the original look.

---

## Open AI branches (not merged)

Each one starts from `main` (`8c77b4d`).

### `claude/hitbox-offset` — 1 commit, 2 files

Adds `upOffset`, `rightOffset`, `downOffset`, `leftOffset` (`Vector2`) and `GetOffset(facing)`
to `Weapon`. The offset for the current facing is added to the hitbox spawn point in
`HoldingWeapons.ForceUse`. The default 0 changes nothing. The tooltip is on its own line, as
the owner asked.

### `claude/hitbox-offset-loot` — 1 commit, 6 files

The same offsets, plus a new optional `weaponLoot` (`Weapon`) field on `Enemy`, `Chest` and
`Destructable`. A new `Loot.Drop` builds the weapon pickup in code. The old `loot` prefab
field still works.

### `claude/work` — 16 commits, 13 files changed compared to `main`

A large rewrite of the weapon system:

- `Weapon` becomes a `partial` class: `Data Types/Weapon.cs` (fields only),
  `Data Types/WeaponLogic.cs` (all the logic, plus `HoldingWeapons` moved out of `Player.cs`),
  and a custom Inspector in `Data Types/Editor/WeaponEditor.cs`.
- Hitboxes and effects can be built in code (length/width box, layer, sprite, per-side
  offsets). Prefabs become optional overrides.
- **Deletes** `Player/Weapons/BaseWeapon.cs`, `SwingWeapon.cs` and `UseRod.cs`.
- Enemies, chests and destructables can drop a `Weapon` asset or a chat skill directly.
- **Gameplay change:** `Chat.addToDB` now ignores commands for skills that are not in
  `availableSkill`, so chat cannot summon until the player picks up the summon skill (which
  is dropped next to the player at start).

**Check before merging `claude/work`:**

1. **Deleted scripts.** Every prefab that uses `BaseWeapon`, `SwingWeapon` or `UseRod` will
   show *Missing (Mono Script)* in Unity (TECH_DEBT A4).
2. **Lost weapon data.** `Weapon.anim` (a `BaseWeapon`) is replaced by `effectPrefab` (an
   `Animator`) with no `[FormerlySerializedAs]`, so every existing weapon asset loses its effect
   link. `hitBox` → `hitBoxPrefab`, `range` and `destroyOnHit` do keep their data.
3. **Style.** The new `Weapon.cs` drops the default usings and the `// [System.Serializable]`
   first line, puts `[FormerlySerializedAs]` on the same line as the field, and writes
   `[CreateAssetMenu]` on one line. All of these go against the owner's rules
   ([STYLE.md §7](STYLE.md#7-keep-these-the-owners-rules)).
4. **Size.** 13 files changed compared to `main` (17 compared to the original), so the diff
   is far from "small".

---

## How to check a future AI change

1. Read the diff against the original: `git diff c470dd7 <branch> -- '*.cs'`
2. Go through the review checklist in [STYLE.md §8](STYLE.md#8-review-checklist).
3. Look for renamed or deleted fields, files, classes, public methods and enum values
   (TECH_DEBT A2, A4).
4. Open the Unity project, let it compile, and play the changed feature.
5. Add the change to this page.
