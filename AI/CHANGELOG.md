# Weapons update: throwing, boomerang, keys, loot slide

All logic in AI/. On/off: Experimental > Weapons (throwing, boomerangReturns, yoink, locks).

Throw     hold Q / pad X + weapon button. Lands as loot, hand goes back to Fist.
          Weapon > Throw Settings (foldout):
          deceleration  0 = flies until a wall
          maxHits       0 = no limit
          spinSpeed     0 = flies point first (a boomerang spins with this too)
          spriteAngle   which way the art points (0 = right)

Boomerang type Boomerang: its attack comes back through walls.
          boomerangDeceleration  slow-down going out

Keys      weapon with isKey opens a Lock it hits, swung or thrown. Door is a Lock.
          hasLock / keyId / consumeKey on the Door or Chest
          keyId  must match the key's keyId (empty on the Door / Lock = any key)

Loot      dropped loot slides and bounces. Tweak LootYoink.speed / deceleration / bounciness.
