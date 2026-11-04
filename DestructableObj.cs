using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Destructable : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    int index;
    int HP;
    [SerializeField] SpriteRenderer myRender;
    [SerializeField, FormerlySerializedAs("hitUntillBreak")] int hitsUntilBreak=1;
    [SerializeField] bool destroyAfterDead;
    [SerializeField] Loot loot;
    
    void Start(){HP = hitsUntilBreak;index=0;}
    void Died(){
        if(destroyAfterDead)Destroy(gameObject);
        if(loot!=null)Instantiate(loot, transform.position, Quaternion.identity).transform.parent = transform.parent;
        loot = null;
    }
    public virtual void recieveDamage(int dmg,bool isMagical){
        if(isMagical)return;
        HP-=1;
        if(++index<sprites.Length)myRender.sprite=sprites[index];
        if(HP==0)Died();
    }
}

