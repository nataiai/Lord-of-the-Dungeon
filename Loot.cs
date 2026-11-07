using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{weapon,chatSkill,None}

public class Loot : MonoBehaviour
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
    SpriteRenderer sr;

    [Header("Yoink: dropped loot slides away, slows down and bounces off solid things")]
    public float yoinkSpeed = 4;   // speed when dropped
    public float deceleration = 8; // speed lost per second
    [Range(0,1)] public float bounciness = 0.6f; // speed kept after a bounce
    Vector2 velocity;
    bool thrown; // flying from a throw: full speed, sticks where it hits, can't be picked up yet
    Character thrower;
    readonly HashSet<Character> thrownHit = new();
    Rigidbody2D rb;
    float radius;
    static readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    static readonly ContactFilter2D solid = new ContactFilter2D(); // default: triggers are ignored

    void Start(){
        sr = GetComponentInChildren<SpriteRenderer>();
        if(sr==null)sr = gameObject.AddComponent<SpriteRenderer>();
        UpdateImage();
        if(GetComponent<Collider2D>()==null)gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // after the sprite, so it fits
        rb = GetComponent<Rigidbody2D>();
        if(rb==null){rb = gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;} // so the moving trigger is tracked properly
        radius = sr.sprite!=null ? Mathf.Max(Mathf.Min(sr.bounds.extents.x,sr.bounds.extents.y),0.05f) : 0.25f;
    }
    public void Yoink()=>Yoink(Random.insideUnitCircle);                 // a random way
    public void Yoink(Vector2 dir){
        if(dir.sqrMagnitude<1e-6f)dir = Random.insideUnitCircle;
        velocity = dir.normalized*yoinkSpeed;
    }
    void Launch(Character by, Vector2 dir, float speed){thrown = true; thrower = by; velocity = dir.normalized*speed;}

    void FixedUpdate(){
        if(velocity==Vector2.zero || rb==null)return;
        float dt = Time.fixedDeltaTime;
        Vector2 pos = rb.position;
        if(HitSolid(pos, velocity*dt, out var hit)){
            pos = hit.centroid;
            if(thrown){velocity = Vector2.zero; thrown = false;} // a thrown weapon stops where it hits and is loot again
            else velocity = Vector2.Reflect(velocity, hit.normal)*bounciness;
        }else pos += velocity*dt;
        rb.MovePosition(pos);
        if(!thrown)velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration*dt); // a throw keeps full speed until it hits
    }
    bool HitSolid(Vector2 pos, Vector2 move, out RaycastHit2D hit){ // the first solid collider in the way: not a trigger, not a character
        int n = Physics2D.CircleCast(pos, radius, move.normalized, solid, hits, move.magnitude);
        for(int i=0;i<n;i++)if(hits[i].collider.GetComponentInParent<Character>()==null){hit = hits[i];return true;}
        hit = default;
        return false;
    }
    void UpdateImage(){if(lootType==LootType.weapon)sr.sprite = weaponLoot.uiWep;}

    void OnTriggerEnter2D(Collider2D other)
    {
        if(thrown){ // still flying: no pickup; hurts the thrower's enemies it flies through (throwDamage 0 = harmless)
            if(weaponLoot!=null && weaponLoot.throwDamage>0 && other.TryGetComponent(out Character c) && thrower!=null && thrower.IsEnemyOf(c) && thrownHit.Add(c)){
                c.recieveDamage(weaponLoot.throwDamage,weaponLoot.isMagical);
                c.receiveKnockback(transform.position,weaponLoot.knocback);
            }
            return;
        }
        if (other.TryGetComponent(out Player player)){
            if(lootType==LootType.weapon)PickWeaponHandle(player);
            if(lootType==LootType.chatSkill)PickChatSkillHandle(player);
            GameManager.Instance.ui.UpdateWeap();
        }
    }
    void PickWeaponHandle(Player player){
        if(TryFill(player.lWeapon)){GameManager.Instance._Connect();return;} // the first weapon joins chat
        if(TryFill(player.rWeapon))return;
        var c = GameManager.Instance.controller; // both hands full: hold that hand's button while stepping on it to swap
        if(c.attackLeft.isPressed)Swap(player,player.lWeapon);
        else if(c.attackRight.isPressed)Swap(player,player.rWeapon);
    }
    bool TryFill(HoldingWeapons hand){ // an empty hand, or bare hands (CQC)
        if(hand.weapon!=null && hand.weapon.type!=WeaponType.CQC)return false;
        hand.weapon = weaponLoot;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_pickup_wep);
        Destroy(gameObject);
        return true;
    }
    void Swap(Player player, HoldingWeapons hand){(hand.weapon,weaponLoot) = (weaponLoot,hand.weapon);UpdateImage();Yoink((Vector2)transform.position-player.center);} // the old weapon slides away from the player
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }

    // throw a weapon: flies from `by` the way it faces at weapon.throwSpeed, stops at the first solid thing, stays there as loot
    public static Loot Throw(Weapon weapon, Character by, Vector2 dir){
        var loot = Drop(null, weapon, by.transform, false);
        loot.transform.position = by.center; // from the body, not the feet
        if(GameManager.Instance.currentRoom!=null)loot.transform.parent = GameManager.Instance.currentRoom.transform; // pauses with its room
        loot.Launch(by, dir, weapon.throwSpeed);
        return loot;
    }

    // drop a Loot prefab and/or a weapon (its pickup is made in code); null = nothing. yoink: slides away from `from`
    public static Loot Drop(Loot prefab, Weapon weapon, Transform from, bool yoink=true){
        Loot dropped = null;
        if(prefab!=null){
            dropped = Instantiate(prefab, from.position, Quaternion.identity);
            dropped.transform.parent = from.parent;
            if(yoink)dropped.Yoink();
        }
        if(weapon==null)return dropped;
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
        if(yoink)loot.Yoink();
        return loot;
    }
}
