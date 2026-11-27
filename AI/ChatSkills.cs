using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Each chat skill: what it does, how it looks (below), and test keys 1-6. Chat votes cast them: Connector/ChatSkill.cs
public static class ChatSkillExtras{
    static SpriteRenderer Body(Player p)=>System.Array.Find(p.GetComponentsInChildren<SpriteRenderer>(), s=>s.sprite); // the sprite you see
    static void Copy(SpriteRenderer copy, SpriteRenderer of, Vector3 offset){ // it walks too
        copy.transform.position = of.transform.position+offset; copy.transform.localScale = of.transform.localScale;
        copy.sprite = of.sprite; copy.flipX = of.flipX;
    }
    // GameManager.Update
    public static void Keys(ChatSkill[] skills){
        var keys = UnityEngine.InputSystem.Keyboard.current;
        if(!Experimental.I.chatSkills || !Experimental.I.skillKeys || keys==null)return;
        for(int i=0;i<skills.Length && i<9;i++)
            if(keys[UnityEngine.InputSystem.Key.Digit1+i].wasPressedThisFrame)ChatSkill.Vote(skills, "keys", skills[i].chatType, "key"); // as a chatter would
    }

    // Berserk: a red copy on top, for now (the Animator keeps the sprite's own color)
    public static void Red(Player p){var sr = Body(p); if(sr)p.StartCoroutine(Red(sr, p));}
    static IEnumerator Red(SpriteRenderer sr, Player p){
        var red = ThrownWeapon.Shadow(sr, sr.transform.parent);
        red.name = "berserk"; red.color = new Color(1,0,0,.5f); red.sortingOrder = sr.sortingOrder+1;
        while(p.Has(BuffType.berserk)){Copy(red, sr, Vector3.zero); yield return null;}
        Object.Destroy(red.gameObject);
    }

    static bool up; static float ends; // floating: lifted now; when it ends
    // Floating: the player floats up, a shadow stays below (like a thrown weapon's)
    public static void Lift(Floating f, Player p){
        ends = Time.time+f.buff.duration;
        if(!up)p.StartCoroutine(Lifting(f, p));
    }
    static IEnumerator Lifting(Floating f, Player p){
        var sr = p.GetComponentInChildren<SpriteRenderer>(); // its sorting layer, like a thrown weapon's shadow
        if(sr==null)yield break;
        var body = Body(p) ?? sr;
        up = true;
        var shadow = ThrownWeapon.Shadow(sr, body.transform.parent);
        float rise = Mathf.Max(f.riseTime,.01f), t = 0, h = 0, left = 1; // t: 0 = on the ground, 1 = up
        while(left>0){
            left = p.Has(BuffType.floating) ? ends-Time.time : 0;
            t = Mathf.Min(Mathf.MoveTowards(t, 1, Time.deltaTime/rise), left/rise); // up at the start, lands as it ends
            float was = h;
            h = f.height*Mathf.SmoothStep(0, 1, t);
            p.rb.position += Vector2.up*(h-was); // the player, not its sprite: the Animator moves that
            Copy(shadow, body, Vector3.down*h);
            yield return null;
        }
        Object.Destroy(shadow.gameObject);
        up = false;
    }
}

// attack x2 at full HP up to x4 (Weapon.Damage), no healing (Character.Heal)
[System.Serializable]
public class Berserk : ChatSkill{
    public Berserk(){Set(ChatType.berserk, BuffType.berserk, 3);} //test: 15
    public static float Damage(Character by)=>by.Has(BuffType.berserk) ? 4-2f*by.HP/by.maxHP : 1;
    public override void Cast(Player p){base.Cast(p); ChatSkillExtras.Red(p);} // looks: AI/
}
// no damage, no knockback (Player)
[System.Serializable]
public class Invincible : ChatSkill{
    public Invincible(){Set(ChatType.invincible, BuffType.invincible, 3);} //test: 10
}
// blocks magic damage (Player.recieveDamage). Enemy data > isMagical: its touch is magic
[System.Serializable]
public class MagicShield : ChatSkill{
    public MagicShield(){Set(ChatType.magicShield, BuffType.magicShield, 3);} //test: 15
}
// blocks physical damage (Player.recieveDamage)
[System.Serializable]
public class PhysicalShield : ChatSkill{
    public PhysicalShield(){Set(ChatType.physicalShield, BuffType.physicalShield, 3);} //test: 15
}
// holes don't take the player (Map/Hole.cs); if it ends over a hole, the player falls.
// The player floats up, a shadow stays below (like a thrown weapon's). Knockback slides further (friction)
[System.Serializable]
public class Floating : ChatSkill{
    [Tooltip("How high the player floats above its shadow")]
    public float height = 1;
    [Tooltip("Seconds to float up, and to come down before it ends")]
    public float riseTime = 1;
    [Tooltip("How fast knockback slows down while floating, x the ground's: 1 = like the ground, 0 = no friction (slides on)")]
    public float friction = .2f;
    public Floating(){Set(ChatType.floating, BuffType.floating, 5);} //test: 10
    // Player.Update: knockback slows down less in the air
    public static float Friction(Character c)=>c.Has(BuffType.floating) ? GameManager.Instance.summondb.floating.friction : 1;
    public override void Cast(Player p){
        base.Cast(p);
        p.rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // so it falls when the float ends over a hole
        ChatSkillExtras.Lift(this, p); // up, a shadow below (AI/)
    }
}
// earthquake / quake: its stun goes on every enemy of the room and the player, then shakes push them all at once,
// each a random way. Not the player while invincible (buff or the frames after a hit) or floating.
[System.Serializable]
public class Earthquake : ChatSkill{
    [Tooltip("How hard each shake pushes the enemies and the player, each a random way")]
    public float knockback = 6;
    [Tooltip("How many shakes: all pushed at once, the first right away")]
    public int shakes = 30;
    [Tooltip("Seconds the shakes are spread over")]
    public float shakeTime = 1.5f;
    public Earthquake(){Set(ChatType.earthquake, BuffType.stun, 3);}
    public override bool CanCast()=>Enemies().Length>0; // no enemies: it would only stun the player
    public override void Cast(Player p){
        var shaken = new List<Character>(Enemies());
        bool immune = Experimental.I.enableInvincible && p.isInvincible || p.Has(BuffType.invincible|BuffType.floating);
        if(!immune)shaken.Add(p);
        foreach(var c in shaken)buff.CopyTo(c.gameObject);
        p.StartCoroutine(Shake(shaken));
    }
    IEnumerator Shake(List<Character> shaken){
        for(int i=0;i<shakes;i++){
            foreach(var c in shaken)if(c)c.receiveKnockback((Vector2)c.transform.position-Random.insideUnitCircle.normalized, knockback);
            yield return new WaitForSeconds(shakeTime/shakes);
        }
    }
    static Enemy[] Enemies()=>System.Array.FindAll(GameManager.Instance.currentRoom.GetComponentsInChildren<Enemy>(), e=>!(e is Chest));
}
