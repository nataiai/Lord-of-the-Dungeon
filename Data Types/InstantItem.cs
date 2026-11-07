using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType{Heal,Buff,Cleanse}

[CreateAssetMenu(fileName = "InstantItemSO", menuName = "InstantItem")]
public class InstantItem : ScriptableObject
{
    public AnimationClip anim_item;
    public ItemType usableItemType;
    public int buffStat_id;
    public int healAmount;
    public int cleanseDebuff_id;
    public float speedBuff;
    public bool invincibility;
    public float duration;
}
