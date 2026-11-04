// [System.Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UsableItemType
{
    Heal,
    Mute,
    Enhance,
    Explode
}


[CreateAssetMenu(
    fileName = "UsableItemSO",
    menuName = "UsableItem"
)]
public class UsableItem : ScriptableObject
{
    public UsableItemType usableItemType;
    public bool isMagical;
    public int damage;
    public float cooldown;

}