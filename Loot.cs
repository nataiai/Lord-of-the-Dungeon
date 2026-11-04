using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{
    weapon,
    chatSkill,
    None
}

// [RequireComponent(typeof(Collider2D))]
public class Loot : MonoBehaviour
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
    SpriteRenderer sr;
    // Start is called before the first frame update
    void Start()
    {
        Yoink();
        if(sr==null)sr=GetComponentInChildren<SpriteRenderer>();
        if(sr==null)sr=GetComponent<SpriteRenderer>();
        if(sr==null)sr=gameObject.AddComponent<SpriteRenderer>();
        // sr.sortingOrder=3;
        if(GetComponent<Collider2D>()==null)gameObject.AddComponent<BoxCollider2D>();
        UpdateImage();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Yoink(){
        // to do: yoink away from origin pos
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
        HoldingWeapons l = player.lWeapon;
        HoldingWeapons r = player.rWeapon;
        if(l.weapon==null||l.weapon.type==WeaponType.CQC){
            l.weapon = weaponLoot;
            Destroy(gameObject);
            GameManager.Instance._Connect();//connect when first pick weapon
            return;
        }
        if(r.weapon==null||r.weapon.type==WeaponType.CQC){
            r.weapon = weaponLoot;
            Destroy(gameObject);
            return;
        }
        // if(GameManager.Instance.controller.attackLeft.isPressedOnce()){
        if(GameManager.Instance.controller.attackLeft.isPressed){
            (l.weapon,weaponLoot) = (weaponLoot,l.weapon);
            UpdateImage();
            Yoink();
            return;
        }
        if(GameManager.Instance.controller.attackRight.isPressed){
            (r.weapon,weaponLoot) = (weaponLoot,r.weapon);
            UpdateImage();
            Yoink();
            return;
        }
    }
    void UpdateImage(){
        if(lootType == LootType.weapon)
            sr.sprite = weaponLoot.uiWep;
    }
    // Drop the Loot prefab and/or a weapon. The weapon pickup is made by code, no prefab needed. null = nothing
    public static void Drop(Loot prefab, Weapon weapon, Transform from){
        if(prefab!=null)Instantiate(prefab, from.position, Quaternion.identity).transform.parent = from.parent;
        if(weapon==null)return;
        var go = new GameObject(weapon.name+" loot");
        go.transform.position = from.position;
        go.transform.parent = from.parent;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var fromSr = from.GetComponentInChildren<SpriteRenderer>();
        if(fromSr!=null){sr.sortingLayerID = fromSr.sortingLayerID; sr.sortingOrder = fromSr.sortingOrder + 1;}
        go.AddComponent<BoxCollider2D>().isTrigger = true; // after the sprite so it fits
        Loot loot = go.AddComponent<Loot>();
        loot.lootType = LootType.weapon;
        loot.weaponLoot = weapon;
    }
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }
}
