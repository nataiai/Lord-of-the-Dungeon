using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Enemy : Character
{
    public EnemySO enemyData;
    public float initSpeed=>enemyData.initSpeed;
    public override CombatStat combatStat{get=>enemyData.combatStat;set{}}
    [SerializeField] Animator anim;
    [SerializeField] AnimationClip[] enemyIdle, enemyWalk, enemyAttack;
    public Loot loot;
    SpriteRenderer sr;
    Collider2D col;
    Color originalSRcolor;
    float deathBreathTime;
    float hurtTime;
    float lifeSpan = 360;
    float born;
    float cooldownTimer;
    bool justSpawn;
    bool notice;
    
    void Awake(){
    }

    protected virtual void Start(){
        HP = enemyData.maxHP;
        born = Time.time;
        if(sr == null) sr = GetComponentsInChildren<SpriteRenderer>()[0]; 
        originalSRcolor = sr.color;
        GetComponent<Rigidbody2D>().freezeRotation = true;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        col = GetComponent<Collider2D>();
        col.isTrigger=true;
        justSpawn = true;
    }
    
    void Update()
    {
        if(justSpawn&&!col.IsTouchingLayers()){col.isTrigger=enemyData.ignoreCollide;justSpawn=false;notice=false;}
        if(hurtTime<Time.time)resetHurtColor();
        if(Time.time>lifeSpan+born)Died();
        if(isDead)Died();
        if(StuntMech(enemyData.knocbackRecovery))return;
        Player player = FindObjectOfType<Player>();
        movement = player.transform.position - transform.position;
        if(movement.magnitude<enemyData.noticeRange)notice=true;
        if(notice)rb.velocity = movement.normalized * enemyData.initSpeed;
        setAnimFacing(movement);
    }

    protected virtual void Died(){
        GameManager.Instance._Connect();//connect when first pick weapon
        sr.color = new Color(1,1,1);
        if(deathBreathTime==0){
            if(loot!=null)Instantiate(loot, transform.position, Quaternion.identity).transform.parent = transform.parent;
            deathBreathTime = Time.time + .5f;
        }
        if(Time.time < deathBreathTime)return;
        // if(enemyData.enemyName.ToLowerInvariant()!="chest" && GameManager.Instance.summondb.getEnemy(enemyData.enemyName) == null){ // if dead and not in spawnable -> make it spawnable
        //     GameManager.Instance.summonableEnemy.Add(this);
        //     gameObject.SetActive(false);
        //     gameObject.transform.position *= 2000; //yeet
        // }else 
        Destroy(gameObject);
    }
    
    public virtual void recieveDamage(int dmg,bool isMagical){
        GameManager.Instance.ui.ShowText("\""+enemyData.enemyName.ToLowerInvariant()+"\"");
        if(isDead)return;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_enemy_hit);
        HurtColor();
        if(isMagical)dmg-=enemyData.combatStat.defenseMagic;
        else dmg-=enemyData.combatStat.defensePhysic;
        if(dmg>0)HP-=dmg;
        else HP-=1; // minimum
    }

    protected virtual void HurtColor(){
        hurtTime = Time.time+0.1f;
        Color red = new Color(1,0,0);
        sr.color = red;
    }
    void resetHurtColor(){
        sr.color = originalSRcolor;
    }

    public void receiveKnockback(Vector2 pos,float force)=>receiveKnockback(pos,force,enemyData.knockbackResistance);

    public virtual void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player player)){
            if (cooldownTimer>Time.time)return;
            player.receiveKnockback(transform.position,enemyData.attackKnockback);
            player.recieveDamage(enemyData.combatStat.attackPhysic);
            cooldownTimer = Time.time+enemyData.cooldown;
        }
    }
}