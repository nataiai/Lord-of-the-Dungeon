using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{
    weapon,
    chatSkill,
    None
}

public class Loot : MonoBehaviour // a pickup on the ground: made by Loot.Drop, or placed in a level by hand
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
    SpriteRenderer sr;

    void Start()
    {
        if(sr==null)sr=GetComponentInChildren<SpriteRenderer>();
        if(sr==null)sr=gameObject.AddComponent<SpriteRenderer>();
        UpdateImage();
        if(GetComponent<Collider2D>()==null)gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // after sprite so it fits
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player player)){
            if(lootType==LootType.weapon)PickWeaponHandle(player);
            if(lootType==LootType.chatSkill)PickChatSkillHandle(player);
            GameManager.Instance.ui.UpdateWeap();
        }
    }
    void PickWeaponHandle(Player player){
        if(weaponLoot==null)return;
        Weapon onGround = weaponLoot.PickUp(player);
        if(onGround==null){Destroy(gameObject);return;}
        weaponLoot = onGround;
        UpdateImage();
    }
    void UpdateImage(){
        if(lootType==LootType.weapon && weaponLoot!=null)sr.sprite = weaponLoot.uiWep;
        if(lootType!=LootType.chatSkill)return;
        Sprite[] icons = GameManager.Instance.chatSkillIcons;
        if((int)skill<icons.Length && icons[(int)skill]!=null)sr.sprite = icons[(int)skill];
    }
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }

    // Put a weapon and/or chat skill pickup on the ground where 'from' is. Built here, no Loot prefab needed
    public static void Drop(Transform from, Weapon weapon, ChatType skill){
        if(weapon!=null)Create(from, LootType.weapon).weaponLoot = weapon;
        if(skill!=ChatType.invalid)Create(from, LootType.chatSkill).skill = skill;
    }
    static Loot Create(Transform from, LootType type){
        var go = new GameObject(type+" loot", typeof(SpriteRenderer));
        go.transform.SetParent(from.parent, false);
        go.transform.position = from.position;
        SpriteRenderer fromSr = from.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if(fromSr!=null){sr.sortingLayerID = fromSr.sortingLayerID; sr.sortingOrder = fromSr.sortingOrder + 1;}
        Loot loot = go.AddComponent<Loot>();
        loot.lootType = type;
        return loot;
    }
}
