using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class ControllerButton{
    [SerializeField] bool _isPressed;
    [SerializeField] HashSet<string> _isSeen=new();
    public bool isPressed{get=>_isPressed;set{_isPressed = value;if(!value)_isSeen.Clear();}}
    public bool isPressedOnce(string uniqueIdentifierString=""){ // true once per press
        if(!isPressed )return false;
        return _isSeen.Add(uniqueIdentifierString);
    }
}

public enum ControlDevice{Any,KeyboardMouse,Gamepad} // Any = 1 player: a connected gamepad replaces keyboard and mouse

public class Controller : MonoBehaviour
{
    [Tooltip("2 players: one controller KeyboardMouse, the other Gamepad")]
    public ControlDevice device;
    public Vector2 movement;
    public Vector2 looking;
    public bool attack;
    public ControllerButton switchWeapon;
    [Tooltip("Turn the mouse off: no mouse aim or clicks")]
    public bool disableMouse;
    bool mouseActive=true; // set by the script: on when the mouse moves, off after mouseTimeDeactivation still
    public float mouseDistanceActivation = 10f;
    Vector2 mouseLastPos;
    float mouseLastTime;
    public float mouseTimeDeactivation=1f;
    public ControllerButton attackLeft;
    public ControllerButton attackRight;
    public ControllerButton jump = new(); // Space / gamepad X (west)
    [Tooltip("Mouse aims from here. Empty = the first player using this controller")]
    public Transform aimFrom;

    void Update(){
        movement = device==ControlDevice.Any ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero; // these axes read gamepads too
        looking = Vector2.zero;
        UpdateInput();
        if(device!=ControlDevice.Gamepad)Cursor.visible = mouseActive;
    }
    void UpdateInput(){
        var gamepad = device==ControlDevice.KeyboardMouse ? null : Gamepad.current;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if(gamepad!=null){ // a connected gamepad replaces keyboard and mouse
            movement = gamepad.leftStick.ReadValue();
            looking = gamepad.rightStick.ReadValue();
            // attack = gamepad.buttonWest.isPressed;
            // switchWeapon.isPressed = gamepad.buttonNorth.isPressed;
            attackLeft.isPressed = gamepad.leftShoulder.isPressed||gamepad.buttonSouth.isPressed;
            attackRight.isPressed = gamepad.rightShoulder.isPressed||gamepad.buttonEast.isPressed;
            jump.isPressed = gamepad.buttonWest.isPressed;
            return;
        }
        if(device==ControlDevice.Gamepad){attackLeft.isPressed = attackRight.isPressed = jump.isPressed = false;return;} // its gamepad is unplugged
        if(keyboard!=null){
            var k = keyboard;
            movement = new Vector2(Axis(k.aKey.isPressed,k.dKey.isPressed), Axis(k.sKey.isPressed,k.wKey.isPressed)); // WASD moves
            looking = new Vector2(Axis(k.leftArrowKey.isPressed,k.rightArrowKey.isPressed), Axis(k.downArrowKey.isPressed,k.upArrowKey.isPressed)); // arrows face
            attack = keyboard.kKey.isPressed;
            switchWeapon.isPressed = keyboard.qKey.isPressed;
            attackLeft.isPressed = keyboard.oKey.isPressed;
            attackRight.isPressed = keyboard.pKey.isPressed;
            jump.isPressed = keyboard.spaceKey.isPressed;
        }
        if(mouse==null||disableMouse){mouseActive = false;return;}
        Vector2 mousePos = mouse.position.ReadValue();
        CheckMouseEnable(mousePos);
        if(mouseActive && aimFrom!=null){
            looking = (Vector2)(Camera.main.ScreenToWorldPoint(mousePos) - aimFrom.position);
            attackLeft.isPressed = mouse.leftButton.isPressed;
            attackRight.isPressed = mouse.rightButton.isPressed;
        }
    }
    static float Axis(bool negative,bool positive)=>(positive?1:0)-(negative?1:0);
    // mouse aim turns on when the mouse moves far enough, and off after mouseTimeDeactivation seconds without moving
    void CheckMouseEnable(Vector2 mousePos){
        if((mousePos-mouseLastPos).magnitude > mouseDistanceActivation)mouseActive=true;
        if(mouseActive){
            if((mouseLastPos-mousePos).magnitude>0){mouseLastTime=Time.time;mouseLastPos=mousePos;}
            if(Time.time - mouseLastTime > mouseTimeDeactivation)mouseActive = false;
        }else if(Time.time - mouseLastTime > mouseTimeDeactivation){mouseLastTime = Time.time;mouseLastPos = mousePos;}
    }
}
