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
    [Tooltip("Keyboard: WASD moves, arrow keys aim; the mouse is ignored")]
    public bool arrowAim;
    public Vector2 movement;
    public Vector2 looking;
    public bool attack;
    public ControllerButton switchWeapon;
    public bool enableMouse=true;
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
        if(device!=ControlDevice.Gamepad)Cursor.visible = enableMouse;
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
            bool l=keyboard.leftArrowKey.isPressed, r=keyboard.rightArrowKey.isPressed, d=keyboard.downArrowKey.isPressed, u=keyboard.upArrowKey.isPressed;
            if(arrowAim){looking = new Vector2(Axis(l,r),Axis(d,u)); l=r=d=u=false;} // arrows aim, so they don't move
            if(device==ControlDevice.KeyboardMouse||arrowAim)movement = new Vector2(Axis(keyboard.aKey.isPressed||l, keyboard.dKey.isPressed||r),
                                                                                    Axis(keyboard.sKey.isPressed||d, keyboard.wKey.isPressed||u));
            attack = keyboard.kKey.isPressed;
            switchWeapon.isPressed = keyboard.qKey.isPressed;
            attackLeft.isPressed = keyboard.oKey.isPressed;
            attackRight.isPressed = keyboard.pKey.isPressed;
            jump.isPressed = keyboard.spaceKey.isPressed;
        }
        if(mouse==null||arrowAim){enableMouse = false;return;}
        Vector2 mousePos = mouse.position.ReadValue();
        CheckMouseEnable(mousePos);
        if(enableMouse && aimFrom!=null){
            looking = (Vector2)(Camera.main.ScreenToWorldPoint(mousePos) - aimFrom.position);
            attackLeft.isPressed = mouse.leftButton.isPressed;
            attackRight.isPressed = mouse.rightButton.isPressed;
        }
    }
    static float Axis(bool negative,bool positive)=>(positive?1:0)-(negative?1:0);
    // mouse aim turns on when the mouse moves far enough, and off after mouseTimeDeactivation seconds without moving
    void CheckMouseEnable(Vector2 mousePos){
        if((mousePos-mouseLastPos).magnitude > mouseDistanceActivation)enableMouse=true;
        if(enableMouse){
            if((mouseLastPos-mousePos).magnitude>0){mouseLastTime=Time.time;mouseLastPos=mousePos;}
            if(Time.time - mouseLastTime > mouseTimeDeactivation)enableMouse = false;
        }else if(Time.time - mouseLastTime > mouseTimeDeactivation){mouseLastTime = Time.time;mouseLastPos = mousePos;}
    }
}
