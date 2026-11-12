using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantItemOnFloor : MonoBehaviour
{
    [SerializeField] InstantItem itemSO;
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider2D col;
    AnimationClip anim_item;

    void Start()
    {
        anim_item = itemSO.anim_item;
        anim.Play(anim_item.name);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player player) && !TryGetComponent(out Loot _))
        {
            player.InstantItemUse(itemSO);
            Destroy(gameObject);
        }
    }
}
