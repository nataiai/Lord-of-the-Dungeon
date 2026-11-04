using UnityEngine;

public class BaseWeapon : MonoBehaviour // a weapon's effect: plays animations[facing], then removes itself
{
    public AnimationClip[] animations; // up, right, down, left
    public Animator anim;
    void Start(){anim=GetComponent<Animator>();}
    void Update(){if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=.9f)Destroy(gameObject);}
}
