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
// public string[] MoveDict = {"up", "right", "down", "left", };
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
    
    protected void setAnimFacing(Vector2 looking){
        animFacing = _setAnimFacing(looking);
    }
    protected virtual int _setAnimFacing(Vector2 looking){
        if(looking.x>0.7071f)return 1;
        if(looking.x<-0.7071f)return 3;
        if(looking.y>0)return 0;
        if(looking.y<0)return 2;
        return 2;
    }
    protected void setAnimFacingLegacy(Vector2 movement){
        if(movement.x!=0){
            if(movement.x>0)animFacing=1;
            else animFacing=3;
        }else if(movement.y!=0){
            if(movement.y>0)animFacing=0;
            else animFacing=2;
        }
    }
    

    protected bool StuntMech(float recover){
        if(stunTime>Time.time){
            rb.velocity*=Mathf.Exp(-recover*Time.deltaTime);
            if(rb.velocity.magnitude<0.5f)stunTime=0;
            return true;
        }return false;
    }

    public void receiveKnockback(Vector2 pos,float force,float resistance){
        Vector2 ePos = (Vector2) transform.position;
        float rotationRange = 10;
        Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-rotationRange, rotationRange)) * (ePos-pos) ;
        dir = dir.normalized;
        rb.velocity = force * dir *(1 - resistance);
        stunTime = Time.time + 1;
    }
    

}
