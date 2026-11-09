# Player/Weapons/BaseWeapon.cs <!-- eeae8c -->
The effect a weapon plays on its user (a swing, a rod flash): an animation per facing that removes itself when done.

## BaseWeapon <!-- 5971d3 -->
A weapon's effect: plays animations[facing], then goes.

## BaseWeapon.Awake <!-- fb8209 -->
Finds its Animator if none is set. This runs inside Instantiate, so it is ready before `ForceUse` plays a clip.

## BaseWeapon.Update <!-- 28a1b0 -->
Removes the effect once its animation is 90% done.
