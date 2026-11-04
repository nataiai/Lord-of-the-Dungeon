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

public abstract class Character : MonoBehaviour // remove abstract and fix combat stat if bug
{
    public abstract CombatStat combatStat{get;set;}
    public Vector2 movement;
    Vector2 _looking;
    public Vector2 looking{get => _looking;set => _looking = value.normalized;}
    public int animFacing; //up = 0, right = 1, down = 2, left = 3
    public int _HP;
    public virtual int HP{get=>_HP;set=>_HP=value;}
    public bool isDead => HP<=0;
    public bool isMoving => movement.magnitude > 0;
    public bool isLooking => looking.magnitude > 0;
    public Rigidbody2D rb;
    float stunTime;

    protected void setAnimFacing(Vector2 v)=>animFacing = _setAnimFacing(v);
    protected virtual int _setAnimFacing(Vector2 looking){
        if(looking.x>0.7071f)return 1;
        if(looking.x<-0.7071f)return 3;
        if(looking.y>0)return 0;
        if(looking.y<0)return 2;
        return 2;
    }
    protected bool StuntMech(float recover){ // true while stunned: slows down, ignores own movement
        if(stunTime<=Time.time)return false;
        rb.velocity*=Mathf.Exp(-recover*Time.deltaTime);
        if(rb.velocity.magnitude<0.5f)stunTime=0;
        return true;
    }
    public void receiveKnockback(Vector2 pos,float force,float resistance){
        Vector2 dir = Quaternion.Euler(0,0,Random.Range(-10f,10f)) * ((Vector2)transform.position-pos); // away from pos, +-10 degrees
        rb.velocity = force * dir.normalized * (1-resistance);
        stunTime = Time.time + 1;
    }
}
