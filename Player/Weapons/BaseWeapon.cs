using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    public AnimationClip[] animations; // up, right, down, left
    public Animator anim;
    void Awake(){if(anim==null)anim=GetComponent<Animator>();}
    void Update(){if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=.9f)Destroy(gameObject);}
}
