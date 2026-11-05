using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BuffType{burn,heal,freeze,stun,none}

// if on ... do ...
// weapons -> copy to hitbox
// hitbox -> copy to hit char
// character -> apply change by type
// use: a small prefab with this (plus any sprite/particles) goes in Weapon.buff,
// HitBox puts a copy on each character it hits. It also works put on a character directly.
[System.Serializable]
// [CreateAssetMenu(fileName = "BuffSO", menuName = "Buff")]
public class BuffEffect : MonoBehaviour
{
    public float duration=1f;
    public BuffType type = BuffType.none;
    public int power = 1; // burn, heal: HP per second
    Character cha;
    float end, tick;
    // Start is called before the first frame update
    void Start()
    {
        cha = gameObject.GetComponentInParent<Character>();
        end = Time.time+duration;
        tick = Time.time+1;
    }

    // LateUpdate, not Update: runs after the character moved itself, so freeze and stun win
    void LateUpdate()
    {
        if(cha!=null && !cha.isDead && Time.time<end){Apply(cha);return;}
        if(cha!=null && cha.gameObject==gameObject)Destroy(this); // on the character itself: remove only the buff
        else Destroy(gameObject); // a copy from HitBox: remove it with its sprite/particles
    }

    public virtual void Apply(Character cha){
        if(type==BuffType.freeze||type==BuffType.stun)cha.rb.velocity = Vector2.zero; // can't move
        if(type==BuffType.stun && cha is Player p)p.lWeapon.lastUse = p.rWeapon.lastUse = Time.time; // can't attack
        if(tick>Time.time)return;
        tick = Time.time+1;
        if(type==BuffType.burn)cha.HP-=power;
        if(type==BuffType.heal){if(cha is Player p2)p2.Heal(power); else cha.HP+=power;}
    }
}
