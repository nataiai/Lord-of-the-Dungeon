using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BuffType{burn,heal,freeze,stun,none}

// if on ... do ...
// weapons -> copy to hitbox
// hitbox -> copy to hit char
// character -> apply change by type
[System.Serializable]
// [CreateAssetMenu(fileName = "BuffSO", menuName = "Buff")]
public class BuffEffect : MonoBehaviour
{
    public float duration=1f;
    public BuffType type = BuffType.none;
    Character cha;
    Weapon weap;
    HitBox hitbox;
    // Start is called before the first frame update
    void Start()
    {
        cha = gameObject.GetComponent<Character>();
        weap = gameObject.GetComponent<Weapon>();
        hitbox = gameObject.GetComponent<HitBox>();
    }

    // Update is called once per frame
    void Update()
    {
        if(cha!=null)Apply(cha);
        if(weap!=null)Apply(weap);
        if(hitbox!=null)Apply(hitbox);
    }

    public virtual void Apply(Character cha){}
    public void Apply(Weapon weap){}
    public void Apply(HitBox hitbox){}
}
