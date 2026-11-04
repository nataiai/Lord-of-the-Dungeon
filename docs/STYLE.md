# Code style of the original code

This page describes how the code looked when the owner wrote it: commit `c470dd7`
("first commit"), before any AI changes. It describes the style. It does not judge it.

Use this page to:

- write new code that looks like it belongs next to the old code, and
- notice when a change (human or AI) drifts away from the owner's style.

To see any file as the owner wrote it: `git show c470dd7:Player.cs`

> Written by Claude (AI) on 2026-11-04 from reading the code and the git history.
> All examples below are copied from commit `c470dd7`.

---

## Fingerprint (the short version)

- Unity C#, global namespace, one main class per file plus small helper types.
- Compact: `if(x)y;` on one line, few spaces, whole methods and properties on one line.
- Two brace styles side by side. Code from Unity's script template keeps braces on their
  own line. Code the owner typed opens braces on the same line.
- Every value you might tune is a `public` field, so you can change it in the Inspector.
  When a value needs side effects, it is a `_name` backing field plus a `name` property.
- Timers are timestamps compared with `Time.time`.
- Old code is commented out, not deleted. Older versions of methods stay alive as `...Legacy`.
- Comments are short, lowercase and informal. Many have a link to the page the code came from.
- Typos in names are kept: `recieveDamage`, `knocback`, `StuntMech`.

---

## 1. Files and folders

**Folders group code by game area.** Core gameplay classes sit at the root.

| Folder | Contents |
|---|---|
| (root) | `GameManager`, `Player`, `Enemy`, `Character`, `Controller`, `HitBox`, `Loot`, `Spawner`, `UI`, `SoundPlayer`, `Title`, ... |
| `Connector/` | Twitch chat and the summon queue |
| `Map/` | Rooms, room switching, room events, chest, healing room, door |
| `Data Types/` (with a space) | ScriptableObjects and `WeaponType` |
| `Player/Weapons/` | Weapon effect animation (`BaseWeapon` and two empty subclasses) |

**One main class per file.** Small related types live in the same file:

| File | Also contains |
|---|---|
| `Player.cs` | `Direction` (direction tables), `HoldingWeapons` (one hand slot) |
| `Character.cs` | `CombatStat` |
| `Controller.cs` | `ControllerButton` |
| `Connector/Chat.cs` | `enum ChatType`, `ChatMsg` |
| `Connector/SummonDB.cs` | `SummonCommand`, `SimpleSummonDB` |
| `Connector/TwitchChat.cs` | `TwitchChatMsg` |
| `Loot.cs` | `enum LootType` |
| `Map/DungeonRoom.cs` | `enum RoomType` |
| `Data Types/UsableItem.cs` | `enum UsableItemType` |

**The file name is usually the class name.** There are two exceptions:
`DestructableObj.cs` holds `Destructable`, and `Data Types/Enemy.cs` holds `EnemySO`.
See [TECH_DEBT.md](TECH_DEBT.md) A3 before you touch either one.

**No namespaces, no assembly definitions (`.asmdef`).**

**File header = Unity's default usings, even when they are not used:**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
```

The four ScriptableObject files in `Data Types/` (`Weapon`, `Enemy`, `Armor`, `UsableItem`)
start with a commented-out line above the usings:

```csharp
// [System.Serializable]
using System.Collections;
```

**Whitespace facts.** ASCII only, LF line endings, 4 spaces (no tabs), no BOM.
13 of the 34 non-empty files do not end with a newline, and many lines have trailing
spaces. Do not "fix" these in bulk. It fills the owner's `git diff` with noise.

---

## 2. Formatting

### Braces: two styles live together

Code that came from Unity's script template (or was pasted from a tutorial) keeps that style:

```csharp
public class Player : Character
{
    ...
    void Update()
    {
```

Code the owner typed opens the brace on the same line, often with no space before `{`:

```csharp
public class GameManager: MonoBehaviour{
    void Start(){
    public void _Connect(){
        if(_connectOnce)return;
```

Both styles are fine. Use the style of the code around your change.

### Compact one-liners

```csharp
if(HP<=0)Died();
if(ui==null)ui = Instantiate(uiPrefab, new Vector2(0,0), Quaternion.identity);
public void Heal(int heal=1){HP+=heal;if(HP>maxHP)HP=maxHP;}
public int maxHP{get=>_maxHP;set{_maxHP=value;GameManager.Instance.ui.UpdateMaxHP(maxHP);}}
public void StopAnim(){if(anim!=null)GameObject.Destroy(anim);anim=null;}
```

- No space after `if`, `for`, `while`, `switch`. Usually no spaces around operators in conditions.
- A one-statement `if` has no braces and stays on the same line.
- Several statements can share one line: `Queue<Vector2> q = new();q.Enqueue(pos);`
- Code can follow a closing brace on the same line:
  `}return false;` · `}else{` · `}else return false;` · `}else enableMouse = false;`
- Several fields in one declaration: `public HoldingWeapons lWeapon,rWeapon;`
- Spacing is not strict: `(Vector2) playerPos`, `(Vector2)  (worldPos - ...)`,
  `hitBox.setVelocity (...)`. Do not reformat lines you did not need to change.

### `switch`

```csharp
        switch(state){
            case IDLE:
                cooldown = 0;
                if(isHealing)ChangeAnimation(playerIdleWater, animFacing);
                else ChangeAnimation(playerIdle, animFacing);
            break;
            case CQC:
            case SPEAR:
                attackState = true;
                ChangeAnimation(playerSpear, animFacing);
                forceAnim = false;
            break;
        }
```

`break;` goes back to the `case` level. Stacked `case` labels share code.
In `Chat.Categorize` the case body is at the same level as the `case` label.

### Algorithm code reads like competitive programming

Short names, sentinel values, and `++`/`--` inside conditions. These lines come from
`UI.UpdateMaxHP`, `UI.UpdateHP`, `DungeonMap.fillTriggerMap` and the BFS in `Spawner.Spawn`:

```csharp
while(diff-->0)heart.Add(Instantiate(heart[0],_t.position,Quaternion.identity,_t));
for(int i=0;++i<heart.Count;){
if(cnt++>HP-1)i.sprite = healthSprite[1];
float m = 999999;
var f = q.Dequeue();
```

### Attributes

- Short Unity attributes go on the same line as the field:
  `[SerializeField] Animator anim;` · `[HideInInspector] public bool isHealing;`
- `[CreateAssetMenu(...)]` is split over several lines:

  ```csharp
  [CreateAssetMenu(
      fileName = "WeaponSO",
      menuName = "Weapon"
  )]
  ```

- The owner's later request (while reviewing AI work): longer attributes such as `[Tooltip]`
  and `[FormerlySerializedAs]` go **on their own line above the field**, so the field name is
  easy to see. `Data Types/Weapon.cs` on `main` follows this.

---

## 3. Naming

| What | Convention | Examples |
|---|---|---|
| Classes, enums | PascalCase | `GameManager`, `HoldingWeapons`, `RoomType` |
| ScriptableObjects | Plain name. Add `SO` only if the name is taken. Default asset name is `XxxSO`. | `Weapon` (asset `WeaponSO`), `EnemySO` (`Enemy` is the MonoBehaviour) |
| Fields, locals | camelCase, short abbreviations | `movement`, `animFacing`, `rb`, `sr`, `col`, `anim`, `uiWep`, `summondb`, `lWeapon`, `rWeapon`, `ePos`, `oriPos` |
| Acronyms | stay upper case | `HP`, `FPS`, `HPHealPerSecond`, `originalSRcolor`, `addToDB`, `SimpleSummonDB` |
| Constants | ALL CAPS | `IDLE`, `MOVING`, `SWING`, `USEROD` |
| Backing fields | `_` + name, often **public** so the Inspector shows it | `_HP`/`HP`, `_maxHP`/`maxHP`, `_initSpeed`/`initSpeed`, `_combatStat`/`combatStat`, `_looking`/`looking` |
| Bools | `is...`, `enable...` | `isDead`, `isMoving`, `isMagical`, `enableMouse`, `enableInvincible` |
| UnityEvents | `on...` | `onAllEnemiesDefeated`, `onCollectedSword` |
| Methods | PascalCase **and** camelCase are both used. There is no strict rule. | `Spawn`, `UpdateMusic`, `TeleportSkill` / `setAnimFacing`, `receiveKnockback`, `addToDB`, `switchRoom` |
| Getters | Java-style `getX()` | `getReloadPercent()`, `getEnemy()`, `getClip()` |
| Death | past tense | `Died()` |

### Prefixes and suffixes that carry meaning

| Mark | Meaning | Examples |
|---|---|---|
| `_` on a method | A raw, internal or "use with care" helper. There is often a safer wrapper. | `_findRoom()` (comment: `// buggy, use with care`), `_Pop()`, `_setAnimFacing()` (behind `setAnimFacing()`), `_Connect()` (connects only once), `_re()` (reset to the first room) |
| `d_` | "Dirty": quick, temporary code. The owner's own words: `// d mean dirty, dirty code` | `d_textBox`, `d_textLabel`, `d_foo()` |
| `sd_` | A sound clip | `sd_enemy_hit` |
| `...Legacy`, `...Legacy2` | An older version, kept for reference or to switch back | `setAnimFacingLegacy`, `roomTriggerExitLegacy`, `roomTriggerExitLegacy2` |
| `...Handle` | Handles one case | `PickWeaponHandle`, `PickChatSkillHandle` |
| `...Mech` | A game mechanic | `StuntMech` (stun) |

### Enum members

Casing is mixed. Keep each enum's own style:

- lower case: `ChatType.summon`, `RoomType.rest`
- PascalCase: `WeaponType.Swinging`, `UsableItemType.Heal`
- mixed: `LootType.weapon`, `LootType.chatSkill`, `LootType.None`

### Typos that are now real names

`recieveDamage` (in `Player`, `Enemy`, `Chest`, `Destructable`), `knocback` (`Weapon` field),
`knocbackRecovery` (`EnemySO` field), `StuntMech`, `hitUntillBreak` (`Destructable` field),
`Destructable` (class), `precReady` (`Skill`).

The owner asked to keep these. Most of them are also saved in Unity scenes and assets (see
[ARCHITECTURE.md](ARCHITECTURE.md#6-what-the-unity-project-must-provide)), so renaming
them can lose data.

### The owner's playful vocabulary

"yeet" (throw something far away), `Yoink()` (pull loot away, still a placeholder),
`d_foo`, "who care". These words are part of the voice of the code. Keep them.

---

## 4. Comments

**Short, lowercase, informal.** Grammar does not matter. Often at the end of a line, sometimes
with no space after `//`:

```csharp
GameManager.Instance._Connect();//connect when first pick weapon
HP = maxHP; //test
```

**Legends for magic values:**

```csharp
public int animFacing; //up = 0, right = 1, down = 2, left = 3
public float knockbackResistance; // 1 = immune to knockback; 0 = get full knockback
public int width,height; // pixel of each cell
case 's': //s(ummon) enemy (north)
```

**Reference links** next to the code that was learned from them: Unity docs
(`targetFrameRate`, Input System `Gamepad` and `ButtonControl`), a Unity Discussions thread
(tilemap positions), and the Lexone Unity-Twitch-Chat source files on GitHub.

**Status notes instead of formal TODOs:**
`// TODO: check cooldown` · `// to do: yoink away from origin pos` ·
`// quick fix, proper wall later` · `//test` · `// buggy, use with care` ·
`// remove abstract and fix combat stat if bug` · `// collision problem` · `// who care`

**Unity's template comments stay,** even when the method under them changed. In `Title.cs`,
`// Start is called before the first frame update` sits above `SubmitInput()`.

**Not used:** XML doc comments (`///`) in the owner's own code (the only ones are in the
commented-out library copy in `TwitchChat.cs`), and `#region`.

### Commented-out code is the history, and a switch

About 376 of the 1,930 lines (~19%) are comments. Roughly 310 of them (~16% of all lines)
are commented-out code. More than 100 of those are Lexone library code (copied or adapted),
kept in `TwitchChat.cs` for reference. The project had no git history before the first commit, so
these comments are its history. They have three jobs:

1. **Old versions.** The grid-based room lookup in `DungeonMap`, the multi-spawner
   `DungeonRoom`, `updateWalkable` in `Spawner`, the per-user `SummonDB` in `SummonDB.cs`.
2. **Switches.** You change behavior by swapping which lines are commented out:

   ```csharp
       public void Spawn(Enemy enemy, Vector2 oriPos,float range){
           Enemy spawned = spawner.Spawn(enemy,oriPos,range);
       // public void Spawn(Enemy enemy){
       //     Enemy spawned = spawner.Spawn(enemy);
           var enemies = transform.Find("Enemies");
   ```

   ```csharp
       // void OnTriggerStay2D(Collider2D other){
       //     if(other.TryGetComponent(out Player player)){
       void Update(){
           var player=GameManager.Instance.player;
   ```

3. **Ideas for later.** `// if(!GameManager.Instance.availableSkill.Contains(skill))return; // who care`
   (lock chat skills until they are picked up), and the "dead enemies become summonable"
   block in `Enemy.Died()`.

**Keep them.** Do not "clean up" commented-out code.

### `if(true)` placeholders

```csharp
    public void TeleportSkill(int x, int y){
        if(true){ // TODO: check cooldown 
```

`if(true)` marks the place where a condition will go later. An AI removed this one, and the
owner put it back by hand (commit `aa1707c`). Keep these.

---

## 5. C# features

**Used:**

- target-typed `new()`: `List<Image> heart = new();`
- ranges: `"summon"[..w0len]`
- tuple swap: `(l.weapon,weaponLoot) = (weaponLoot,l.weapon);`
- expression-bodied members (`=>`) for properties, short methods and overloads
- `TryGetComponent(out Player player)` in physics callbacks
- `var` for locals, optional parameters (`Heal(int heal=1)`)
- properties with side effects (setters that update the UI)
- `?.Invoke()` on UnityEvents
- string concatenation (`"Play:"+clip.name`). In live code, interpolation appears once (`Spawner.Awake`).

**Not used:** namespaces, interfaces, the owner's own C# events or delegates (there are only
UnityEvents and the IRC library's event), LINQ, `async`/`await`, the owner's own generics,
`readonly`, `#region`.

**Logging:** mostly `print(...)`. In live code, `Debug.Log` appears once.

---

## 6. Unity patterns (the house way)

**Inspector first.** Values you might tune are `public`. Prefab wiring (animators, clips,
sprites, renderers) uses private `[SerializeField]`. Public runtime-only state uses
`[HideInInspector]`.

**Backing field + property.** The raw value is a public `_field`. Unity saves it, and the
Inspector shows it as "Max HP". The property adds the side effect:

```csharp
public int _maxHP = 4;
public int maxHP{get=>_maxHP;set{_maxHP=value;GameManager.Instance.ui.UpdateMaxHP(maxHP);}}
```

**One hub.** `GameManager.Instance` is a singleton property with a duplicate check. Other code
goes through it: `GameManager.Instance.player`, `.ui`, `.map`, `.controller`, `.summondb`,
`.currentRoom`. Short pass-through properties exist for convenience: `playerPos`, `summondb`,
`summonableEnemy`, `dungeonMap`, `currentRoom`. `SoundPlayer` uses a simpler static `instance`.

**Timers are deadlines.** Store `Time.time + duration` (or the start time) and compare later:

```csharp
public bool isOnCooldown=>lastUse+cooldown>Time.time;
invincibleTime = invincibleCooldownTime+Time.time;
hurtTime = Time.time+0.1f;
```

There is only one coroutine (the UI text box).

**Poll every frame instead of using events.** `GameManager` pulls from the summon queue.
The `Event...` components check their condition in `Update`. The UI reads cooldown percents
every frame. Values are pushed only when they change: the `HP`/`maxHP` setters update the UI,
`UpdateWeap()` runs after a pickup, and `UpdateMusic()` runs after a room switch.

**Defensive setup in `Start`.** Find or add what is missing, so a prefab works with little setup:

```csharp
if(sr==null)sr=GetComponentInChildren<SpriteRenderer>();
if(sr==null)sr=GetComponent<SpriteRenderer>();
if(sr==null)sr=gameObject.AddComponent<SpriteRenderer>();
```

The code also forces component settings at runtime. `Enemy.Start` sets `freezeRotation`,
the `"Enemy"` layer and `isTrigger`.

**Physics callbacks** use `other.TryGetComponent(out T x)`.

**Animation from code.** `Animator.Play(clip.name)` is called directly, with clip arrays in
facing order (up, right, down, left). No Animator parameters or transitions.

**Data lives in ScriptableObjects.** `[CreateAssetMenu]` puts them at the top level of the
Create menu ("Weapon", "Enemy", "Armor", "UsableItem").

**Plain `[System.Serializable]` classes** group data in the Inspector: `CombatStat`,
`HoldingWeapons`, `ControllerButton`, `Skill`, `SimpleSummonDB`, `SummonCommand`.
`[System.Serializable]` is also put where it has no effect: on `enum RoomType` and on the
MonoBehaviours `DungeonRoom` and `HealingRoom`. It does no harm. Leave it.

**Scene events.** Small components with a UnityEvent check a condition in `Update`, fire once,
`print` a message, and then destroy themselves (`EventAllEnemiesDefeated`,
`EventCertainObjectDestroyed`).

**Parallel arrays instead of dictionaries**, because the Inspector cannot show a `Dictionary`:
`roomTypeList[i]` goes with `roomMusicList[i]`. The commented-out `Dictionary` and `RoomMusic`
class show the idea that came first.

**Movement through physics.** `rb.velocity`, not `transform.position`. The comment on the old
line says why: `// collision problem`.

---

## 7. Keep these (the owner's rules)

These rules come from the owner's review of earlier AI work (branch
`claude/relaxed-babbage-llhs4x`, see [HISTORY.md](HISTORY.md)):

1. Keep the diff against the first commit small. Change only what was asked.
   No extra "while I'm here" fixes.
2. Keep the default usings, the empty `Start`/`Update` methods, the template comments,
   commented-out code, `if(true)` placeholders, the compact style, and `d_` names.
3. Do not remove unused fields or methods. The owner plans to use them.
4. Rename only names that really mislead (as `range` did). Keep typos.
5. When you rename a saved field, add `[FormerlySerializedAs("oldName")]` on its own line.
6. Do not rename, move or delete files or classes.
7. Bow and Boomerang are not implemented yet. Leave them.

---

## 8. Review checklist

Use this when you review a change, from a person or an AI:

- [ ] Only the lines that the task needs were changed? (`git diff c470dd7 -- '*.cs'`, or compare with `main`)
- [ ] Untouched lines were not reformatted (braces, spaces, blank lines, final newline)?
- [ ] Usings, empty `Start`/`Update`, and template comments are still there?
- [ ] Commented-out code and `if(true)` placeholders are still there?
- [ ] Unused fields and methods are still there?
- [ ] No saved field was renamed without `[FormerlySerializedAs]` on its own line?
- [ ] No file or class was renamed, moved or deleted?
- [ ] No public method was renamed? (It may be wired in the Inspector.)
- [ ] Enums: new values were only added at the end?
- [ ] The new code uses the same compact style as the code next to it?
