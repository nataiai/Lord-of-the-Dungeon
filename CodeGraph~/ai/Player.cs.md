# Player.cs <!-- 445727 -->
The streamer's character: two hands that each hold a weapon, movement and facing, attacks, animations, getting hurt, and respawning.

## Direction <!-- 797279 -->
The four facings as vectors: up, right, down, left.
Indexed by `animFacing`. `strMoveDict` (the names) is not used.

## HoldingWeapons <!-- e43ae7 -->
One hand: its weapon, cooldown, and what an attack spawns.
Any Character can hold weapons. `StopAnim` is never called.

## HoldingWeapons.Use <!-- aac98b -->
Attacks if the hand has a weapon that is off cooldown.

## HoldingWeapons.ForceUse <!-- 388eb2 -->
One attack: starts the cooldown, plays the weapon's effect animation on the user (sped up to fit the cooldown) and its sound, then spawns the hitbox in front of the user. A hitbox that stays put is turned to face the attack. The hitbox gets the weapon, the owner (so it skips their team), the buff, damage × the user's attack stat, and a velocity: one of the four facings (Experimental.fourWayAttack) or the exact look direction.

## HoldingWeapons.StopAnim <!-- c2bd49 -->
Removes the effect animation early. Nothing calls it yet.

## Player <!-- b840cb -->
The streamer's character: moves, attacks with two hands.
Setting `HP` or `maxHP` updates the hearts in the UI. `state` is IDLE, MOVING, or the WeaponType number while attacking. Not used by code yet: `speedBoost`, `activeArmor`, `playerShooting`.

## Player.Start <!-- 87b1b3 -->
Full HP, finds the controller, and remembers the start as the room entry point.

## Player.Update <!-- 321246 -->
Each frame: a dead player respawns (test behaviour); movement comes from the controller and stops during an attack (Experimental.stopMoveOnAttack); facing follows the movement or the look input (Experimental.lookFollowMovement); it moves unless stunned; each hand attacks when its button is held; then the animation updates.

## Player.TryAttack <!-- 45b2fe -->
When the hand can attack and its button is pressed: attacks, forces the attack animation to start, and sets the state to the weapon type.

## Player.InstantItemUse <!-- 319e89 -->
A heal item heals with the heal animation. Other item types do nothing yet.

## Player.UpdateAnim <!-- 6cfa72 -->
Lets a running attack animation finish (to 90%) unless a new attack forces a restart. Then picks the clip for the state and facing: idle or walk (the water versions in a healing room), or the weapon's attack (bare hands use the spear clip). Afterwards the state goes back to idle or moving.

## Player.ChangeAnimation(AnimationClip,int) <!-- 26500c -->
Picks the clip for the facing, if that array has one.

## Player.ChangeAnimation(AnimationClip) <!-- c332df -->
Plays the clip from the start unless it is already playing (a forced attack restarts it). Attack clips are sped up to fit the weapon cooldown.

## Player.recieveDamage <!-- dde550 -->
Plays the hit sound. With Experimental.enableInvincible, hits during the invincible time after a hit do nothing; otherwise HP goes down by the damage (defense is not applied) and a new invincible time starts.

## Player.receiveKnockback <!-- 610850 -->
No knockback while invincible; otherwise knockback reduced by `knockbackResistance`.
