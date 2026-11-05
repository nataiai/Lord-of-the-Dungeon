using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    public EnemySO enemyData;
    public float initSpeed=>enemyData.initSpeed;
    public override CombatStat combatStat{get=>enemyData.combatStat;set{}}
    public override int maxHP{get=>enemyData.maxHP;set{}}
    [SerializeField] Animator anim;
    [SerializeField] AnimationClip[] enemyIdle, enemyWalk, enemyAttack;
    public Loot loot;
    [Tooltip("Optional. Weapon to drop, no Loot prefab needed")]
    public Weapon weaponLoot;
    SpriteRenderer sr;
    Collider2D col;
    Color originalSRcolor;
    float deathBreathTime;
    float hurtTime;
    float lifeSpan = 360; // seconds, then it dies by itself
    float born;
    float cooldownTimer;
    bool justSpawn;
    bool notice;

    protected virtual void Start(){
        HP = enemyData.maxHP;
        born = Time.time;
        sr = GetComponentInChildren<SpriteRenderer>();
        originalSRcolor = sr.color;
        GetComponent<Rigidbody2D>().freezeRotation = true;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        col = GetComponent<Collider2D>();
        col.isTrigger=true; // a trigger until it touches nothing, so it never spawns stuck
        justSpawn = true;
    }
    void Update(){
        if(justSpawn&&!col.IsTouchingLayers()){col.isTrigger=enemyData.ignoreCollide;justSpawn=false;notice=false;}
        if(hurtTime<Time.time)sr.color = originalSRcolor;
        if(Time.time>lifeSpan+born)Died();
        if(isDead)Died();
        if(StuntMech(enemyData.knocbackRecovery))return;
        movement = GameManager.Instance.player.transform.position - transform.position;
        if(movement.magnitude<enemyData.noticeRange)notice=true;
        if(notice)rb.velocity = movement.normalized * enemyData.initSpeed;
        setAnimFacing(movement.normalized);
    }

    protected virtual void Died(){
        GameManager.Instance._Connect(); // the first kill also joins chat
        sr.color = Color.white;
        if(deathBreathTime==0){
            Loot.Drop(loot, weaponLoot, transform);
            if(isDead){ // killed, not timed out: chat may summon this type now
                GameManager.Instance.summondb.Unlock(enemyData);
                AddSummonTemplate(); // fills summonableEnemy from kills; comment out to fill it by hand
            }
            deathBreathTime = Time.time + .5f;
        }
        if(Time.time >= deathBreathTime)Destroy(gameObject);
    }
    void AddSummonTemplate(){ // a hidden copy of this enemy becomes what chat summons, if its type has none yet
        var db = GameManager.Instance.summondb;
        if(db.getEnemy(enemyData.enemyName)!=null)return;
        sr.color = originalSRcolor; // not the death white or hurt red
        var t = Instantiate(this); t.gameObject.SetActive(false); t.name = enemyData.enemyName+" (summon template)";
        t.loot = null; t.weaponLoot = null; // summons drop nothing, so unique loot is not copied
        db.summonableEnemy.Add(t);
    }
    public override void recieveDamage(int dmg,bool isMagical){
        base.recieveDamage(dmg,isMagical);
        GameManager.Instance.ui.ShowText("\""+enemyData.enemyName.ToLowerInvariant()+"\""); // teaches chat the summon name
        if(isDead)return;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_enemy_hit);
        HurtColor();
        dmg -= isMagical?enemyData.combatStat.defenseMagic:enemyData.combatStat.defensePhysic;
        HP -= Mathf.Max(dmg,1);
    }
    protected virtual void HurtColor(){hurtTime = Time.time+0.1f; sr.color = Color.red;}

    public void receiveKnockback(Vector2 pos,float force)=>receiveKnockback(pos,force,enemyData.knockbackResistance);

    public virtual void OnTriggerStay2D(Collider2D other){
        if(cooldownTimer>Time.time || deathBreathTime!=0 || !other.TryGetComponent(out Player player))return; // no hits while dying
        player.receiveKnockback(transform.position,enemyData.attackKnockback);
        player.recieveDamage(enemyData.combatStat.attackPhysic);
        cooldownTimer = Time.time+enemyData.cooldown;
    }
}
