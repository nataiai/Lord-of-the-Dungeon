using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Flags] public enum BuffType{none=0,burn=1,heal=2,slow=4,stun=8,stopMovement=16,stopFacing=32}

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
        [ShowIf("type", BuffType.burn, BuffType.heal, BuffType.slow, BuffType.stun, BuffType.stopMovement, BuffType.stopFacing)]
        public float duration=1f;
        [ShowIf("type", BuffType.burn, BuffType.heal)]
        public int power = 1; // burn, heal: HP per second
        [ShowIf("type", BuffType.burn, BuffType.heal, BuffType.slow, BuffType.stun, BuffType.stopMovement, BuffType.stopFacing)]
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
        if(buff.type.HasFlag(BuffType.slow))cha.rb.velocity *= .9f;
        if(buff.type.HasFlag(BuffType.stun))cha.froze();
        if(buff.type.HasFlag(BuffType.stopMovement))cha.froze();
        if(buff.type.HasFlag(BuffType.stopFacing))cha.stopFacing();
        if(tick>Time.time)return;
        tick = Time.time+1;
        if(buff.type.HasFlag(BuffType.burn))cha.recieveDamage(buff.power);
        if(buff.type.HasFlag(BuffType.heal))cha.Heal(buff.power);
    }
}
