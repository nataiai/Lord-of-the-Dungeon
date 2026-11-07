using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    [SerializeField] Sprite[] sprites; // one per hit
    int index;
    int HP;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField] int hitUntillBreak=1;
    [SerializeField] bool destroyAfterDead;
    [SerializeField] Loot loot;
    [Tooltip("Optional. Weapon to drop, no Loot prefab needed")]
    [SerializeField] Weapon weaponLoot;

    void Start(){HP = hitUntillBreak;}
    void Died(){
        PathFinding.Invalidate(); // it may have been in the way
        if(destroyAfterDead)Destroy(gameObject);
        Loot.Drop(loot, weaponLoot, transform);
    }
    public virtual void recieveDamage(int dmg,bool isMagical){ // magic does nothing; any other hit counts as 1
        if(isMagical)return;
        HP-=1;
        if(++index<sprites.Length)myRender.sprite=sprites[index];
        if(HP==0)Died();
    }
}
