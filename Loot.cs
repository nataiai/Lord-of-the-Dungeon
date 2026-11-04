using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loot : MonoBehaviour // a weapon on the ground: made by Weapon.Drop, or placed in a level by hand
{
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
        if(weaponLoot==null || !other.TryGetComponent(out Player player))return;
        Weapon onGround = weaponLoot.PickUp(player);
        GameManager.Instance.ui.UpdateWeap();
        if(onGround==null){Destroy(gameObject);return;}
        weaponLoot = onGround;
        UpdateImage();
    }
    void UpdateImage(){if(weaponLoot!=null)sr.sprite = weaponLoot.uiWep;}
}
