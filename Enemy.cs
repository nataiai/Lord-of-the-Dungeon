using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    public EnemySO enemyData;
    public float initSpeed=>enemyData.initSpeed;
    public override Team team=>Team.Enemy;
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
    [Tooltip("Tick if the sprite art faces left. It is flipped to face the player")]
    [SerializeField] bool spriteFacesLeft;
    protected virtual bool facesPlayer => true; // false: never flipped (Chest)
    Vector2 way;

    protected virtual void Start(){
        HP = enemyData.maxHP;
        born = Time.time;
        sr = GetComponentInChildren<SpriteRenderer>();
        foreach(Transform child in sr.transform){
            var s = child.GetComponent<SpriteRenderer>();
            if(s!=null)sr=s;
        }
        originalSRcolor = sr.color;
        GetComponent<Rigidbody2D>().freezeRotation = true;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        col = GetComponent<Collider2D>();
        col.isTrigger=true;
        justSpawn = true;
    }
    void Update(){
        if(justSpawn&&!col.IsTouchingLayers()){col.isTrigger=enemyData.ignoreCollide;justSpawn=false;notice=false;}
        if(hurtTime<Time.time)sr.color = originalSRcolor;
        if(Time.time>lifeSpan+born)Died();
        if(isDead)Died();
        if(StuntMech(enemyData.knocbackRecovery))return;
        Vector2 target = GameManager.Instance.player.center;
        movement = target - center;
        if(facesPlayer && canFace && Mathf.Abs(movement.x)>0.01f)sr.flipX = (movement.x<0) != spriteFacesLeft;
        if(movement.magnitude<enemyData.noticeRange)notice=true;
        if(notice){
            way = enemyData.ignoreCollide ? movement.normalized : PathFinding.Direction(center,target,GameManager.Instance.currentRoom.bounds,col); // ghosts go straight
            rb.velocity = Vector2.ClampMagnitude(way + PathFinding.Separation(col, LayerMask.GetMask("Enemy")), 1) * enemyData.initSpeed;
        }
        setAnimFacing(movement.normalized);
    }

    void OnDrawGizmosSelected(){
        PathFinding.DrawGizmos(col);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center+way);
    }

    protected virtual void Died(){
        GameManager.Instance._Connect();
        sr.color = Color.white;
        if(deathBreathTime==0){
            Loot.Drop(loot, weaponLoot, transform);
            if(isDead){
                GameManager.Instance.summondb.Unlock(enemyData);
                AddSummonTemplate(); // fills summonableEnemy from kills; comment out to fill it by hand
            }
            deathBreathTime = Time.time + .5f;
        }
        if(Time.time >= deathBreathTime)Destroy(gameObject);
    }
    void AddSummonTemplate(){
        var db = GameManager.Instance.summondb;
        if(db.getEnemy(enemyData.enemyName)!=null)return;
        sr.color = originalSRcolor;
        var t = Instantiate(this); t.gameObject.SetActive(false); t.name = enemyData.enemyName+" (summon template)";
        t.loot = null; t.weaponLoot = null;
        db.summonableEnemy.Add(t);
    }
    public override void recieveDamage(int dmg,bool isMagical){
        base.recieveDamage(dmg,isMagical);
        GameManager.Instance.ui.ShowText(enemyData.enemyName.ToLowerInvariant());
        if(isDead)return;
        HurtColor();
        dmg -= isMagical?enemyData.combatStat.defenseMagic:enemyData.combatStat.defensePhysic;
        HP -= Mathf.Max(dmg,1);
    }
    protected virtual void HurtColor(){hurtTime = Time.time+0.1f; sr.color = Color.red;}

    public override void receiveKnockback(Vector2 pos,float force)=>receiveKnockback(pos,force,enemyData.knockbackResistance);

    public virtual void OnTriggerStay2D(Collider2D other){
        if(cooldownTimer>Time.time || deathBreathTime!=0 || !other.TryGetComponent(out Character victim) || !IsEnemyOf(victim))return;
        victim.receiveKnockback(transform.position,enemyData.attackKnockback);
        victim.recieveDamage(enemyData.combatStat.attackPhysic);
        cooldownTimer = Time.time+enemyData.cooldown;
    }
}
