using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Enemy
{
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
    public override void recieveDamage(int dmg,bool isMagical){if(TryGetComponent(out Lock l) && l.isLocked)return; HP=0;} // locked: a key opens it (AI/Lock.cs)
    protected override void HurtColor(){}
    protected override bool facesPlayer => false;
    public override void OnTriggerStay2D(Collider2D other){}
}
