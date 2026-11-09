# Data Types/Enemy.cs <!-- 7d5834 -->
The enemy type asset (Create → Enemy): one per kind of enemy, in Database/Enemies.

## EnemySO <!-- 39bac8 -->
One enemy type: name chat types, speed, HP, attack, defense.
`enemyName` is the word chat types to summon it. `ignoreCollide` makes it fly: it goes straight through walls and over holes (ghosts). `knockbackResistance` 1 means no knockback; `knocbackRecovery` is how fast it stops sliding after one.
