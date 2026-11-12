using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Enemy, ILock
{
    [Tooltip("Not empty: locked, opens with the key weapon of the same keyId")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey;
    public bool Fits(Weapon key) => Lock.KeyFits(keyId, key);
    public void Unlock() => keyId = "";
    public bool ConsumesKey => consumeKey;
    [SerializeField] Sprite openSprite, closedSprite;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField] AudioClip sd_open_chest;

    bool opened;
    protected override void Died(){
        if(opened || !isDead)return;
        opened = true;
        if(sd_open_chest!=null)SoundPlayer.PlaySound(sd_open_chest);
        Loot.Drop(loot, weaponLoot, transform);
        loot = null; weaponLoot = null;
        myRender.sprite = openSprite;
    }
    public override void recieveDamage(int dmg,bool isMagical){if(Lock.IsLocked(keyId) && Experimental.I.locks)return; HP=0;}
    protected override void HurtColor(){}
    protected override bool facesPlayer => false;
    public override void OnTriggerStay2D(Collider2D other){}
}
