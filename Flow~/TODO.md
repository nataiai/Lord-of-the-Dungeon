# To do (found by Dungeon Flow's checks; nothing changed)

- Blocked hits still play the hit sound: `Player.cs:190` plays it before the invincible
  and shield checks (191-192).
- A dead enemy still shows its name when hit: `Enemy.cs:123` shows it before
  `if(isDead)return;` (124).
- Bats are named "skeleton": `Database/Enemies/Bat.asset` has `enemyName: skeleton`,
  which `Enemy.cs:123` shows on a hit.
- The `maxHP` setter never runs (`Player.cs:69`): nothing sets `maxHP`, so the hearts are
  fitted once in `GameManager.Start` (`GameManager.cs:46`) and never again.
