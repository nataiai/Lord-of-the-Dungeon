using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType{Heal,Buff,Cleanse}

[CreateAssetMenu(fileName = "InstantItemSO", menuName = "InstantItem")]
public class InstantItem : ScriptableObject
{
    public AnimationClip anim_item;
    public ItemType usableItemType;
    [ShowIf("usableItemType", ItemType.Buff)]
    public int buffStat_id;
    [ShowIf("usableItemType", ItemType.Heal)]
    public int healAmount;
    [ShowIf("usableItemType", ItemType.Cleanse)]
    public int cleanseDebuff_id;
    [ShowIf("usableItemType", ItemType.Buff)]
    public float speedBuff;
    [ShowIf("usableItemType", ItemType.Buff)]
    public bool invincibility;
    [ShowIf("usableItemType", ItemType.Buff)]
    public float duration;
}
