using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    int index;
    int HP;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField] int hitUntillBreak=1;
    [SerializeField] bool destroyAfterDead;
    [SerializeField] Weapon loot;
    
    void Start(){HP = hitUntillBreak;index=0;}
    void Died(){
        if(destroyAfterDead)Destroy(gameObject);
        if(loot!=null)loot.Drop(transform);
        loot = null;
    }
    public virtual void recieveDamage(int dmg,bool isMagical){
        if(isMagical)return;
        HP-=1;
        if(++index<sprites.Length)myRender.sprite=sprites[index];
        if(HP==0)Died();
    }
}

