using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Direction{
    static public string[] strMoveDict = {"up", "right", "down", "left", };
    static public Vector2[] vec2MoveDict = {Vector2.up, Vector2.right, Vector2.down, Vector2.left, };
}

[System.Serializable]
public class HoldingWeapons{ // one hand
    public Weapon weapon;
    public float lastUse;
    public bool isOnCooldown=>lastUse+cooldown>Time.time;
    public bool isUsable=>weapon!=null&&!isOnCooldown;
    public float cooldown=>weapon.cooldown;
    BaseWeapon anim;
    public void Use(Character user){if(isUsable)ForceUse(user);} // any Character can hold weapons
    public float getReloadPercent()=>weapon==null?1:Mathf.Min((Time.time-lastUse)/weapon.cooldown,1); // for ui
    public void ForceUse(Character player){
        lastUse = Time.time;
        Vector2 playerPos = player.transform.position;
        Vector2 looking4dir = Direction.vec2MoveDict[player.animFacing];
        Vector2 initPos = playerPos + new Vector2(0,0.5f) + looking4dir * weapon.hitBoxDistance + weapon.GetOffset(player.animFacing);
        if(weapon.anim!=null){
            anim = Object.Instantiate(weapon.anim, playerPos, Quaternion.identity,player.transform);
            if(player.animFacing<anim.animations.Length){
                var clip = anim.animations[player.animFacing];
                anim.anim.Play(clip.name);
                anim.anim.speed = Mathf.Max(clip.length / cooldown, 1);
            }
        }
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
        HitBox hitBox = Object.Instantiate(weapon.hitBox, initPos, weapon.velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, looking4dir));
        hitBox.weapon = weapon;
        hitBox.owner = player;
        if(weapon.buff!=null)weapon.buff.CopyTo(hitBox.gameObject); // weapon -> hitbox
        hitBox.damage = weapon.damage * (weapon.isMagical? player.combatStat.attackMagic:player.combatStat.attackPhysic);
        hitBox.playerPos = playerPos;
        hitBox.setVelocity(player.looking * weapon.velocity);
    }
    public void StopAnim(){if(anim!=null)GameObject.Destroy(anim.gameObject);anim=null;}
}

public class Player : Character
{
    public CombatStat _combatStat;
    public override CombatStat combatStat{get=>_combatStat;set=>_combatStat=value;}
    public float _initSpeed=1;
    public float initSpeed=>_initSpeed;
    public override Team team=>Team.Player;
    public int _maxHP = 4;
    public override int maxHP{get=>_maxHP;set{_maxHP=value;GameManager.Instance.ui.UpdateMaxHP(maxHP);}}
    public bool enableInvincible;
    public float speedBoost;
    public int state; // IDLE, MOVING, or the WeaponType number while attacking
    bool attackState;
    const int IDLE = 0;
    const int MOVING = 1;
    [HideInInspector] public bool isHealing;
    const int SWING = (int)WeaponType.Swinging;
    const int USEROD = (int)WeaponType.Rod;
    const int SPEAR = (int)WeaponType.Spear;
    const int CQC = (int)WeaponType.CQC;
    float cooldown;
    string currentAnim;
    [SerializeField] Animator anim;
    [SerializeField] AnimationClip[] playerIdle, playerWalk, playerIdleWater, playerWalkWater, playerSwinging, playerShooting, playerUseRod, playerSpear;
    public HoldingWeapons lWeapon,rWeapon;
    [SerializeField] Armor activeArmor;
    [SerializeField] float knockbackResistance;
    Controller controller;
    public float invincibleCooldownTime;
    public float invincibleTime;
    public bool isInvincible => invincibleTime>Time.time;
    public bool forceAnim;
    Vector2 lastMove;

    public override int HP{get=>_HP;set{_HP=value;GameManager.Instance.ui.UpdateHP(HP);}}

    void Start(){
        HP = maxHP;
        controller = GameManager.Instance.controller;
        invincibleTime = Time.time;
    }
    void Update(){
        if(HP<=0)Died();
        movement = Vector2.ClampMagnitude(controller.movement,1);
        if(controller.looking.magnitude>0)looking = controller.looking; // no look input: keep the last direction it looked
        else if(!isLooking)looking = movement; // never looked yet: face where it walks
        if(!StuntMech(8))rb.velocity = movement * initSpeed;
        if (movement.magnitude > 0) lastMove = movement;
        looking = lastMove;
        TryAttack(lWeapon, controller.attackLeft);
        TryAttack(rWeapon, controller.attackRight);
        UpdateAnim();
    }
    void TryAttack(HoldingWeapons hand, ControllerButton button){
        if(!hand.isUsable || !button.isPressed)return;
        forceAnim = true;
        hand.Use(this);
        cooldown = hand.cooldown;
        state = (int)hand.weapon.type;
    }
    void Died(){
        HP = maxHP; //test
        transform.position = new Vector2(0,0);
        GameManager.Instance._re();
    }

    void UpdateAnim(){
        if(!forceAnim && attackState && anim.GetCurrentAnimatorStateInfo(0).normalizedTime<.9f)return; // let the attack finish
        attackState = false;
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
    void ChangeAnimation(AnimationClip[] toAnim, int index){if(index<toAnim.Length)ChangeAnimation(toAnim[index]);}
    void ChangeAnimation(AnimationClip toAnim){
        if(!forceAnim && currentAnim == toAnim.name)return;
        anim.Play(toAnim.name, 0, 0f);
        currentAnim = toAnim.name;
        anim.speed = cooldown!=0?Mathf.Max(toAnim.length / cooldown, 1):1; // an attack fits its cooldown
    }

    public override void recieveDamage(int dmg, bool isMagical=false){
        base.recieveDamage(dmg,isMagical);
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_player_hit);
        if(enableInvincible && isInvincible)return;
        HP-=dmg;
        invincibleTime = invincibleCooldownTime+Time.time;
    }
    public override void receiveKnockback(Vector2 pos,float force){
        if(enableInvincible && isInvincible)return;
        receiveKnockback(pos,force,knockbackResistance);
    }
}
