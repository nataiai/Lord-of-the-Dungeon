using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    public AnimationClip[] animations;
    public Animator anim;

    void Start(){anim=GetComponent<Animator>();}

    void Update(){
        if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=.9f)Destroy(gameObject);
    }
    
}
