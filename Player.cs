using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Direction{

    static public string[] strMoveDict = {"up", "right", "down", "left", };
    static public Vector2[] vec2MoveDict = {Vector2.up, Vector2.right, Vector2.down, Vector2.left, };
}

[System.Serializable]
public class HoldingWeapons{
    public Weapon weapon;
    public float lastUse;
    public bool isOnCooldown=>lastUse+cooldown>Time.time;
    public bool isUsable=>weapon!=null&&!isOnCooldown;
    public float cooldown=>weapon.cooldown;
    BaseWeapon anim;
    public void Use(Player player){
        if(!isUsable)return;
        else ForceUse(player);
    }
    public float getReloadPercent(){ // for ui
        if(weapon==null)return 1;
        float o = (Time.time-lastUse)/weapon.cooldown;
        if(o>1)o=1;
        return o;
    }
    public void ForceUse(Player player){
        lastUse = Time.time;
        Vector2 playerPos = player.transform.position;
        Vector2 looking = player.looking;
        Vector2 looking4dir = Direction.vec2MoveDict[player.animFacing];
        Vector2 initPos = (Vector2) playerPos + new Vector2(0,0.5f) + looking4dir * weapon.range;

        // BaseWeapon anim = Player.Instantiate(weapon.anim, playerPos, Quaternion.FromToRotation(Vector2.right, looking4dir),player.transform);
        if(weapon.anim!=null)anim = Player.Instantiate(weapon.anim, playerPos, Quaternion.identity,player.transform);
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
 
        if ((weapon.anim!=null)&&player.animFacing<anim.animations.Length){
            var tmp = anim.animations[player.animFacing];
            anim.anim.Play(tmp.name);
            anim.anim.speed = tmp.length / cooldown;
            if(anim.anim.speed<1)anim.anim.speed=1;
        }
        // HitBox hitBox = Player.Instantiate(weapon.hitBox, initPos, Direction.vec2MoveDict[player.animFacing]);
        HitBox hitBox = Player.Instantiate(weapon.hitBox, initPos, weapon.velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, looking4dir));
        hitBox.weapon = weapon;
        hitBox.damage = weapon.damage * (weapon.isMagical? player.combatStat.attackMagic:player.combatStat.attackPhysic);
        hitBox.playerPos = playerPos;
        hitBox.setVelocity (looking.normalized * weapon.velocity);
    }
    public void StopAnim(){if(anim!=null)GameObject.Destroy(anim);anim=null;}
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
        if(animFacing<toAnim.Length)ChangeAnimation(toAnim[index]);
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
