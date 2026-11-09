# Lord of the Dungeon
A top-down dungeon game that a streamer plays while chat fights back: chat types `summon <enemy>` to send enemies at the streamer, but only kinds the streamer has already killed. These notes are written by the AI; the graphs below them are drawn by the generator.

**The parts.** `GameManager` is the hub everything reaches through `GameManager.Instance`. It holds the player, UI, input, map and chat. `Player` and `Enemy` share `Character` (HP, team, knockback, stun). An attack is a `HitBox` spawned by one of the player's two hands (`HoldingWeapons`); weapons, enemy types and items are ScriptableObject assets in `Data Types/`. The dungeon is a `DungeonMap` of `DungeonRoom`s; only the room the player is in is active. `Experimental` holds every on/off switch for features still in testing.

**External code.** `AI/` holds code written by the AI and is drawn as outside boxes only: `PathFinding` (enemies walk around walls and holes and keep apart), `UnifiedChat` (reads Twitch and YouTube chat with no package, used when `Experimental.unifiedChat` is on), and `PlayModeOnly` (the `[PlayModeOnly]` Inspector attribute).

## A chat message becomes an enemy
```mermaid
sequenceDiagram
    participant V as Viewer in chat
    participant R as TwitchChat or UnifiedChat
    participant C as Chat
    participant Q as SimpleSummonDB
    participant G as GameManager
    participant S as DungeonRoom and Spawner
    V->>R: "summon skeleton"
    R->>C: addToDB(user, message)
    C->>C: Categorize: summon
    C->>Q: Add(SummonCommand)
    loop every frame
        G->>Q: GetToSpawn()
        Q-->>G: a summon template, if the cooldown is ready and the type is unlocked
    end
    G->>S: Spawn near the player
    S->>S: search free cells on a 0.5 grid, pick one at random
```

## An attack lands
```mermaid
sequenceDiagram
    participant I as Controller
    participant P as Player
    participant H as HoldingWeapons
    participant B as HitBox
    participant E as Enemy
    participant L as Loot and SimpleSummonDB
    I->>P: button held (read every frame)
    P->>H: TryAttack, then Use
    H->>B: ForceUse spawns the hitbox (and the effect animation)
    B->>E: OnTriggerEnter2D: recieveDamage and receiveKnockback
    E->>E: name shown on screen, HP minus defense
    E->>L: HP 0: drop loot, unlock this type for chat, store a summon template
    E->>E: first kill joins chat (GameManager._Connect)
```

## The player goes through a door
```mermaid
sequenceDiagram
    participant T as RoomTrigger
    participant M as DungeonMap
    participant R as Rooms
    participant O as SoundPlayer and PathFinding
    T->>M: OnTriggerExit2D: roomTriggerExit
    M->>M: moving toward the paired trigger?
    M->>R: switchRoom: new room on, old room off
    M->>R: each player remembers where it entered (holes respawn it there)
    M->>O: room music, clear the pathfinding cache
```
