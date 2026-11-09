# Enemy.cs <!-- 5fb4eb -->
Every enemy: chases the player around walls, hurts on touch, drops loot, and once killed becomes something chat can summon.

## Enemy <!-- 3394e4 -->
Chases the player, hurts on touch, unlocks chat summons.
Its numbers come from `enemyData` (an EnemySO asset). It lives at most `lifeSpan` (360 s), then dies by itself without unlocking anything. The clip arrays `enemyIdle`, `enemyWalk` and `enemyAttack` are not used by code.

## Enemy.Start <!-- 606b72 -->
Full HP, remembers its sprite color, puts itself on the Enemy layer and starts as a trigger, so a fresh spawn never gets stuck inside a wall or another enemy.

## Enemy.Update <!-- 2a9775 -->
Each frame: once it touches nothing it becomes solid (unless it flies); the hurt color fades; it dies when its time is up or HP is 0. Unless stunned, it faces the player and, after noticing them (within `noticeRange`, then for good), walks toward them: around walls with PathFinding, straight if it flies, kept apart from other enemies by Separation.

## Enemy.Died <!-- 5812bd -->
Runs every frame from the moment it dies. The first call joins chat (the first kill starts the stream connection), drops loot and, if it was killed rather than timed out, unlocks its type for chat and stores a summon template. Half a second later it is destroyed.

## Enemy.AddSummonTemplate <!-- 25cd09 -->
If chat has nothing to clone for this type yet, makes a hidden, inactive copy of this enemy (normal color, no loot) and adds it to the summonable list.

## Enemy.recieveDamage <!-- 4e5654 -->
Shows the enemy's name on screen, so chat learns what to type. A living enemy flashes red and loses the damage minus its defense (magic or physical), at least 1.

## Enemy.OnTriggerStay2D <!-- 504640 -->
Touch damage: a Character of another team touching it is knocked back and hurt, at most once per `cooldown`, and never while it is dying.
