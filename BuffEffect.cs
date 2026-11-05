using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BuffType{burn,heal,freeze,stun,none}

// if on ... do ...
// weapons -> copy to hitbox
// hitbox -> copy to hit char
// character -> apply change by type
// for now: a prefab with this goes in Weapon.buff (or put it on a hitbox prefab); the hitbox copies it onto each enemy it hits
[System.Serializable]
// [CreateAssetMenu(fileName = "BuffSO", menuName = "Buff")]
public class BuffEffect : MonoBehaviour
{
    public float duration=1f;
    public BuffType type = BuffType.none;
    public int power = 1; // burn, heal: HP per second
    Character cha;
    float end, tick; // end is set only when copied onto a character, so other copies (summon templates) expire at once
    // Start is called before the first frame update
    void Start()
    {
        cha = gameObject.GetComponent<Character>();
        tick = Time.time+1;
    }

    // weapon -> hitbox, hitbox -> hit char: the same buff, restarted if it is already there
    public void CopyTo(GameObject go){
        foreach(var b in go.GetComponents<BuffEffect>())if(b.type==type){b.end=Time.time+duration;return;}
        var c = go.AddComponent<BuffEffect>();
        c.type=type; c.duration=duration; c.power=power; c.end=Time.time+duration;
    }

    // on a hitbox: copy to the hit enemy (the player too, later)
    void OnTriggerEnter2D(Collider2D other)
    {
        if(TryGetComponent(out HitBox _) && other.TryGetComponent(out Enemy e))CopyTo(e.gameObject);
    }

    // on a character: apply until end. LateUpdate runs after the character moved itself, so freeze wins
    void LateUpdate()
    {
        if(cha==null)return;
        if(cha.isDead || Time.time>end)Destroy(this);
        else Apply(cha);
    }

    public virtual void Apply(Character cha){
        if(type==BuffType.freeze||type==BuffType.stun)cha.rb.velocity = Vector2.zero; // can't move
        if(tick>Time.time)return;
        tick = Time.time+1;
        if(type==BuffType.burn)cha.HP-=power;
        if(type==BuffType.heal)cha.HP+=power;
    }
}
