using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootType{weapon,chatSkill, instantItem,None}

public class Loot : MonoBehaviour
{
    public LootType lootType = LootType.None;
    public ChatType skill = ChatType.invalid;
    public Weapon weaponLoot;
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
    static readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    static readonly ContactFilter2D solid = new ContactFilter2D(); // default: triggers are ignored

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
        hb.damage = weapon.throwDamage * (weapon.isMagical ? by.combatStat.attackMagic : by.combatStat.attackPhysic);
        if(weapon.buff!=null)weapon.buff.CopyTo(loot.gameObject); // burn, stun...: like a swing
        loot.hitBox = hb; loot.thrower = by; loot.thrownFrom = hand;
        loot.radius = Mathf.Max(weapon.throwSize,0.05f);
        loot.velocity = dir.normalized*weapon.throwInitSpeed;
        loot.deceleration = weapon.throwDeceleration; loot.bounciness = weapon.bounciness; loot.bounce = weapon.isBounce;
        loot.transform.rotation = Quaternion.Euler(0,0,loot.PointAngle(loot.velocity));
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
        float dt = Time.fixedDeltaTime;
        if(returning){FlyBack(dt); return;}
        if(velocity==Vector2.zero)return;
        Vector2 pos = rb.position, move = velocity*dt;
        bool blocked = false;
        int n = Physics2D.CircleCast(pos, radius, move.normalized, solid, hits, move.magnitude);
        for(int i=0;i<n && !blocked;i++){
            var col = hits[i].collider;
            var ch = col.GetComponentInParent<Character>();
            if(ch!=null && !(ch is Chest)){ // characters don't block; a throw hurts them on the way
                if(isThrown){hitBox.Touch(col); if(!isThrown && !returning){pos = hits[i].centroid; blocked = true;}} // its last hit: it drops right there
                continue;
            }
            if(isThrown)hitBox.Touch(col); // a lock, a crate, a chest...
            if(consumed)return;
            pos = hits[i].centroid;
            blocked = true;
            if(bounce)velocity = Vector2.Reflect(velocity, hits[i].normal)*bounciness;
            else if(isThrown){StopThrow(); if(!returning)velocity = Vector2.zero;} // sticks where it hits
            else velocity = Vector2.zero;
        }
        if(consumed)return;
        if(!blocked)pos += move;
        rb.MovePosition(pos);
        velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration*dt);
        if(velocity==Vector2.zero && isThrown)StopThrow(); // out of speed
        Spin(dt);
    }
    void FlyBack(float dt){ // loyal: back to the thrower through walls, speeding up to its throw speed
        if(thrower==null || !thrower.isActiveAndEnabled){Land(); return;} // nobody to come back to: loot here
        Vector2 to = thrower.center - rb.position;
        if(to.magnitude < radius+0.4f){Catch(); return;}
        var w = weaponLoot;
        float speed = w.throwDeceleration>0 ? Mathf.Min(velocity.magnitude + w.throwDeceleration*dt, w.throwInitSpeed) : w.throwInitSpeed;
        velocity = to.normalized*speed;
        rb.MovePosition(rb.position + velocity*dt);
        Spin(dt);
    }
    void Catch(){ // back into the hand it came from, or the other fist hand; both hands full: it drops at the thrower's feet
        var hand = IsFree(thrownFrom) ? thrownFrom : IsFree(thrower.lWeapon) ? thrower.lWeapon : IsFree(thrower.rWeapon) ? thrower.rWeapon : null;
        if(hand==null){Land(); rb.position = thrower.transform.position; return;}
        hand.weapon = weaponLoot;
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_pickup_wep);
        GameManager.Instance.ui.UpdateWeap();
        Destroy(gameObject);
    }
    static bool IsFree(HoldingWeapons h) => h!=null && (h.weapon==null || h.weapon.type==WeaponType.CQC); // empty or fist
    void Spin(float dt){
        if(!isThrown)return;
        var w = weaponLoot;
        if(w.spinSpeed==0){rb.MoveRotation(PointAngle(velocity)); return;} // point first
        rb.MoveRotation(rb.rotation + w.spinSpeed*dt*Mathf.Clamp01(velocity.magnitude/Mathf.Max(w.throwInitSpeed,0.01f))); // slows with it
    }
    float PointAngle(Vector2 v) => v.sqrMagnitude<1e-6f ? transform.eulerAngles.z : Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg - weaponLoot.spriteAngle;

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

    // drop a Loot prefab and/or a weapon (its pickup is made in code); null = nothing. yoink: it slides away
    public static Loot Drop(Loot prefab, Weapon weapon, Transform from, bool yoink=true){
        Loot dropped = null;
        if(prefab!=null){
            dropped = Instantiate(prefab, from.position, Quaternion.identity);
            dropped.transform.parent = from.parent;
            if(yoink)dropped.Yoink();
        }
        if(weapon==null)return dropped;
        dropped = MakeWeaponLoot(weapon, from);
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
