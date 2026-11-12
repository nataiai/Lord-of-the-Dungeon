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

public class Controller : MonoBehaviour
{
    [PlayModeOnly] public Vector2 movement;
    [PlayModeOnly] public Vector2 looking;
    [PlayModeOnly] public bool attack;
    [PlayModeOnly] public ControllerButton switchWeapon;
    bool disableMouse => Experimental.I.disableMouse;
    bool mouseActive=true;
    public float mouseDistanceActivation = 10f;
    Vector2 mouseLastPos;
    float mouseLastTime;
    public float mouseTimeDeactivation=1f;
    [PlayModeOnly] public ControllerButton attackLeft;
    [PlayModeOnly] public ControllerButton attackRight;
    [PlayModeOnly] public ControllerButton throwHold = new();

    void Update(){
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        looking = Vector2.zero;
        UpdateInput();
        Cursor.visible = mouseActive;
    }
    void UpdateInput(){
        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if(gamepad!=null){
            movement = gamepad.leftStick.ReadValue();
            looking = gamepad.rightStick.ReadValue();
            // attack = gamepad.buttonWest.isPressed;
            // switchWeapon.isPressed = gamepad.buttonNorth.isPressed;
            attackLeft.isPressed = gamepad.leftShoulder.isPressed||gamepad.buttonSouth.isPressed;
            attackRight.isPressed = gamepad.rightShoulder.isPressed||gamepad.buttonEast.isPressed;
            throwHold.isPressed = gamepad.xButton.isPressed;
            return;
        }
        if(keyboard!=null){
            var k = keyboard;
            movement = new Vector2(Axis(k.aKey.isPressed,k.dKey.isPressed), Axis(k.sKey.isPressed,k.wKey.isPressed)); // WASD moves
            looking = new Vector2(Axis(k.leftArrowKey.isPressed,k.rightArrowKey.isPressed), Axis(k.downArrowKey.isPressed,k.upArrowKey.isPressed)); // arrows face
            attack = keyboard.kKey.isPressed;
            switchWeapon.isPressed = keyboard.qKey.isPressed;
            attackLeft.isPressed = keyboard.oKey.isPressed;
            attackRight.isPressed = keyboard.pKey.isPressed;
            throwHold.isPressed = k.qKey.isPressed;
        }
        if(mouse==null||disableMouse){mouseActive = false;return;}
        Vector2 mousePos = mouse.position.ReadValue();
        CheckMouseEnable(mousePos);
        if(mouseActive){
            looking = (Vector2)(Camera.main.ScreenToWorldPoint(mousePos) - GameManager.Instance.player.transform.position);
            attackLeft.isPressed = mouse.leftButton.isPressed;
            attackRight.isPressed = mouse.rightButton.isPressed;
        }
    }
    static float Axis(bool negative,bool positive)=>(positive?1:0)-(negative?1:0);
    void CheckMouseEnable(Vector2 mousePos){
        if((mousePos-mouseLastPos).magnitude > mouseDistanceActivation)mouseActive=true;
        if(mouseActive){
            if((mouseLastPos-mousePos).magnitude>0){mouseLastTime=Time.time;mouseLastPos=mousePos;}
            if(Time.time - mouseLastTime > mouseTimeDeactivation)mouseActive = false;
        }else if(Time.time - mouseLastTime > mouseTimeDeactivation){mouseLastTime = Time.time;mouseLastPos = mousePos;}
    }
}
