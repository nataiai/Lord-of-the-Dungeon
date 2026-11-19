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
    float lifeSpan = 86400; // seconds, then it dies by itself
    float born;
    float cooldownTimer;
    bool justSpawn;
    bool notice;
    [Tooltip("Tick if the sprite art faces left. It is flipped to face the player")]
    [SerializeField] bool spriteFacesLeft;
    protected virtual bool facesPlayer => true; // false: never flipped (Chest)
    Vector2 way;
    Vector2 home; // where it started (Experimental: Enemy reset)
    float leftAt = -1;

    void OnDisable(){leftAt = Time.time;} // the player left its room
    void OnEnable(){ // the player is back (Experimental: Enemy reset)
        if(leftAt<0 || isDead)return;
        var x = Experimental.I;
        float away = Time.time-leftAt;
        if(x.resetPosition && x.walkBack)StartCoroutine(WalkHome(initSpeed*away));
        else if(x.resetPosition){transform.position = home; Physics2D.SyncTransforms();} // its collider (center) moves now, not at the next physics step
        if(x.resetHealth)Heal(x.enemyRegen>0 ? (int)(x.enemyRegen*away) : maxHP);
        if(x.forgetPlayer)notice = false;
        if(x.clearBuffs)foreach(var b in GetComponents<BuffEffect>())Destroy(b);
        if(x.pauseLifeSpan)born += away;
    }
    IEnumerator WalkHome(float distance){ // where it got to, walking home by the path, while you were away
        yield return null; // PathFinding knows this room's walls from the next frame
        Vector2 off = (Vector2)transform.position - center, at = center, goal = home - off;
        var bounds = GameManager.Instance.currentRoom.bounds;
        for(int i=0;i<400 && distance>0 && (goal-at).magnitude>.05f;i++){
            float step = Mathf.Min(.25f, Mathf.Min(distance, (goal-at).magnitude));
            at += (enemyData.ignoreCollide ? (goal-at).normalized : PathFinding.Direction(at, goal, bounds, bodyCollider))*step; // ghosts go straight
            distance -= step;
        }
        transform.position = at + off;
        Physics2D.SyncTransforms(); // its collider (center) moves now, not at the next physics step
        if(Experimental.I.forgetPlayer)notice = false; // it saw you at the door the frame before it got here
    }
    protected virtual void Start(){
        HP = enemyData.maxHP;
        born = Time.time;
        home = transform.position;
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
        Vector2 goal = notice ? target : home+center-(Vector2)transform.position; // not seen you: back home (after an earthquake)
        if(notice || (goal-center).magnitude>.1f){
            way = enemyData.ignoreCollide ? (goal-center).normalized : PathFinding.Direction(center,goal,GameManager.Instance.currentRoom.bounds,col); // ghosts go straight
            rb.velocity = Vector2.ClampMagnitude(way + PathFinding.Separation(col, LayerMask.GetMask("Enemy")), 1) * enemyData.initSpeed;
        }
        else rb.velocity = Vector2.zero; // home
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
        victim.recieveDamage(enemyData.isMagical?enemyData.combatStat.attackMagic:enemyData.combatStat.attackPhysic, enemyData.isMagical);
        cooldownTimer = Time.time+enemyData.cooldown;
    }
}
