using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour // a weapon's effect: plays animations[facing], then removes itself
{
    public AnimationClip[] animations; // up, right, down, left
    public Animator anim;
    void Awake(){if(anim==null)anim=GetComponent<Animator>();} // Awake runs inside Instantiate, before ForceUse plays it
    void Update(){if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=.9f)Destroy(gameObject);}
}
