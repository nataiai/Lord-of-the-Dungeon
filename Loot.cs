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
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }
}
