using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public partial class HoldingWeapons{ // one hand
    public Weapon weapon;
    [PlayModeOnly] public float lastUse;
    public bool isOnCooldown=>lastUse+cooldown>Time.time;
    public bool isUsable=>weapon!=null&&!isOnCooldown;
    public float cooldown=>weapon.cooldown;
    BaseWeapon anim;
    public void Unequip(){weapon = PrefabDatabase.Instance.fist; }
    public void Use(Character user){if(isUsable)ForceUse(user);}
    public float getReloadPercent()=>weapon==null?1:Mathf.Min((Time.time-lastUse)/weapon.cooldown,1); // for ui
    public void ForceUse(Character player){
        lastUse = Time.time;
        Vector2 looking4dir = Direction.vec2MoveDict[player.animFacing];
        PlayAnim(player);
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
        SpawnHitBox(player, looking4dir, Experimental.I.fourWayAttack ? looking4dir : player.looking);
    }
    // dir: where it spawns and points, flyDir: where it flies. damage, size: x normal
    public HitBox SpawnHitBox(Character player, Vector2 dir, Vector2 flyDir, float damage=1, float size=1){
        Vector2 playerPos = player.transform.position;
        Vector2 initPos = playerPos + new Vector2(0,0.5f) + dir * weapon.hitBoxDistance + weapon.GetOffset(Direction.facing(dir));
        HitBox hitBox = Object.Instantiate(weapon.hitBox, initPos, weapon.velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, dir));
        hitBox.transform.localScale *= size;
        hitBox.weapon = weapon;
        hitBox.owner = player;
        if(weapon.buff!=null)weapon.buff.CopyTo(hitBox.gameObject); // weapon -> hitbox
        hitBox.damage = weapon.Damage(player, damage);
        hitBox.knockback = weapon.knocback;
        hitBox.playerPos = playerPos;
        hitBox.setVelocity(flyDir * weapon.velocity);
        AIHooks.OnAttack(hitBox, this, player, flyDir);
        return hitBox;
    }
    public void PlayAnim(Character player)=>PlayAnim(player, player.animFacing);
    public void PlayAnim(Character player, int facing, float angle=0){ // angle: turned around the body (spin)
        if(weapon.anim==null)return;
        anim = Object.Instantiate(weapon.anim, player.transform.position, Quaternion.identity,player.transform);
        if(angle!=0){ // a holder turns it: the clip moves and turns the anim itself
            var holder = new GameObject("turned anim").transform;
            holder.SetParent(player.transform, false);
            anim.transform.SetParent(holder, true);
            holder.RotateAround((Vector2)player.transform.position+new Vector2(0,0.5f), Vector3.forward, angle);
            Object.Destroy(holder.gameObject, 2);
        }
        if(facing<anim.animations.Length){
            var clip = anim.animations[facing];
            anim.anim.Play(clip.name);
            anim.anim.speed = Mathf.Max(clip.length / cooldown, 1);
        }
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
    bool enableInvincible => Experimental.I.enableInvincible;
    public float speedBoost;
    [PlayModeOnly] public int state;
    bool attackState;
    const int IDLE = 0;
    const int MOVING = 1;
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
    [PlayModeOnly] public float invincibleTime;
    public bool isInvincible => invincibleTime>Time.time;
    [PlayModeOnly] public bool forceAnim;
    [Tooltip("Fall down animation when it falls in a hole (a state with this clip's name in its Animator). Empty: it shrinks away")]
    public AnimationClip fallClip;
    [PlayModeOnly] public Vector2 roomEntryPos;

    public override int HP{get=>_HP;set{_HP=value;GameManager.Instance.ui.UpdateHP(HP);}}

    void Start(){
        HP = maxHP;
        controller = GameManager.Instance.controller;
        roomEntryPos = transform.position;
        invincibleTime = Time.time;
    }
    void Update(){
        if(HP<=0)Died();
        movement = Vector2.ClampMagnitude(controller.movement,1);
        if(Experimental.I.stopMoveOnAttack && attackState)movement = Vector2.zero;
        if(Experimental.I.lookFollowMovement){if(isMoving)looking = movement;}
        else if(controller.looking.magnitude>0)looking = controller.looking;
        else if(!isLooking)looking = movement;
        if(!StuntMech(8))rb.velocity = movement * initSpeed;
        if(ThrownWeapon.TryThrow(this)){UpdateAnim(); return;}
        TryAttack(lWeapon, controller.attackLeft);
        TryAttack(rWeapon, controller.attackRight);
        UpdateAnim();
    }
    void TryAttack(HoldingWeapons hand, ControllerButton button){
        if(WeaponCharge.Blocks(this, hand, button))return; // charge skills (AI/WeaponCharge.cs)
        if(!hand.isUsable || !button.isPressed)return;
        ForceAttack(hand);
    }
    public void ForceAttack(HoldingWeapons hand){ // even on cooldown
        hand.ForceUse(this);
        ForceAnim(hand);
    }
    public void ForceAnim(HoldingWeapons hand){ // the attack anim only
        forceAnim = true;
        cooldown = hand.cooldown;
        state = (int)hand.weapon.type;
    }
    public void InstantItemUse(InstantItem item){
        if(item==null)return;
        if(item.usableItemType==ItemType.Heal)HealWithEffect(item.healAmount);
    }
    public void Respawn(){
        transform.position = roomEntryPos;
        currentAnim = null;
    }
    void Died(){
        HP = maxHP; //test
        transform.position = roomEntryPos = new Vector2(0,0);
        GameManager.Instance._re();
    }

    void UpdateAnim(){
        if(!forceAnim && attackState && anim.GetCurrentAnimatorStateInfo(0).normalizedTime<.9f)return;
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
        anim.speed = cooldown!=0?Mathf.Max(toAnim.length / cooldown, 1):1;
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
