# Weapons update: throwing, boomerang, keys, loot slide

All logic in AI/. On/off: Experimental > Weapons (throwing, boomerangReturns, yoink, locks).

Throw     hold Q / pad X + weapon button. Lands as loot, hand goes back to Fist.
          throwDeceleration  0 = flies until a wall
          throwMaxHits       0 = no limit
          spinSpeed          0 = flies point first
          spriteAngle        which way the art points (0 = right)

Boomerang type Boomerang: its attack comes back through walls.
          boomerangDeceleration  slow-down going out

Keys      weapon with isKey opens a Door (hasLock) or Lock it hits, swung or thrown.
          keyId       on the key and on the Door / Lock: must match (empty on Door / Lock = any key)
          consumeKey  key is used up
          Lock only: hideOnUnlock (hide the closed door), onUnlock

Loot      dropped loot slides and bounces. Tweak LootYoink.speed / deceleration / bounciness.
