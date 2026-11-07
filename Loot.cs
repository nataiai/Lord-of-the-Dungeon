using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{weapon,chatSkill, instantItem,None}

public class Loot : MonoBehaviour
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
    [Tooltip("If the weapon is a key: which Locks it opens. Empty = the weapon's keyId")]
    public string keyId;
    public InstantItem instantItem;
    public Animator anim;

    SpriteRenderer sr;

    [Header("Yoink: dropped loot slides away, slows down and bounces off solid things")]
    public float yoinkSpeed = 4;          // speed when dropped
    public float yoinkDeceleration = 8;   // speed lost per second
    [Range(0,1)] public float yoinkBounciness = 0.6f; // speed kept per bounce

    Vector2 velocity;
    float deceleration, bounciness, radius;
    bool bounce = true;
    // a thrown weapon (Loot.Throw): its HitBox is on, it can't be picked up, and it ends as loot where it stops
    HitBox hitBox;
    Player thrower;
    HoldingWeapons thrownFrom;
    bool returning, consumed; // returning: a loyal weapon flying back. consumed: a key used up on a lock
    public bool isThrown => hitBox!=null && hitBox.enabled;
    Rigidbody2D rb;

    void Start(){
        sr = GetComponentInChildren<SpriteRenderer>();
        if(sr==null)sr = gameObject.AddComponent<SpriteRenderer>();
        UpdateImage();
        if(GetComponent<Collider2D>()==null)gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // after the sprite, so it fits
        rb = GetComponent<Rigidbody2D>();
        if(rb==null){rb = gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;} // so the moving trigger is tracked
        if(radius==0)radius = sr.sprite!=null ? Mathf.Max(Mathf.Min(sr.bounds.extents.x,sr.bounds.extents.y),0.05f) : 0.25f;
    }
    void UpdateImage(){if(lootType==LootType.weapon)sr.sprite = weaponLoot.uiWep;}

    public void Yoink()=>Yoink(Random.insideUnitCircle); // a random way
    public void Yoink(Vector2 dir){
        if(dir.sqrMagnitude<1e-6f)dir = Random.insideUnitCircle;
        if(dir.sqrMagnitude<1e-6f)dir = Vector2.right;
        velocity = dir.normalized*yoinkSpeed;
        deceleration = yoinkDeceleration; bounciness = yoinkBounciness; bounce = true;
    }

    // throw a weapon from `by`'s `hand` the way `dir` points (Weapon.throw* settings). It hurts the thrower's enemies,
    // opens a Lock if it's a key, and ends as loot where it stops. A loyal weapon flies back instead
    public static Loot Throw(Weapon weapon, Player by, HoldingWeapons hand, Vector2 dir){
        var loot = MakeWeaponLoot(weapon, by.transform, Mathf.Max(weapon.throwSize,0.05f));
        loot.transform.position = by.center; // from the body, not the feet
        if(GameManager.Instance.currentRoom!=null)loot.transform.parent = GameManager.Instance.currentRoom.transform; // pauses with its room
        var hb = loot.gameObject.AddComponent<HitBox>(); // the throw's hitbox, switched off when it lands
        hb.weapon = weapon; hb.owner = by; hb.thrownLoot = loot; hb.playerPos = by.center;
        loot.keyId = hb.keyId = hand!=null ? hand.keyId : "";
        hb.damage = weapon.throwDamage * (weapon.isMagical ? by.combatStat.attackMagic : by.combatStat.attackPhysic);
        if(weapon.buff!=null)weapon.buff.CopyTo(loot.gameObject); // burn, stun...: like a swing
        loot.hitBox = hb; loot.thrower = by; loot.thrownFrom = hand;
        loot.radius = Mathf.Max(weapon.throwSize,0.05f);
        loot.velocity = dir.normalized*weapon.throwInitSpeed;
        loot.deceleration = weapon.throwDeceleration; loot.bounciness = weapon.bounciness; loot.bounce = weapon.isBounce;
        loot.transform.rotation = Quaternion.Euler(0,0,Slide.PointAngle(loot.velocity, weapon.spriteAngle));
        if(weapon.useSound!=null)SoundPlayer.PlaySound(weapon.useSound);
        return loot;
    }
    public void StopThrow(){ // the throw ends: a loyal weapon turns back, others become loot right here
        if(!isThrown || returning)return;
        if(weaponLoot.isLoyal && thrower!=null){returning = true; hitBox.hitEnemy.Clear(); return;} // can hit again on the way back
        Land();
    }
    void Land(){
        returning = false;
        velocity = Vector2.zero;
        if(hitBox!=null)hitBox.enabled = false;
    }
    public void ConsumeKey(){consumed = true; Destroy(gameObject);}

    void FixedUpdate(){
        if(rb==null || consumed)return;
        if(returning){FlyBack(); return;}
        Slide.Step(rb, ref velocity, radius, deceleration, bounciness, bounce, isThrown ? ThrowTouch : null); // AI/Slide.cs
        if(consumed)return;
        if(velocity==Vector2.zero && isThrown)StopThrow(); // stopped (a wall, out of speed): loot here, or loyal: flies back
        Spin();
    }
    bool ThrowTouch(Collider2D col){ // the throw touches something on its way: hurt it, open it...
        hitBox.Touch(col);
        return !consumed && (isThrown || returning); // false: the throw ended on this hit (its last enemy, a used-up key): stop right here
    }
    void FlyBack(){ // loyal: back to the thrower through walls, speeding up to its throw speed
        if(thrower==null || !thrower.isActiveAndEnabled){Land(); return;} // nobody to come back to: loot here
        if(Slide.Toward(rb, ref velocity, thrower.center, weaponLoot.throwDeceleration, weaponLoot.throwInitSpeed) < radius+0.4f){Catch(); return;} // AI/Slide.cs
        Spin();
    }
    void Catch(){ // back into the hand it came from, or the other fist hand; both hands full: it drops at the thrower's feet
        var hand = IsFree(thrownFrom) ? thrownFrom : IsFree(thrower.lWeapon) ? thrower.lWeapon : IsFree(thrower.rWeapon) ? thrower.rWeapon : null;
        if(hand==null){Land(); rb.position = thrower.transform.position; return;}
        hand.weapon = weaponLoot; hand.keyId = keyId;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_pickup_wep);
        GameManager.Instance.ui.UpdateWeap();
        Destroy(gameObject);
    }
    static bool IsFree(HoldingWeapons h) => h!=null && (h.weapon==null || h.weapon.type==WeaponType.CQC); // empty or fist
    void Spin(){if(isThrown)Slide.Turn(rb, velocity, weaponLoot.spinSpeed, weaponLoot.throwInitSpeed, weaponLoot.spriteAngle);}

    void OnTriggerEnter2D(Collider2D other)
    {
        if(isThrown || consumed)return; // still flying: can't be picked up
        if (other.TryGetComponent(out Player player)){
            if(lootType==LootType.weapon)PickWeaponHandle(player);
            if(lootType==LootType.chatSkill)PickChatSkillHandle(player);
            if(lootType==LootType.instantItem)InstantItemUse(player);
            GameManager.Instance.ui.UpdateWeap();
        }
    }
    void InstantItemUse(Player player)
    {

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
        hand.weapon = weaponLoot; hand.keyId = keyId;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_pickup_wep);
        Destroy(gameObject);
        return true;
    }
    void Swap(Player player, HoldingWeapons hand){(hand.weapon,weaponLoot) = (weaponLoot,hand.weapon); (hand.keyId,keyId) = (keyId,hand.keyId);UpdateImage();Yoink((Vector2)transform.position-player.center);} // the old weapon slides away from the player
    void PickChatSkillHandle(Player player){
        if(skill!=ChatType.invalid && !GameManager.Instance.availableSkill.Contains(skill))GameManager.Instance.availableSkill.Add(skill);
        Destroy(gameObject);
    }

    // drop a Loot prefab and/or a weapon (its pickup is made in code); null = nothing. yoink: it slides away
    // keyId: if the weapon is a key, which Locks it opens
    public static Loot Drop(Loot prefab, Weapon weapon, Transform from, bool yoink=true, string keyId=""){
        Loot dropped = null;
        if(prefab!=null){
            dropped = Instantiate(prefab, from.position, Quaternion.identity);
            dropped.transform.parent = from.parent;
            if(yoink)dropped.Yoink();
        }
        if(weapon==null)return dropped;
        dropped = MakeWeaponLoot(weapon, from);
        dropped.keyId = keyId;
        if(yoink)dropped.Yoink();
        return dropped;
    }
    static Loot MakeWeaponLoot(Weapon weapon, Transform from, float circle=0){ // circle > 0: a round trigger that size (a throw)
        var go = new GameObject(weapon.name+" loot");
        go.transform.position = from.position;
        go.transform.parent = from.parent;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.uiWep;
        var fromSr = from.GetComponentInChildren<SpriteRenderer>();
        if(fromSr!=null){sr.sortingLayerID = fromSr.sortingLayerID; sr.sortingOrder = fromSr.sortingOrder + 1;}
        Collider2D col = circle>0 ? (Collider2D)go.AddComponent<CircleCollider2D>() : go.AddComponent<BoxCollider2D>(); // after the sprite, so the box fits
        if(col is CircleCollider2D cc)cc.radius = circle;
        col.isTrigger = true;
        var loot = go.AddComponent<Loot>();
        loot.lootType = LootType.weapon;
        loot.weaponLoot = weapon;
        return loot;
    }
}
