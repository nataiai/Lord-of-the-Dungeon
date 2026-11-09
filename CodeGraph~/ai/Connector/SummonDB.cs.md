# Connector/SummonDB.cs <!-- 9f1b67 -->
The queue of enemies chat asked for, and which enemy types chat may summon.

## SummonCommand <!-- 3d569c -->
One chat request: who asked, and which enemy.

## SimpleSummonDB <!-- 1d0627 -->
Chat's summon queue, unlocked types and shared cooldown.
Chat can only summon a type after the streamer has killed one (`unlocked`); `summonableEnemy` holds a hidden copy of that first kill to clone. One cooldown (`spawnSkill`) is shared by all of chat. `limitPerPlayer` is not used yet.

## SimpleSummonDB.GetToSpawn <!-- 9ddc40 -->
Called every frame by GameManager. When the cooldown is ready, takes the next request; unknown or locked names give nothing and do not start the cooldown.

## SimpleSummonDB.Add <!-- 7e2d06 -->
Queues a request; the queue is cleared when it grows past 300.

## SimpleSummonDB.Unlock <!-- 1e8e28 -->
Lets chat summon this enemy type from now on.

## SimpleSummonDB.getEnemy <!-- 94e843 -->
The summon template for a typed name (case and spaces ignored), only if that type is unlocked.
