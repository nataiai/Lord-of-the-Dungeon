# Controller.cs <!-- 65f7d7 -->
Player input: gamepad, keyboard and mouse, read once per frame into fields that Player and UI use.

## ControllerButton <!-- cf4c5e -->
A button that can be asked "pressed just now?" once per caller.

## ControllerButton.isPressed.set <!-- 69156d -->
Stores the button state; letting go clears the memory of who already saw this press.

## ControllerButton.isPressedOnce <!-- 46c9bd -->
True only the first time each caller (by id string) asks during one press. The dialogue uses this so one click advances one line.

## Controller <!-- ea66bd -->
Reads gamepad, keyboard and mouse into movement, look and attack.

## Controller.Update <!-- ac4bd8 -->
Starts from the old Input axes, then lets `UpdateInput` overwrite with the Input System, and shows the cursor only while the mouse is in use.

## Controller.UpdateInput <!-- 537068 -->
A connected gamepad replaces keyboard and mouse: left stick moves, right stick looks, shoulders or south/east buttons attack. Otherwise WASD moves, arrows look, O and P attack (K and Q are read but not used for attacks). Then the mouse, unless Experimental.disableMouse: once it wakes up it aims at the cursor and its buttons attack.

## Controller.CheckMouseEnable <!-- 04726b -->
The mouse wakes up when it moves farther than `mouseDistanceActivation` and goes back to sleep after `mouseTimeDeactivation` seconds without moving.
