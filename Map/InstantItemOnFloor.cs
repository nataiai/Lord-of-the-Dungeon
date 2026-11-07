using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantItemOnFloor : MonoBehaviour
{
    [SerializeField] InstantItem itemSO;
    [SerializeField] Animator anim;
    AnimationClip anim_item;

    void Start()
    {
        anim_item = itemSO.anim_item;
        anim.Play(anim_item.name);
    }

}
