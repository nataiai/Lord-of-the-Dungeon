using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// What a weapon does: attack, hit rules, pick up, gizmo preview. The data is in Weapon.cs
public partial class Weapon
{
    public Vector2 GetOffset(int facing) => new[]{upOffset, rightOffset, downOffset, leftOffset}[facing];
    public Vector2 HitBoxPos(Vector2 playerPos, int facing) =>
        playerPos + new Vector2(0,0.5f) + Direction.vec2MoveDict[facing] * hitBoxDistance + GetOffset(facing);
    // Code-made box corners around HitBoxPos (for the gizmo): length along facing, width across. null with a prefab or no size
    public Vector2[] GetBoxCorners(int facing){
        if(hitBoxPrefab!=null || hitBoxLength<=0 || hitBoxWidth<=0)return null;
        Vector2 f = Direction.vec2MoveDict[facing] * hitBoxLength, w = Vector2.Perpendicular(Direction.vec2MoveDict[facing]) * hitBoxWidth/2;
        return new[]{w, f+w, f-w, -w};
    }

    // Attack: play the effect and spawn the hitbox. Returns the effect (or null)
    public Animator Use(Player player){
        if(useSound!=null)SoundPlayer.PlaySound(useSound);
        SpawnHitBox(player);
        return SpawnEffect(player);
    }

    Animator SpawnEffect(Player player){
        int facing = player.animFacing;
        AnimationClip clip = facing<animations.Length? animations[facing]:null;
        if(clip==null && effectPrefab==null && effectController==null)return null;
        Animator effect;
        if(effectPrefab!=null)effect = Instantiate(effectPrefab, player.transform);
        else{ // code-made: a SpriteRenderer + Animator, the clip animates the sprite
            effect = new GameObject(name+" effect", typeof(SpriteRenderer), typeof(Animator)).GetComponent<Animator>();
            CopySorting(player, effect.GetComponent<SpriteRenderer>(), effectOrder);
            effect.transform.SetParent(player.transform, false);
            effect.runtimeAnimatorController = effectController;
        }
        PlayableGraph graph = default;
        float delay = 0; // 0 = wait for the controller's state to finish
        if(clip!=null){
            float speed = Mathf.Max(1, clip.length / cooldown);
            delay = clip.length / speed * .9f;
            if(effect.runtimeAnimatorController!=null){effect.Play(clip.name, 0, 0); effect.speed = speed;} // state named like the clip
            else AnimationPlayableUtilities.PlayClip(effect, clip, out graph).SetSpeed(speed); // no Animator Controller needed
        }else if(effect.runtimeAnimatorController==null){Destroy(effect.gameObject, cooldown); return effect;}
        player.StartCoroutine(EndEffect(effect, graph, delay));
        return effect;
    }

    static IEnumerator EndEffect(Animator effect, PlayableGraph graph, float delay){
        if(delay>0)yield return new WaitForSeconds(delay);
        else do yield return null; while(effect!=null && effect.GetCurrentAnimatorStateInfo(0).normalizedTime<.9f);
        if(graph.IsValid())graph.Destroy();
        if(effect!=null)Destroy(effect.gameObject);
    }

    HitBox SpawnHitBox(Player player){
        int facing = player.animFacing;
        Vector2 playerPos = player.transform.position;
        Vector2 pos = HitBoxPos(playerPos, facing);
        Quaternion rot = velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, Direction.vec2MoveDict[facing]);
        HitBox hb;
        if(hitBoxPrefab!=null)hb = Instantiate(hitBoxPrefab, pos, rot);
        else{ // code-made: Rigidbody2D + trigger box + HitBox
            hb = new GameObject(name+" hitbox", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(HitBox)).GetComponent<HitBox>();
            hb.transform.SetPositionAndRotation(pos, rot);
            for(int i=0;i<32;i++)if((hitBoxLayer.value & 1<<i)!=0){hb.gameObject.layer = i; break;}
            hb.GetComponent<Rigidbody2D>().gravityScale = 0;
            BoxCollider2D box = hb.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            if(hitBoxLength>0 && hitBoxWidth>0){ // from the spawn point: length along facing, width across
                Vector2 dir = Quaternion.Inverse(rot) * Direction.vec2MoveDict[facing]; // facing in the hitbox's own space
                box.size = new Vector2(Mathf.Abs(dir.x)*hitBoxLength + Mathf.Abs(dir.y)*hitBoxWidth, Mathf.Abs(dir.y)*hitBoxLength + Mathf.Abs(dir.x)*hitBoxWidth);
                box.offset = dir * hitBoxLength/2;
            }
            if(hitBoxSprite!=null){var sr = hb.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = hitBoxSprite; CopySorting(player, sr, 1);}
        }
        hb.weapon = this;
        hb.damage = damage * (isMagical? player.combatStat.attackMagic:player.combatStat.attackPhysic);
        hb.playerPos = playerPos;
        Vector2 v = player.looking.normalized * velocity;
        if(v.magnitude!=0)hb.GetComponent<Rigidbody2D>().velocity = v;
        if(destroyTimer!=0)Destroy(hb.gameObject, destroyTimer);
        return hb;
    }

    static void CopySorting(Player player, SpriteRenderer sr, int order){ // draw on the player's layer
        SpriteRenderer p = player.GetComponentInChildren<SpriteRenderer>();
        if(p!=null){sr.sortingLayerID = p.sortingLayerID; sr.sortingOrder = p.sortingOrder + order;}
    }

    // Called by the hitbox when it touches something
    public void OnHit(HitBox hb, Collider2D other){
        if(other.TryGetComponent(out Player _))return; // never hit the player
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
            Vector2[] s = GetBoxCorners(i);
            Gizmos.DrawWireSphere(pos, 0.05f);
            if(s!=null&&s.Length>=3)for(int j=0;j<s.Length;j++)Gizmos.DrawLine(pos+s[j], pos+s[(j+1)%s.Length]);
        }
    }
}

[System.Serializable]
public class HoldingWeapons{ // one hand of the player: only tracks its cooldown, the Weapon does the rest
    public Weapon weapon;
    public float lastUse;
    public bool isOnCooldown=>lastUse+cooldown>Time.time;
    public bool isUsable=>weapon!=null&&!isOnCooldown;
    public float cooldown=>weapon.cooldown;
    Animator effect;
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
    public void StopAnim(){if(effect!=null)Object.Destroy(effect.gameObject);effect=null;}
}
