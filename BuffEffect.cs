using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BuffType{burn,heal,slow,stun,none,stopMovement,stopFacing}

// if on ... do ...
// weapons -> copy to hitbox
// hitbox -> copy to hit char
// character -> apply change by type
// [CreateAssetMenu(fileName = "BuffSO", menuName = "Buff")]
public class BuffEffect : MonoBehaviour
{
    [System.Serializable]
    public class Buff{
        public BuffType type = BuffType.none; // none = no effect
        public float duration=1f;
        public int power = 1; // burn, heal: HP per second
        public bool isStackable;
        public void CopyTo(GameObject go){
            if(type==BuffType.none)return;
            if(!isStackable)foreach(var b in go.GetComponents<BuffEffect>())if(b.buff.type==type){b.end=Time.time+duration;return;}
            var c = go.AddComponent<BuffEffect>();
            c.buff=this; c.end=Time.time+duration;
        }
    }
    public Buff buff;
    Character cha;
    float end, tick;
    // Start is called before the first frame update
    void Start()
    {
        cha = gameObject.GetComponent<Character>();
        tick = Time.time+1;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(TryGetComponent(out HitBox hb) && other.TryGetComponent(out Character c) && hb.CanHit(c))buff.CopyTo(c.gameObject);
    }

    void LateUpdate()
    {
        if(cha==null)return;
        if(cha.isDead || Time.time>end)Destroy(this);
        else Apply(cha);
    }

    public virtual void Apply(Character cha){
        if(buff.type==BuffType.slow)cha.rb.velocity *= .9f;
        if(buff.type==BuffType.stun)cha.froze();
        if(buff.type==BuffType.stopMovement)cha.froze();
        if(buff.type==BuffType.stopFacing)cha.stopFacing();
        if(tick>Time.time)return;
        tick = Time.time+1;
        if(buff.type==BuffType.burn)cha.recieveDamage(buff.power);
        if(buff.type==BuffType.heal)cha.Heal(buff.power);
    }
}
