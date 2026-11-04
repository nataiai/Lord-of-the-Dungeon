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
    [Header("Weapon")]
    public WeaponType type;
    public Sprite uiWep;
    public bool isMagical;
    public int damage;
    public float cooldown;
    public float knocback;
    public AudioClip useSound;
    public AudioClip hitSound;

    [Header("Hitbox")]
    [Tooltip("Optional. Empty = made by code from the settings below")]
    [FormerlySerializedAs("hitBox")]
    public HitBox hitBoxPrefab;
    [Tooltip("Box length, from the player along the facing direction. 0 = no box")]
    [HideIfSet("hitBoxPrefab")] public float hitBoxLength;
    [Tooltip("Box width, across the facing direction. 0 = no box")]
    [HideIfSet("hitBoxPrefab")] public float hitBoxWidth;
    [Tooltip("Optional hitbox sprite (e.g. arrow)")]
    [HideIfSet("hitBoxPrefab")] public Sprite hitBoxSprite;
    [Tooltip("Physics layer of the hitbox (pick one)")]
    [HideIfSet("hitBoxPrefab")] public LayerMask hitBoxLayer;
    [Tooltip("Push the hitbox forward from the player")]
    [FormerlySerializedAs("range")]
    public float hitBoxDistance;
    [Tooltip("Hitbox flying speed. 0 = stays where it spawns")]
    public float velocity;
    [Tooltip("Seconds before the hitbox disappears. 0 = disappears on first touch")]
    public float destroyTimer;
    [Tooltip("Enemies it can hit before disappearing. 0 = unlimited")]
    [FormerlySerializedAs("destroyOnHit")]
    public int maxHits;
    [Tooltip("Per facing side (x = right, y = up): extra offset, and an optional custom shape that replaces the box or prefab collider")]
    public HitBoxSide up, right, down, left;

    [Header("Effect")]
    [Tooltip("Optional. Empty = made by code. Spawned on the player; plays the clip below if set, else lives for one cooldown")]
    public GameObject effectPrefab;
    [Tooltip("Effect clip per facing: up, right, down, left")]
    public AnimationClip[] animations;
    [Tooltip("Effect draw order relative to the player. 1 = in front, -1 = behind")]
    [HideIfSet("effectPrefab")] public int effectOrder = 1;

    public HitBoxSide GetSide(int facing) => new[]{up, right, down, left}[facing];
    public Vector2 HitBoxPos(Vector2 playerPos, int facing) =>
        playerPos + new Vector2(0,0.5f) + Direction.vec2MoveDict[facing] * hitBoxDistance + GetSide(facing).offset;
    // Hitbox outline around HitBoxPos: the side's custom shape, else the length x width box (code-made only), else null
    public Vector2[] GetShape(int facing){
        Vector2[] shape = GetSide(facing).shape;
        if(shape!=null && shape.Length>=3)return shape;
        if(hitBoxPrefab!=null || hitBoxLength<=0 || hitBoxWidth<=0)return null;
        Vector2 f = Direction.vec2MoveDict[facing] * hitBoxLength, w = Vector2.Perpendicular(Direction.vec2MoveDict[facing]) * hitBoxWidth/2;
        return new[]{w, f+w, f-w, -w};
    }

    // Attack: play the effect and spawn the hitbox. Returns the effect object (or null)
    public GameObject Use(Player player){
        if(useSound!=null)SoundPlayer.PlaySound(useSound);
        SpawnHitBox(player);
        return SpawnEffect(player);
    }

    GameObject SpawnEffect(Player player){
        int facing = player.animFacing;
        AnimationClip clip = facing<animations.Length? animations[facing]:null;
        if(clip==null && effectPrefab==null)return null;
        GameObject effect;
        if(effectPrefab!=null)effect = Instantiate(effectPrefab, player.transform);
        else{
            effect = new GameObject(name+" effect", typeof(SpriteRenderer));
            CopySorting(player, effect.GetComponent<SpriteRenderer>(), effectOrder);
            effect.transform.SetParent(player.transform, false);
        }
        if(clip==null){Destroy(effect, cooldown); return effect;}
        if(!effect.TryGetComponent(out Animator animator))animator = effect.AddComponent<Animator>();
        float speed = Mathf.Max(1, clip.length / cooldown);
        AnimationPlayableUtilities.PlayClip(animator, clip, out PlayableGraph graph).SetSpeed(speed); // no Animator Controller needed
        player.StartCoroutine(EndEffect(effect, graph, clip.length / speed * .9f));
        return effect;
    }

    static IEnumerator EndEffect(GameObject effect, PlayableGraph graph, float delay){
        yield return new WaitForSeconds(delay);
        if(graph.IsValid())graph.Destroy();
        if(effect!=null)Destroy(effect);
    }

    HitBox SpawnHitBox(Player player){
        int facing = player.animFacing;
        Vector2 playerPos = player.transform.position;
        Vector2 pos = HitBoxPos(playerPos, facing);
        Quaternion rot = velocity!=0?Quaternion.identity:Quaternion.FromToRotation(Vector2.right, Direction.vec2MoveDict[facing]);
        Vector2[] shape = GetShape(facing);
        GameObject go;
        if(hitBoxPrefab!=null){
            go = Instantiate(hitBoxPrefab, pos, rot).gameObject;
            if(shape!=null)foreach(var c in go.GetComponents<Collider2D>())c.enabled=false; // replaced by the shape below
        }else{
            go = new GameObject(name+" hitbox");
            go.transform.SetPositionAndRotation(pos, rot);
            for(int i=0;i<32;i++)if((hitBoxLayer.value & 1<<i)!=0){go.layer = i; break;}
            go.AddComponent<Rigidbody2D>().gravityScale = 0;
            if(shape==null)go.AddComponent<BoxCollider2D>().isTrigger = true;
            if(hitBoxSprite!=null){var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = hitBoxSprite; CopySorting(player, sr, 1);}
        }
        if(shape!=null){
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = System.Array.ConvertAll(shape, p => (Vector2)(Quaternion.Inverse(rot) * p)); // shape is in world directions
        }
        if(!go.TryGetComponent(out HitBox hb))hb = go.AddComponent<HitBox>();
        hb.weapon = this;
        hb.damage = damage * (isMagical? player.combatStat.attackMagic:player.combatStat.attackPhysic);
        hb.playerPos = playerPos;
        Vector2 v = player.looking.normalized * velocity;
        if(v.magnitude!=0)go.GetComponent<Rigidbody2D>().velocity = v;
        if(destroyTimer!=0)Destroy(go, destroyTimer);
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
            Vector2[] s = GetShape(i);
            Gizmos.DrawWireSphere(pos, 0.05f);
            if(s!=null&&s.Length>=3)for(int j=0;j<s.Length;j++)Gizmos.DrawLine(pos+s[j], pos+s[(j+1)%s.Length]);
        }
    }
}

public class HideIfSetAttribute : PropertyAttribute // Inspector: hide this field while another field is assigned
{
    public string field;
    public HideIfSetAttribute(string field){this.field = field;}
}
#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(HideIfSetAttribute))]
class HideIfSetDrawer : UnityEditor.PropertyDrawer
{
    bool Hidden(UnityEditor.SerializedProperty p) =>
        p.serializedObject.FindProperty(((HideIfSetAttribute)attribute).field)?.objectReferenceValue != null;
    public override float GetPropertyHeight(UnityEditor.SerializedProperty p, GUIContent label) =>
        Hidden(p) ? -UnityEditor.EditorGUIUtility.standardVerticalSpacing : UnityEditor.EditorGUI.GetPropertyHeight(p, label, true);
    public override void OnGUI(Rect r, UnityEditor.SerializedProperty p, GUIContent label){
        if(!Hidden(p))UnityEditor.EditorGUI.PropertyField(r, p, label, true);
    }
}
#endif

[System.Serializable]
public class HitBoxSide
{
    [Tooltip("Extra hitbox spawn offset")]
    public Vector2 offset;
    [Tooltip("Optional custom hitbox shape, 3+ points around the spawn point. Empty = use the box (or the prefab's collider)")]
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
