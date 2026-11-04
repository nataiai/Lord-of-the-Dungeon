// [System.Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "WeaponSO",
    menuName = "Weapon"
)]
public class Weapon : ScriptableObject // everything a weapon is and does: stats, attack, hit rules, pick up
{
    public WeaponType type;
    public AudioClip useSound;
    public AudioClip hitSound;
    public bool isMagical;
    public int damage;
    public float cooldown;
    public float knocback;

    [Tooltip("How far in front of the player the hitbox spawns")]
    [FormerlySerializedAs("range")]
    public float hitBoxDistance;
    public HitBox hitBox;
    [Tooltip("Optional sprite for the hitbox (e.g. arrow). Empty = keep the prefab's sprite")]
    public Sprite hitBoxSprite;
    [Header("Hitbox per facing direction (x = right, y = up)")]
    public HitBoxSide up;
    public HitBoxSide right, down, left;
    [Tooltip("Hitbox flying speed. 0 = stays where it spawns")]
    public float velocity;
    [Tooltip("Seconds before the hitbox disappears. 0 = disappears on first touch")]
    public float destroyTimer;
    [Tooltip("Enemies it can hit before disappearing. 0 = unlimited")]
    [FormerlySerializedAs("destroyOnHit")]
    public int maxHits;
    [Tooltip("Weapon effect clip per facing: up, right, down, left. Empty = no effect")]
    public AnimationClip[] animations;
    [Tooltip("Effect draw order relative to the player. 1 = in front, -1 = behind")]
    public int effectOrder = 1;
    public Sprite uiWep;

    public HitBoxSide GetSide(int facing) => new[]{up, right, down, left}[facing];
    public Vector2 HitBoxPos(Vector2 playerPos, int facing) =>
        playerPos + new Vector2(0,0.5f) + Direction.vec2MoveDict[facing] * hitBoxDistance + GetSide(facing).offset;

    // Attack: play the effect animation and spawn the hitbox. Returns the effect object (or null)
    public GameObject Use(Player player){
        int facing = player.animFacing;
        Vector2 playerPos = player.transform.position;
        Vector2 looking4dir = Direction.vec2MoveDict[facing];

        if(useSound!=null)SoundPlayer.PlaySound(useSound);
        GameObject effect = null;
        AnimationClip clip = facing<animations.Length? animations[facing]:null;
        if(clip!=null){ // effect object is built here, no prefab or Animator Controller needed
            SpriteRenderer playerSr = player.GetComponentInChildren<SpriteRenderer>();
            effect = new GameObject(name+" effect", typeof(SpriteRenderer), typeof(Animator));
            effect.transform.SetParent(player.transform, false);
            SpriteRenderer esr = effect.GetComponent<SpriteRenderer>();
            if(playerSr!=null){esr.sortingLayerID = playerSr.sortingLayerID; esr.sortingOrder = playerSr.sortingOrder + effectOrder;}
            float speed = Mathf.Max(1, clip.length / cooldown);
            AnimationPlayableUtilities.PlayClip(effect.GetComponent<Animator>(), clip, out PlayableGraph graph).SetSpeed(speed);
            player.StartCoroutine(EndEffect(effect, graph, clip.length / speed * .9f));
        }

        HitBox hb = Instantiate(hitBox, HitBoxPos(playerPos, facing), velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, looking4dir));
        hb.weapon = this;
        hb.damage = damage * (isMagical? player.combatStat.attackMagic:player.combatStat.attackPhysic);
        hb.playerPos = playerPos;
        if(hitBoxSprite!=null && hb.TryGetComponent(out SpriteRenderer sr))sr.sprite = hitBoxSprite;
        Vector2[] shape = GetSide(facing).shape;
        if(shape!=null && shape.Length>=3){ // replace the prefab's collider with the custom shape
            foreach(var c in hb.GetComponents<Collider2D>())c.enabled=false;
            var poly = hb.gameObject.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = System.Array.ConvertAll(shape, p => (Vector2)(Quaternion.Inverse(hb.transform.rotation) * p));
        }
        Vector2 v = player.looking.normalized * velocity;
        if(v.magnitude!=0)hb.GetComponent<Rigidbody2D>().velocity = v;
        if(destroyTimer!=0)Destroy(hb.gameObject, destroyTimer);
        return effect;
    }

    static IEnumerator EndEffect(GameObject effect, PlayableGraph graph, float delay){
        yield return new WaitForSeconds(delay);
        if(graph.IsValid())graph.Destroy();
        if(effect!=null)Destroy(effect);
    }

    // Called by the hitbox when it touches something
    public void OnHit(HitBox hb, Collider2D other){
        if (other.TryGetComponent(out Enemy enemy)){
            if(!hb.hitEnemy.Add(enemy))return;
            enemy.recieveDamage(hb.damage,isMagical);
            enemy.receiveKnockback(hb.playerPos,knocback);
            if( maxHits>0 && maxHits <= ++hb.hitCount)
                Destroy(hb.gameObject);
        }else if(other.TryGetComponent(out Destructable des)&&!isMagical){
            des.recieveDamage(hb.damage,isMagical);
            Destroy(hb.gameObject);
        }
        if(destroyTimer==0)
            Destroy(hb.gameObject);
    }

    // Put this weapon into an empty (or CQC) hand, or swap with the hand whose attack is pressed.
    // Returns what stays on the ground: null = picked up, this = nothing happened, other = swapped out weapon
    public Weapon PickUp(Player player){
        HoldingWeapons l = player.lWeapon;
        HoldingWeapons r = player.rWeapon;
        if(l.weapon==null||l.weapon.type==WeaponType.CQC){
            l.weapon = this;
            GameManager.Instance._Connect();//connect when first pick weapon
            return null;
        }
        if(r.weapon==null||r.weapon.type==WeaponType.CQC){
            r.weapon = this;
            return null;
        }
        Weapon old;
        // if(GameManager.Instance.controller.attackLeft.isPressedOnce()){
        if(GameManager.Instance.controller.attackLeft.isPressed){(old,l.weapon) = (l.weapon,this); return old;}
        if(GameManager.Instance.controller.attackRight.isPressed){(old,r.weapon) = (r.weapon,this); return old;}
        return this;
    }

    // Live preview of every side's hitbox (Scene view, or Game view with Gizmos on)
    public void DrawGizmos(Vector2 playerPos){
        for(int i=0;i<4;i++){
            Vector2 pos = HitBoxPos(playerPos, i);
            Vector2[] s = GetSide(i).shape;
            Gizmos.DrawWireSphere(pos, 0.05f);
            if(s!=null&&s.Length>=3)for(int j=0;j<s.Length;j++)Gizmos.DrawLine(pos+s[j], pos+s[(j+1)%s.Length]);
        }
    }
}

[System.Serializable]
public class HitBoxSide
{
    [Tooltip("Extra hitbox spawn offset")]
    public Vector2 offset;
    [Tooltip("Optional hitbox shape, 3+ points around the spawn point. Empty = use the prefab's collider")]
    public Vector2[] shape;
}

[System.Serializable]
public class HoldingWeapons{ // one hand of the player: only tracks its cooldown, the Weapon does the rest
    public Weapon weapon;
    public float lastUse;
    public bool isOnCooldown=>lastUse+cooldown>Time.time;
    public bool isUsable=>weapon!=null&&!isOnCooldown;
    public float cooldown=>weapon.cooldown;
    GameObject effect;
    public void Use(Player player){
        if(!isUsable)return;
        else ForceUse(player);
    }
    public float getReloadPercent(){ // for ui
        if(weapon==null)return 1;
        float o = (Time.time-lastUse)/weapon.cooldown;
        if(o>1)o=1;
        return o;
    }
    public void ForceUse(Player player){
        lastUse = Time.time;
        effect = weapon.Use(player);
    }
    public void StopAnim(){if(effect!=null)GameObject.Destroy(effect);effect=null;}
}
