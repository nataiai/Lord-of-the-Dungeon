using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Enemy
{
    [SerializeField] Sprite openSprite, closedSprite;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField] AudioClip sd_open_chest;

    protected override void Died(){ // opens; a chest is never destroyed
        Loot.Drop(loot, weaponLoot, transform);
        loot = null; weaponLoot = null;
        myRender.sprite = openSprite;
    }
    public override void recieveDamage(int dmg,bool isMagical){if(TryGetComponent(out Lock l) && l.isLocked)return; HP=0;} // locked: only a key opens it (Lock.cs)
    protected override void HurtColor(){}
    protected override bool facesPlayer => false;
    public override void OnTriggerStay2D(Collider2D other){}
}
