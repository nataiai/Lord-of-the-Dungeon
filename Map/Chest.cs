using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Enemy
{
    [SerializeField] Sprite openSprite, closedSprite;
    [SerializeField] SpriteRenderer myRender;

    protected override void Died(){ // opens; a chest is never destroyed
        Loot.Drop(loot, weaponLoot, transform);
        loot = null; weaponLoot = null;
        myRender.sprite = openSprite;
    }
    public override void recieveDamage(int dmg,bool isMagical){HP=0;}
    protected override void HurtColor(){}
    public override void OnTriggerStay2D(Collider2D other){}
}
