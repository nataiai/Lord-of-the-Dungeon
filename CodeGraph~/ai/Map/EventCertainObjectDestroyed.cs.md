# Map/EventCertainObjectDestroyed.cs <!-- c5d459 -->
Fires a UnityEvent once, when a given pickup (the sword) is gone.

## EventCertainObjectDestroyed <!-- 0bcd7b -->
Runs its event once the `sword` pickup is gone.

## EventCertainObjectDestroyed.Update <!-- 902a6e -->
When the sword has been picked up: runs the event, clears the pathfinding cache, and removes itself.

## EventCertainObjectDestroyed.IsConditionTrue <!-- 0dfdb6 -->
True once the sword object no longer exists.
