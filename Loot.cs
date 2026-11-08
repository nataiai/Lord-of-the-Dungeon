using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{weapon,chatSkill, instantItem,None}

public partial class Loot : MonoBehaviour
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
    public InstantItem instantItem;
    public Animator anim;
    [Tooltip("Fall down animation when it falls in a hole (a state with this clip's name in its Animator). Empty: it shrinks away")]
    public AnimationClip fallClip;

    SpriteRenderer sr;

    void Start(){
        sr = GetComponentInChildren<SpriteRenderer>();
        if(sr==null)sr = gameObject.AddComponent<SpriteRenderer>();
        UpdateImage();
        if(GetComponent<Collider2D>()==null)gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // after the sprite, so it fits
    }
    void Yoink()=>LootYoink.Push(gameObject); // slides away (AI/Yoink.cs)
    void UpdateImage(){if(lootType==LootType.weapon)sr.sprite = weaponLoot.uiWep;}

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player player)){
            if(lootType==LootType.weapon)PickWeaponHandle(player);
            if(lootType==LootType.chatSkill)PickChatSkillHandle(player);
            if(lootType==LootType.instantItem)InstantItemUse(player);
            GameManager.Instance.ui.UpdateWeap();
        }
    }
    void InstantItemUse(Player player)
    {
        player.InstantItemUse(instantItem);
        Destroy(gameObject);
    }
    void PickWeaponHandle(Player player){
        if(TryFill(player.lWeapon)){GameManager.Instance._Connect();return;} // the first weapon joins chat
        if(TryFill(player.rWeapon))return;
        var c = GameManager.Instance.controller; // both hands full: hold that hand's button while stepping on it to swap
        if(c.attackLeft.isPressed)Swap(player.lWeapon);
        else if(c.attackRight.isPressed)Swap(player.rWeapon);
    }
    bool TryFill(HoldingWeapons hand){ // an empty hand, or bare hands (CQC)
        if(hand.weapon!=null && hand.weapon.type!=WeaponType.CQC)return false;
        hand.weapon = weaponLoot; hand.keyId = keyId; hand.lootScale = sr.transform.lossyScale;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_pickup_wep);
        Destroy(gameObject);
        return true;
    }
    void Swap(HoldingWeapons hand){(hand.weapon,weaponLoot) = (weaponLoot,hand.weapon); (hand.keyId,keyId) = (keyId,hand.keyId);UpdateImage();Yoink();}
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }

    // drop a Loot prefab and/or a weapon (its pickup is made in code); null = nothing
    public static void Drop(Loot prefab, Weapon weapon, Transform from){
        if(prefab!=null){var p = Instantiate(prefab, from.position, Quaternion.identity); p.transform.parent = from.parent; AIHooks.OnDrop(p, from);}
        if(weapon==null)return;
        var go = new GameObject(weapon.name+" loot");
        go.transform.position = from.position;
        go.transform.parent = from.parent;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var fromSr = from.GetComponentInChildren<SpriteRenderer>();
        if(fromSr!=null){sr.sortingLayerID = fromSr.sortingLayerID; sr.sortingOrder = fromSr.sortingOrder + 1;}
        go.AddComponent<BoxCollider2D>().isTrigger = true; // after the sprite, so it fits
        var loot = go.AddComponent<Loot>();
        loot.lootType = LootType.weapon;
        loot.weaponLoot = weapon;
        AIHooks.OnDrop(loot, from); // slides away, key id (AI/Hooks.cs)
    }
}
