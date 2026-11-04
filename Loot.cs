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
        Weapon onGround = weaponLoot.PickUp(player);
        if(onGround==null){Destroy(gameObject);return;}
        if(onGround==weaponLoot)return;
        weaponLoot = onGround;
        UpdateImage();
        Yoink();
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
