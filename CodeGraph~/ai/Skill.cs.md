# Skill.cs <!-- 9cd816 -->
A cooldown for a chat skill. Chat's summons share one (`SimpleSummonDB.spawnSkill`).

## Skill <!-- 5f72b1 -->
A chat skill's cooldown, with the reload amount for the UI.
Not a MonoBehaviour, so its `Start` never runs and a new Skill is ready at once.

## Skill.Call <!-- d3c494 -->
Uses the skill if it is ready (true) and starts the cooldown; false while it reloads.
