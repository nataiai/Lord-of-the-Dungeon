using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class ControllerButton{
    [SerializeField] bool _isPressed;
    [SerializeField] bool _isSeen;
    public bool isPressed{
        get =>_isPressed;
        set{
            _isPressed = value;
            if(!isPressed)_isSeen = false;
        }
    }
    public bool isPressedOnce(){
        if(isPressed){
            if (_isSeen)return false;
            _isSeen = true;
            return true;
        }else return false;
    }
}

public class Controller : MonoBehaviour
{
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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        looking = Vector2.zero;
        UpdateInput();
        Cursor.visible = enableMouse;
    }
    void UpdateInput(){
        var gamepad = Gamepad.current; //https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/Gamepad.html
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (gamepad!=null){
            movement = gamepad.leftStick.ReadValue();
            looking = gamepad.rightStick.ReadValue();
            attack = gamepad.buttonWest.isPressed;
            switchWeapon.isPressed = gamepad.buttonNorth.isPressed;
            attackLeft.isPressed = gamepad.leftShoulder.isPressed;
            attackRight.isPressed = gamepad.rightShoulder.isPressed;
            return;
        } 
        if (keyboard!=null){
            attack = keyboard.kKey.isPressed;
            switchWeapon.isPressed = keyboard.qKey.isPressed;
        } 
        if (mouse!=null){
            Vector2 mousePos = mouse.position.ReadValue();
            CheckMouseEnable(mousePos);
            if(enableMouse){
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(mousePos.x, mousePos.y, 0f)
                );
                looking = (Vector2)  (worldPos - GameManager.Instance.player.transform.position);
            }
            attackLeft.isPressed = mouse.leftButton.isPressed;
            attackRight.isPressed = mouse.rightButton.isPressed;
        }else enableMouse = false;
    }
    
    bool CheckMouseEnable(Vector2 mousePos){
        if((mousePos-mouseLastPos).magnitude > mouseDistanceActivation)enableMouse=true;
        if(enableMouse){
            if((mouseLastPos-mousePos).magnitude>0)mouseLastTime=Time.time;
            if(Time.time - mouseLastTime > mouseTimeDeactivation)enableMouse = false;
        }else{
            if(Time.time - mouseLastTime > mouseTimeDeactivation){
                mouseLastTime = Time.time;
                mouseLastPos = mousePos;
            }
        }
        return enableMouse;
    }
}

// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/api/UnityEngine.InputSystem.Controls.ButtonControl.html
// ButtonControl -> isPressed wasPressedThisFrame wasReleasedThisFrame