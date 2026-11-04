using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BaseWeapon : MonoBehaviour
{
    public AnimationClip[] animations;
    public Animator anim;

    void Start(){anim=GetComponent<Animator>();}

    PlayableGraph graph;

    public void PlayClip(AnimationClip clip, float speed){ // plays a clip without needing it in the Animator Controller
        if(!anim)anim=GetComponent<Animator>(); // Start hasn't run yet
        AnimationPlayableUtilities.PlayClip(anim, clip, out graph).SetSpeed(speed);
        Destroy(gameObject, clip.length / speed * .9f);
    }
    void OnDestroy(){if(graph.IsValid())graph.Destroy();}

    void Update(){
        if(!graph.IsValid() && anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=.9f)Destroy(gameObject);
    }
    
}
