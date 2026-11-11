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

Keys      a weapon with a keyId is a key. A Door / Chest / Lock with a keyId is locked;
          the key with the same keyId opens it, swung or thrown.
          consumeKey  key is used up (default off)

Loot      dropped loot slides and bounces. Tweak LootYoink.speed / deceleration / bounciness.

PlayOnce  AI/PlayOnce.cs: play an animation once, then hide / destroy. Used by Door and the heal effect.
