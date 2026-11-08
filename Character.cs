using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CombatStat{
    public int attackPhysic = 1;
    public int attackMagic = 1;
    public int defensePhysic = 1;
    public int defenseMagic = 1;
}

[System.Flags] public enum Team{Player=1,Enemy=2,Neutral=4} // hitboxes and touch damage only hurt other teams. Flags: a field can tick several teams

public abstract class Character : MonoBehaviour // remove abstract and fix combat stat if bug
{
    public abstract CombatStat combatStat{get;set;}
    public abstract int maxHP{get;set;}
    [PlayModeOnly] public Vector2 movement;
    Vector2 _looking;
    public Vector2 looking{get => _looking;set => _looking = value.normalized;}
    [PlayModeOnly] public int animFacing; //up = 0, right = 1, down = 2, left = 3
    [PlayModeOnly] public int _HP;
    public virtual int HP{get=>_HP;set=>_HP=value;}
    public bool isDead => HP<=0;
    public bool isMoving => movement.magnitude > 0;
    public bool isLooking => looking.magnitude > 0;
    public Rigidbody2D rb;
    float stunTime;
    Collider2D _col;
    public Collider2D bodyCollider{get{if(!_col)_col = GetComponent<Collider2D>(); return _col;}}
    public Vector2 center => bodyCollider?(Vector2)bodyCollider.bounds.center:(Vector2)transform.position; // middle of the body; the transform is at the feet
    public virtual Team team => Team.Neutral;
    public bool IsEnemyOf(Character c) => c!=null && c.team!=team;

    protected void setAnimFacing(Vector2 v)=>animFacing = _setAnimFacing(v);
    protected virtual int _setAnimFacing(Vector2 looking){
        if(looking.x>0.7071f)return 1;
        if(looking.x<-0.7071f)return 3;
        if(looking.y>0)return 0;
        if(looking.y<0)return 2;
        return 2;
    }
    public void froze(){stunTime=Time.time+1;}
    protected bool StuntMech(float recover){ // true while stunned: slows down, ignores own movement
        if(stunTime<=Time.time)return false;
        rb.velocity*=Mathf.Exp(-recover*Time.deltaTime);
        if(rb.velocity.magnitude<0.5f)stunTime=0;
        return true;
    }
    public virtual void receiveKnockback(Vector2 pos,float force)=>receiveKnockback(pos,force,0);
    public void receiveKnockback(Vector2 pos,float force,float resistance){
        Vector2 dir = Quaternion.Euler(0,0,Random.Range(-10f,10f)) * ((Vector2)transform.position-pos); // away from pos, +-10 degrees
        rb.velocity = force * dir.normalized * (1-resistance);
        stunTime = Time.time + 1;
    }

    public virtual void Heal(int heal=1){HP+=heal;if(HP>maxHP)HP=maxHP;}
    [PlayModeOnly] public bool isHealing; // standing in a healing room
    public void HealWithEffect(int amount){ // heal and show the heal animation
        Heal(amount);
        var healEffect = GameManager.Instance.healEffect;
        if(healEffect!=null)Destroy(Instantiate(healEffect,transform.position,Quaternion.identity,transform),.5f);
    }

    public virtual void recieveDamage(int dmg,bool isMagical=false){
    }

}
