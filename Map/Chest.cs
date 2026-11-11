using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Enemy, ILock // a key opens it, like a Door (AI/Lock.cs)
{
    [Tooltip("Not empty: locked, opens with the key weapon of the same keyId")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey;
    public bool Fits(Weapon key) => Lock.KeyFits(keyId, key);
    public void Unlock() => keyId = ""; // the key's hit then opens it
    public bool ConsumesKey => consumeKey;
    [SerializeField] Sprite openSprite, closedSprite;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField] AudioClip sd_open_chest;

    bool opened;
    protected override void Died(){ // opens; a chest is never destroyed. Called every frame once HP is 0, so only the first call opens it
        if(opened || !isDead)return; // !isDead: the enemy lifespan timeout, a chest only opens when hit
        opened = true;
        if(sd_open_chest!=null)SoundPlayer.PlaySound(sd_open_chest);
        Loot.Drop(loot, weaponLoot, transform);
        loot = null; weaponLoot = null;
        myRender.sprite = openSprite;
    }
    public override void recieveDamage(int dmg,bool isMagical){if(Lock.IsLocked(keyId) && Experimental.I.locks)return; HP=0;} // locked: only a key opens it
    protected override void HurtColor(){}
    protected override bool facesPlayer => false;
    public override void OnTriggerStay2D(Collider2D other){}
}
