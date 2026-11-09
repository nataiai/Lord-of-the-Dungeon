# Map/HealingRoom.cs <!-- 666e53 -->
A trigger area (a spa or pool) that heals the characters of chosen teams while they stand in it.

## HealingRoom <!-- f2a2e0 -->
Heals characters of the ticked teams standing inside.
`heals` is a flags field: tick Player, Enemy or Neutral. Characters inside get `isHealing`, which makes the player use the in-water idle and walk clips.

## HealingRoom.OnTriggerEnter2D <!-- 2da0b1 -->
A character of a healed team steps in: it joins the list and starts healing. The first one in starts the heal timer.

## HealingRoom.OnTriggerExit2D <!-- 96d570 -->
A character on the list steps out: it stops healing.

## HealingRoom.Update <!-- b81c3c -->
`HPHealPerSecond` times a second: forgets characters that were destroyed, heals each living one by 1 HP with the heal animation (the dead stay dead), and plays the heal sound once.
