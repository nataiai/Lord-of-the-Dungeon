using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Direction{

    static public string[] strMoveDict = {"up", "right", "down", "left", };
    static public Vector2[] vec2MoveDict = {Vector2.up, Vector2.right, Vector2.down, Vector2.left, };
}

public class Player : Character
{
    public CombatStat _combatStat;
    public override CombatStat combatStat{get=>_combatStat;set=>_combatStat=value;}
    public float _initSpeed=1;
    public float initSpeed=>_initSpeed;
    public int _maxHP = 4;
    public int maxHP{get=>_maxHP;set{_maxHP=value;GameManager.Instance.ui.UpdateMaxHP(maxHP);}}
    public bool enableInvincible;
    public float speedBoost;
    public int state;
    bool attackState;
    const int IDLE = 0;
    const int MOVING = 1;
    // const int WIDLE = 10;
    // const int WMOVING = 11;
    [HideInInspector] public bool isHealing;
    const int SWING = (int)WeaponType.Swinging;
    const int USEROD = (int)WeaponType.Rod;
    const int SPEAR = (int)WeaponType.Spear;
    const int CQC = (int)WeaponType.CQC;
    Vector2 lastMove;
    float cooldown;
    string currentAnim;
    [SerializeField] Animator anim;
    [SerializeField] AnimationClip[] playerIdle, playerWalk, playerIdleWater, playerWalkWater, playerSwinging, playerShooting, playerUseRod, playerSpear;
    // [SerializeField] Weapon activeWeapon,weapon1,weapon2;
    public HoldingWeapons lWeapon,rWeapon;
    [SerializeField] Armor activeArmor;
    [SerializeField] float knockbackResistance;
    Controller controller;

    public float invincibleCooldownTime;
    float invincibleTime;
    public bool isInvincible => invincibleTime>Time.time;
    public bool forceAnim;

    public override int HP{get=>_HP;set{_HP=value;GameManager.Instance.ui.UpdateHP(HP);}}

    public void Heal(int heal=1){HP+=heal;if(HP>maxHP)HP=maxHP;}

    void Awake() {
    }
    void OnDrawGizmos(){ // live preview of hitboxes (Scene view, or Game view with Gizmos on)
        if(lWeapon.weapon!=null)lWeapon.weapon.DrawGizmos(transform.position);
        if(rWeapon.weapon!=null)rWeapon.weapon.DrawGizmos(transform.position);
    }

    void Start()
    {
        HP = maxHP;
        controller = GameManager.Instance.controller;
        invincibleTime = Time.time;
    }

    void Update()
    {
        if(HP<=0)Died();
        movement = controller.movement;
        looking = controller.looking;
        if(movement.magnitude>1)movement/=movement.magnitude;
        if(movement.magnitude>0)lastMove = movement;
        if(looking.magnitude==0)looking=lastMove;
        // transform.position += (Vector3) (movement) * initSpeed * Time.deltaTime; // collision problem
        if(!StuntMech(8))rb.velocity = (movement) * initSpeed;
        // if(controller.attack)Attack(activeWeapon);
        if(lWeapon.isUsable && controller.attackLeft.isPressed){
            forceAnim = true;
            lWeapon.Use(this);
            cooldown = lWeapon.cooldown;
            state = (int)lWeapon.weapon.type;
        }
        if(rWeapon.isUsable && controller.attackRight.isPressed){
            forceAnim = true;
            rWeapon.Use(this);
            cooldown = rWeapon.cooldown;
            state = (int)rWeapon.weapon.type;
        }
        // if(controller.isSwitchWeapon())SwitchWeapon();
        UpdateAnim();
        
    }

    void Died(){
        HP = maxHP; //test
        transform.position = new Vector2(0,0);
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        GameManager.Instance._re();
    }

    void UpdateAnim(){
        if(! forceAnim && attackState && anim.GetCurrentAnimatorStateInfo(0).normalizedTime<.9f)return;
        else attackState = false;
        setAnimFacing(looking);
        switch(state){
            case IDLE:
                cooldown = 0;
                if(isHealing)ChangeAnimation(playerIdleWater, animFacing);
                else ChangeAnimation(playerIdle, animFacing);
            break;
            case MOVING:
                cooldown = 0;
                if(isHealing)ChangeAnimation(playerWalkWater, animFacing);
                else ChangeAnimation(playerWalk, animFacing);
            break;
            case SWING:
                attackState = true;
                ChangeAnimation(playerSwinging, animFacing);
                forceAnim = false;
            break;
            case USEROD:
                attackState = true;
                ChangeAnimation(playerUseRod, animFacing);
                forceAnim = false;
            break;
            case CQC:
            case SPEAR:
                attackState = true;
                ChangeAnimation(playerSpear, animFacing);
                forceAnim = false;
            break;
        }
        if (!isMoving)state = IDLE;
        else state = MOVING;
    }
    
    // void ChooseWeapon(int index=0){ // if index = 1/2 set weapon according to. 
    //     if(index==1)activeWeapon = weapon1;
    //     else if(index==2)activeWeapon = weapon2;
    // }
    // void SwitchWeapon() => activeWeapon = activeWeapon == weapon1 ? weapon2 : weapon1;

    // protected override int _setAnimFacing(Vector2 looking){ // when player look other way
    //     int newAnimFacing = base._setAnimFacing(looking);
    //     if(newAnimFacing!=animFacing){lWeapon.StopAnim();rWeapon.StopAnim();}
    //     return newAnimFacing;
    // }

    void ChangeAnimation(AnimationClip toAnim)
    {
        if(forceAnim || currentAnim != toAnim.name)
        {

            anim.Play(toAnim.name, 0, 0f);

            currentAnim = toAnim.name;

            if(cooldown!=0)anim.speed = toAnim.length / cooldown;
            else anim.speed = 1;
            if(anim.speed<1) anim.speed = 1;
        }
    }

    void ChangeAnimation(AnimationClip[] toAnim, int index){
        if(index<toAnim.Length)ChangeAnimation(toAnim[index]);
    }


    public void recieveDamage(int dmg){
        if(enableInvincible && isInvincible)return;
        HP-=dmg;
        
        invincibleTime = invincibleCooldownTime+Time.time;
    }

    // public void receiveKnockback(Vector3 pos)=>receiveKnockback(pos,0);
    public void receiveKnockback(Vector2 pos,float force){
        if(enableInvincible && isInvincible)return;
        receiveKnockback(pos,force,knockbackResistance);
    }


}
