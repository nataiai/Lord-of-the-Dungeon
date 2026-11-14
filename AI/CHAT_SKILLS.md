# Chat skills: final design (chatter first)

Design only. Nothing is built or run. Every code claim was checked against the scripts. The repo has no scenes or prefabs, so prefab facts are marked "check".

## The idea

- A chatter owns two things: their **turns** (exists today) and **one voice**.
- **Alone** (summon, potion, teleport) = a fair line. Fewest turns goes first.
- **Crowd** (berserk, invincible, shields, earthquake, float) = a vote. Typing the word is your voice. It fires when enough chatters type the same word.
- One small vote class runs the buff choice, earthquake, float and the weapons room.
- Every effect is the existing `Buff`, put on the player or the enemies.
- No per-chatter cooldowns, points or saved data.

## 1. Chatter model

Stored per chatter (key = username, like `turns`):

| What | Status | Used for |
|---|---|---|
| `turns` | exists | order in the summon, potion and teleport lines |
| `seen` | new | time of their last valid command |
| 1 teleport request | new | a newer one replaces it |
| 1 ballot per open vote | new | typing again moves it |

**Alone = a line.** Each cooldown: fewest turns first, then the oldest request. `Next` / `Count` are reused unchanged. A turn counts when the game tries your request, also if it fails (potions work like that today).

**Crowd = a voice.** `need` chatters must type the same word inside one vote.

- `need = max(1, round(crowdNeed x sqrt(active)))`
- active = chatters with any valid command in the last 120 s. Fixed when the vote opens.

| Active chatters | Need |
|---|---|
| 1-2 | 1 |
| 3-6 | 2 |
| 7-12 | 3 |
| 13-20 | 4 |
| 21-30 | 5 |
| 100 | 10 |
| 500 | 22 |

**How often can I?**
- Teleport: when it is your turn. With K askers, about one each per K x 30 s.
- Crowd word: any time. You are one voice per vote. The skill's cooldown limits the streamer, not you.
- A crowd cast costs no turn, so helping with float never delays your summon.

**What stops one spammer**
- 100 x "float" = 1 ballot. 100 teleports = 1 request.
- Each served request puts them behind everyone with fewer turns.
- They count as 1 active chatter.
- Skill words need 3+ letters, so "F" or "E" spam is not a vote.
- Hole in today's code, 1 line to fix: `Add` wipes the whole summon line at 300 requests, and `limitPerPlayer` (10) is unused. Honour it in `Add`.
- Left over: one troll can keep a failing vote line on screen. It is one line of text.
- Many accounts (bots) can't be stopped in the game. Raise `crowdNeed`; the rest is for Twitch mods.

**1 chatter vs 500**
- 1-2: need 1. Every word casts at once; only cooldowns limit. This is also how you test alone.
- 5: need 2. Nobody fires a crowd skill alone.
- 500: 22 same words within 20 s. One teleport per 30 s, to the asker with the fewest turns.

**How a chatter knows it worked.** The game can't write to chat, so one text label on screen shows:
- "+Bob" beside the tally when his word counted.
- "Bob teleports you to 3 4", or "Bob: 3 4 is blocked".
- "EARTHQUAKE! Ann +4" (Ann opened the vote).
- Why not: "earthquake in 12s", "floating is locked", "no enemies here".
- When nothing is happening: the words that are unlocked and ready. That is the manual.

**Built around Twitch**
- Stream delay is 3-10 s: votes last 20 s, no buff is under 10 s.
- Twitch blocks the same message twice in 30 s: prefixes give variants (float, floa, flo).
- Chatterino adds an invisible word to a repeat, which breaks 1-word commands and "vote 2". One line at the top of `addToDB` strips it (`AddPotion` already does this).

## 2. Commands

A prefix is enough. First matching row wins, as today.

| Type this | Who | Does |
|---|---|---|
| `summon skeleton` / `spawn` | alone | unchanged |
| `potion` | alone | unchanged |
| `teleport 3 4` | alone | fair line + cooldown (new) |
| `vote 2` / `vote spear` | crowd | ballot in the room vote; else same as the skill word |
| `berserk` | crowd | buff vote |
| `invincible` | crowd | buff vote |
| `magicshield` ("magic") | crowd | buff vote |
| `physicalshield` ("phys") | crowd | buff vote |
| `earthquake` / `quake` | crowd | own vote |
| `floating` ("float") | crowd | own vote |
| `work x` | - | still nothing |

- Enum, new ones at the end: `...,potion,berserk,invincible,magicShield,physicalShield,earthquake,floating`. (`float` is a C# keyword.)
- New rows are **1 word only**. With 2 words, "magic trick" or "float away" would vote.
- New words need **3+ letters** ("flo", "ear", "ber", "inv", "mag", "phy", "qua"). Checked in AI/, `Categorize` untouched.
- Clashes: "p", "po" alone stay potion (its row is first); "ph..." is the shield. b, i, m, e, q, f are free. The old rows need 2+ words (potion aside), so they never take a 1-word skill.
- Never add a row starting with "s": "s x" already summons.

## 3. The one vote (`ChatVote`)

One class, about 40 lines. It holds: options, ballots (chatter -> option), end time, `need`, a callback.

- One ballot per chatter. Typing again moves it.
- A word picks by number ("2") or by how the name starts ("spe").
- **need > 0** (crowd skills): the first option with `need` ballots wins at once. Time up: nothing happens, no cooldown used.
- **need 0** (weapons room): runs its whole time. Most ballots wins. Tie: random. Zero votes: "nobody".

**Crowd votes**
- Each skill has a `group` number. Same group = one vote and one shared cooldown.
- Defaults: the four buffs are group 1, earthquake 2, float 3. So "vote for 1 buff" is one vote, and earthquake and float run beside it.
- Opens when a chatter types a word that is unlocked and off cooldown. Options: the ready skills of that group.
- 20 s (`voteTime`).
- A counter-vote never helps the other side: voices must agree on the same word.
- Survives room changes and deaths, so the streamer can't dodge it through a door.
- Shows only options with votes: `berserk 1/2 · invincible 2/2 +Ann  9s`

**Room vote** (weapons room)
- Opened by the room, 30 s, need 0. Vote with `vote 2` or `vote spear`.
- Shows every option: `RIGHT HAND  1 sword 4 · 2 spear 2 · 3 rod 0  22s  (vote 2)`
- A crowd vote and the room vote can be open together; a chatter has one ballot in each.
- Later, same call, nothing built now: `vote left` for doors (your "vote leftdoor" comment).

## 4. Skills

| Skill | Group | Cooldown | Lasts |
|---|---|---|---|
| teleport | line | 30 s | - |
| berserk | 1 | 60 s | 15 s |
| invincible | 1 | 60 s | 10 s |
| magicshield | 1 | 60 s | 15 s |
| physicalshield | 1 | 60 s | 15 s |
| earthquake | 2 | 45 s | 3 s stun |
| floating | 3 | 30 s | 10 s |

All are Inspector data (TwitchChat > Summondb > Chat Skills).

### 4.1 Teleport (chatter skill)

- **Type:** `teleport 3 4` or `t 3 4`. x y are tiles of the current room. `1 1` is the bottom-left floor tile. A ruler on the room edge shows the numbers.
- **Takes:** 1 chatter. One request each.
- **Happens:** when `teleportSkill` is ready, the asker with the fewest turns is picked. A marker shows on the tile for 1 s, then the player jumps. The rest of the line is cleared: ask again. So no request is older than one cooldown.
- **Blocked spot:** only that request is dropped. No cooldown used. The next asker is tried.
- **Stream shows:** the marker, "Bob teleports you to 3 4", slot 3 reloading.
- **Code:** `teleportQ` + `teleportSkill` beside `potionQ` / `potionSkill`. `GameManager.TeleportSkill` stays the only place that checks and moves. It returns bool, takes floats (tile centres), and `move=false` only asks "is it free" (for the marker). The `if(true){ // TODO` line stays; the caller checks the cooldown.
- **Mixes / abuse:**
  - Can't land on a hole, wall, enemy, loot, hot spring or doorway: `OverlapPoint` counts every collider, triggers too (Unity default; check Physics 2D settings). An invisible trigger is a no-teleport zone, no code.
  - Troll use: beside enemies or a pit edge. Limits: 30 s, fair order, the 1 s marker.
  - **Bug guard:** today, teleporting the player out of a `RoomTrigger` while he walks to the door fires `roomTriggerExit` -> `switchRoom`, and the room he stands in is hidden. Rule: no teleport while the player is in a doorway or falling; the request waits.
  - A room change clears the line (numbers are per room).
  - Turns are shared with summon and potion: heavy summoners teleport later.

### 4.2 Buff vote: berserk / invincible / magicshield / physicalshield

- **Type:** one of the four words. It opens or joins the buff vote and picks that buff. Another buff word moves your voice.
- **Takes:** `need` chatters on the same buff, within 20 s.
- **Happens:** the winner's `Buff` goes on the player with `CopyTo` (first buff ever put on the player). All four start their cooldown together, so only one is active.
- **Stream shows:** the tally line, then "INVINCIBLE! Ann +1", the buff's effect prefab on the player while it lasts, the slots reloading.

**Berserk** (15 s)
- Attack x (2 + `power` x share of HP missing), power 2. With 4 hearts: x2, x2.5, x3, x3.5.
- Sword1 (3): 6, 7, 9, 10. Rapier (1): 2, 2, 3, 3. Zweihander (6): 12, 15, 18, 21.
- No healing at all.
- Code: `Weapon.Damage` (the one formula: swings, charge skills, throws) x `ChatSkill.Berserk(by)`. `if(Has(BuffType.berserk))return;` in `Character.Heal` and `HealWithEffect`: covers the Health Bone, the hot spring and heal buffs, and no heal glow plays.
- Mixes: a Health Bone walked over is used up for nothing. Never with invincible or a shield (same group). Fine with earthquake and float.
- Abuse: chat fires it at 1 heart beside the hot spring. That is its price; at most 15 s of every 60.
- It runs on after a death (`Died` refills HP in the same frame).

**Invincible** (10 s)
- No damage, no knockback, no earthquake stun. Still falls in holes.
- Code: one line in `Player.recieveDamage`, next to the i-frame check: `if(ChatSkill.Blocks(this,isMagical))return;`. It serves invincible and both shields. `receiveKnockback` gets `|| Has(BuffType.invincible)`.
- Works with `enableInvincible` on or off. Never touches `invincibleTime`.

**Magic shield** (15 s)
- Blocks magic damage. Knockback still pushes.
- **Today nothing deals magic damage to the player:** `Enemy.OnTriggerStay2D` always uses `attackPhysic`. Needs `EnemySO.isMagical` (Ghost). See question 2.

**Physical shield** (15 s)
- Blocks physical damage: today every enemy touch and burn tick.
- Knockback still pushes, earthquake still stuns. That keeps it different from invincible.

### 4.3 Earthquake (chat skill)

- **Type:** `earthquake` or `quake`. Its own vote.
- **Takes:** `need` chatters within 20 s.
- **Happens:** a stun `Buff` (3 s) on every enemy in the room, and on the player too, **unless** the player has the invincible buff, is in invincibility frames, or is floating.
- Chests are skipped. `EnemySO.buffResistance` is respected (boss, flyers).
- Refused in a room with no enemies: it would only stun the streamer.
- Player stun = can't walk, can still attack and turn (today's stun).
- **Stream shows:** `earthquake 3/5 +Bob  12s`, then "EARTHQUAKE! Ann +4", a dust / rumble prefab, stun stars on everyone stunned.
- **Mixes:**
  - Combo: invincible (10 s), then earthquake. Both votes run side by side, so chat can time it.
  - Stunned enemies still hurt on touch (no stun check there).
  - Berserk + earthquake: stunned enemies, big hits. Two votes.
- **Abuse:** stunning the streamer on purpose: 3 s per 45 s, and it needs `need` chatters. His counter: ask chat for invincible or float.

### 4.4 Float (chat skill)

- **Type:** `floating` or `float`. Its own vote.
- **Happens:** flag `floating` on the player for 10 s. Holes ignore a floating player.
- **Code:** `Map/Hole.cs`, 2 edited lines: `OnTriggerEnter2D` becomes `OnTriggerStay2D` (a float that ends over a hole drops the player), and the skip test gets `|| c!=null && c.Has(BuffType.floating)`. AI sets the player's body to never sleep when float starts; no prefab setting needed.
- Spike tiles and ground-impact attacks don't exist yet. When made, their first line is `if(c.Has(BuffType.floating))return;`. Nothing is built for them now.
- **Stream shows:** `floating 2/3 +Bob  14s`, "FLOATING! Ann +2", wings and a shadow prefab while it lasts.
- **Mixes:**
  - Ends over a hole: the player falls and respawns at the room entry, no damage (today's hole).
  - Enemies still hit; walls still block; safe from earthquake.
  - Teleport still can't target a hole.
  - It crosses pit rows used as a wall. Where crossing is not allowed, use a wall.
- With stream delay it is no reflex save: the streamer waits at the pit and asks chat.
- **Abuse:** casting it in a room without holes to burn the cooldown: a 20 s gap, and it needs `need` chatters.

### 4.5 Weapons room (chat votes the right hand)

- **Type:** `vote 2` or `vote spear`. The room opens the vote when the player is in it.
- **Takes:** any number. 30 s. Most votes wins, tie random.
- **Happens:** the winner goes in the right hand and is `chatFixed`: loot can't fill or swap it, it can't be thrown. The left hand stays free.
- The old right-hand weapon drops on the winner's pedestal. (Not at the player: `Loot.Drop` parents to `from.parent`, which for the player is not the room.)
- **Zero votes, or nobody active** (then it ends at once): the weapons drop as normal loot. Free pick, nothing fixed.
- `onVoted` event: wire it to the exit `Door.OpenDoor`, so the streamer can't walk out of the vote. Not wired: leaving cancels the vote, coming back restarts it.
- **Stream shows:** numbered pedestals, the vote line, "chat picked spear", the pickup sound, the right weapon icon changes.
- Fixed until the next weapons room. Death keeps it (death keeps weapons today).
- Don't offer keys: a used-up key empties the fixed hand.
- Same start (RodFire1, RodIce1): "vote rod" takes the first; numbers avoid it.
- Trolling = voting the worst weapon. That is the room's point.

## 5. Unlocks

Nothing reads `GameManager.availableSkill` today; `Loot.PickChatSkillHandle` only fills it.

- One check in AI/: `Experimental.chatSkills && (!skillLoot || availableSkill.Contains(type))`.
- `skillLoot` off (default): every skill in the list works. Good for testing.
- `skillLoot` on: teleport and the six crowd skills work only after their Chat Skill loot (Loot Type = Chat Skill, Skill = floating...). `Loot.cs` needs no change.
- Start skills: add them to GameManager > Available Skill.
- Pickup shows "chat unlocked: floating". The ready-words list shows only unlocked words.
- Summon, potion and vote are never gated. The weapons room is gated by reaching it.
- Unlocks survive death (`_re` doesn't touch the list).

## 6. Experimental toggles

Under `[Header("Chat")]` in `Experimental.cs`:

| Toggle | Default | Meaning |
|---|---|---|
| `chatSkills` | off | Master. Off = today: teleport jumps at once by world position, new words and votes ignored |
| `crowdNeed` | 1 | need = this x sqrt(active). 0 = one chatter is enough. 2 = stricter (45 of 500) |
| `skillLoot` | off | On = a skill needs its Chat Skill loot |
| `teleportRoomTiles` | on | On = x y are room tiles. Off = world position |

The last three are `[ShowIf("chatSkills", true)]`. Not experimental, so Inspector data on Summondb: Teleport Skill, Chat Skills, Vote Time 20, Active Time 120, Teleport Warn 1.

## 7. Where the code goes

**Your files: about 36 new lines, 15 edited.**

- `Connector/Chat.cs`: +12, 2 edited. Enum (second line for the new names), 7 rows, 4 hooks in `addToDB`, teleport case.
- `Connector/SummonDB.cs`: +5. `teleportSkill`, `teleportQ`, `chatSkills`, the three times, the `limitPerPlayer` guard in `Add`.
- `GameManager.cs`: +2, 2 edited. `summondb.ChatUpdate();` in Update; `TeleportSkill`.
- `BuffEffect.cs`: 5 edited. Enum gets `berserk=128,invincible=256,magicShield=512,physicalShield=1024,floating=2048`. Three `[ShowIf]` lists become `~BuffType.none` (any type): shorter, and never touched again. `power` gets `BuffType.berserk`. `Apply()` is untouched.
- `Character.cs`: +2, 1 edited. `Has`, the two heal guards.
- `Player.cs`: +1, 1 edited.
- `Map/Hole.cs`: 2 edited.
- `Loot.cs`: 2 edited (`TryFill`, `Swap`).
- `UI.cs`: +3. `chatText`, its `SetText` (null-guarded), one loop line for slots 3+.
- `Experimental.cs`: +11.
- Only with magic damage: `Data Types/Enemy.cs` +1, `Enemy.cs` 1 edited, `Ghost.asset` fills the new field.

The hooks you will read:

```cs
// Chat.cs, addToDB
msg = msg.Replace("\U000E0000",""); // Chatterino adds that to a repeat
if(Categorize(msg)!=ChatType.invalid)summondb.Seen(username);
case ChatType.teleport: if(...)summondb.AddTeleport(username,x,y); break;
case ChatType.vote: summondb.Vote(username,words[1]); break;
default: summondb.Call(username,Categorize(msg),words); break; // chat skills

// GameManager.cs
public bool TeleportSkill(float x, float y, bool move=true){
    ...{if(move)player.transform.position = pos; return true;}
    ...
    return false;

// Character.cs
public bool Has(BuffType t){foreach(var b in GetComponents<BuffEffect>())if((b.buff.type&t)!=0)return true; return false;}
if(Has(BuffType.berserk))return; // Heal, HealWithEffect

// Player.cs
if(ChatSkill.Blocks(this,isMagical))return; // recieveDamage
if(enableInvincible && isInvincible || Has(BuffType.invincible))return; // receiveKnockback

// Loot.cs
if(hand.chatFixed || hand.weapon!=null && hand.weapon.type!=WeaponType.CQC)return false;
void Swap(HoldingWeapons hand){if(hand.chatFixed)return; ...

// UI.cs
if(chatText)chatText.SetText(GameManager.Instance.summondb.ChatText());
```

**AI/: about 210 lines** (the earlier 135 estimate was too low).

- `AI/ChatVote.cs` (new, ~40): the vote.
- `AI/ChatSkill.cs` (new, ~125): `class ChatSkill : Skill {group, buff, castEffect}` and its six defaults; partial `SimpleSummonDB` (so it uses the private `turns`, `Next`, `Count`): seen / need, `Call` / `Vote` / `Cast`, the teleport line, the text, the ruler; static `Berserk`, `Blocks`.
- `AI/WeaponRoom.cs` (new, ~30).
- `AI/Fields.cs`: +1 (`chatFixed`), 1 edited (`Weapon.Damage`).
- `AI/ThrownWeapon.cs`: 1 edited (`hand.chatFixed ||`).
- `AI/ChatPotion.cs`: 1 edited (`NewRoom` also clears `teleportQ` and the room vote).
- `AI/CHANGELOG.md`: one section.

Reused, not rewritten: `Skill`, `SummonCommand` (its unused `spawnableLocation` holds "x y"), `turns` / `Next` / `Count`, `Buff.CopyTo` and `BuffEffect`, `buffResistance`, `PlayOnce.Spawn`, `Loot.Drop`, `availableSkill` + Chat Skill loot, `ShowIf`.

Left out on purpose: per-chatter cooldowns or points, a "no" ballot, answers in chat, door votes, spike tiles, shields that count hits. Don't name a class `Chatter`: Lexone's is used in `TwitchChat.cs`.

## 8. What you make in Unity

**Must**
- UI prefab: one TMP text (about 4 lines, top centre) on UI > Chat Text. Without it chatters see nothing. (`ShowText` can't be reused: every enemy hit overwrites it.)
- Tick Experimental > Chat > chatSkills.
- Weapons room: a room with `WeaponRoom`. Weapons = the Weapon assets. Spots = one SpriteRenderer child per weapon, on pedestal art with 1, 2, 3 painted. On Voted -> the exit Door's `OpenDoor`.

**Should**
- Looping buff effect prefabs (Chat Skills > Buff > Effect): berserk aura, invincible glow, magic bubble, physical bubble, float wings + shadow, stun stars.
- One-shot prefabs (Cast Effect): earthquake dust / rumble, teleport marker (1 s long).
- Skill Slot children 3 to 9, built like 1 and 2: teleport, then Chat Skills in list order. Put the word on each icon. Fewer children = fewer shown.
- Enemy assets: tick stun in Buff Resistance on bosses and flyers (Bat, Ghost) the earthquake should skip.

**Check**
- Grid cell size is 1 unit (the tile numbers assume it).
- Player > Invincible Cooldown Time is above 0, or the "invincibility frames" rule never applies.

**Only if used**
- Chat Skill loot prefabs, when `skillLoot` is on.
- Invisible triggers where teleport must not land.
- A Twitch panel listing the commands.

Made by code, no content: the teleport ruler (TMP numbers on the room's bottom and left edge, like the mini map's "?").

## 9. Build order

Each step works alone.

1. **Teleport chat can use** (~15 lines yours, ~55 AI). The line, `TeleportSkill` bool, room tiles, the ruler, the text label, the doorway guard, the Chatterino line, `chatSkills` + `teleportRoomTiles`. Closes your TODO.
2. **The vote + earthquake** (~8 yours, ~85 AI). `ChatVote`, seen / need, `Call` / `Cast`, the tally lines, `crowdNeed`. Earthquake first because stun exists: no `BuffType`, `Player` or `Hole` change.
3. **Float** (~9 yours, ~3 AI). The flag, `Has`, the two Hole lines, one row.
4. **Player buffs** (~10 yours, ~12 AI). Four flags and rows, the two Player lines, the heal guards, `Damage` x berserk, the group cooldown. Magic damage if you say yes.
5. **Weapons room** (~3 yours, ~35 AI). `WeaponRoom.cs`, the vote case, `chatFixed` and its three guards.
6. **Polish.** Slots 3+, the ready-words list, `skillLoot` + "chat unlocked", the 1 s marker, the `limitPerPlayer` line, CHANGELOG.

Test each step with one account: need is 1, so every vote closes at once.

## 10. Open questions (with my pick)

1. **Teleport numbers:** room tiles with a ruler (`1 1` = bottom left), or world positions as today? *Pick: room tiles. Chatters can't know world positions.*
2. **Magic damage:** add `EnemySO.isMagical` and tick it on Ghost, so the magic shield blocks something? *Pick: yes (3 lines + 1 asset field). If no: remove magicshield from the Chat Skills list.*
3. **One buff at a time?** As designed the four share a cooldown. *Pick: yes. Stacking is only a `group` number change later, no code.*
4. **Earthquake mercy rules:** (a) count invincibility frames even while `enableInvincible` is off (the timer runs either way)? (b) is a floating player safe? *Pick: yes to both. Without (a) your frames rule does nothing today; (b) fits "float evades ground impact".*
5. **Berserk and potions:** a Health Bone walked over during berserk is used up for nothing. *Pick: keep it (0 lines). The streamer can walk around it for 15 s.*
6. **Weapons room:** fixed until the next weapons room, also through death; nobody votes = free pick; exit closed until the vote ends. *Pick: all three as designed.*
7. **Vote length:** 20 s. Is your stream on low latency? *Pick: 20 s on Twitch low latency; 30 s otherwise or on YouTube.*
8. **Which skills are loot?** *Pick: `skillLoot` off while testing. For release: teleport from the start, then float (before the first pit room), earthquake, invincible, shields, berserk last.*
9. **Player stun from the earthquake:** can't walk but can attack (today's stun), or no attacks either? *Pick: keep today's stun (0 lines).*