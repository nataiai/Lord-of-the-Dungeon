using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chat skills: chat types a word (3+ letters: "ber" works). One class per skill below, a Summondb field each.
// A crowd vote: it casts when need chatters typed it within voteTime (Experimental.crowdNeed).
// On/off: Experimental.chatSkills. Experimental.skillKeys: keys 1-6 cast them, for testing.
[System.Serializable]
public class ChatSkill : Skill{
    public BuffEffect.Buff buff = new();
    List<string> votes = new(); float until; int need; // the open vote: one per chatter, [0] opened it
    const float voteTime = 20, activeTime = 120;
    static Dictionary<string,float> seen = new(); // chatter: their last command
    static string said = ""; static float saidAt;
    protected void Set(ChatType word, BuffType type, float duration, float cooldown=5){ //test: 60, floating 30, earthquake 45
        chatType = word; this.cooldown = cooldown; buff = new(){type = type, duration = duration};
    }
    // what it does: its buff on the player. A skill can change it
    public virtual void Cast(Player p){buff.CopyTo(p.gameObject);}
    public virtual bool CanCast()=>true; // false: no votes

    // UI.chatText: chat can't be answered, the screen can
    public static string Text(){
        string t = Time.time<saidAt+4 ? said : "";
        foreach(var s in GameManager.Instance.summondb.skills)if(s.votes.Count>0 && Time.time<s.until)
            t += $"\n{s.chatType} {s.votes.Count}/{s.need} +{s.votes[s.votes.Count-1]} {s.until-Time.time:0}s"; // berserk 1/2 +Ann 9s
        return t.Trim();
    }
    // Chat.addToDB: any command makes the chatter active. A skill word needs 3+ letters ("F" isn't one)
    public static void Seen(ChatSkill[] skills, string username, ChatType type, string msg){
        if(type!=ChatType.invalid && (msg.Trim().Length>=3 || !System.Array.Exists(skills, x=>x.chatType==type)))seen[username] = Time.time;
    }
    // Chat.addToDB: a vote. On cooldown: no votes
    public static void Vote(ChatSkill[] skills, string username, ChatType type, string msg){
        var s = System.Array.Find(skills, x=>x.chatType==type);
        if(!Experimental.I.chatSkills || s==null || msg.Trim().Length<3 || !s.isReady || !s.CanCast())return; // "b" or "i" alone is no cast
        s.AddVote(username);
    }
    void AddVote(string username){
        if(Time.time>until)votes.Clear(); // time up: no cast, no cooldown used
        if(votes.Contains(username))return;
        if(votes.Count==0){until = Time.time+voteTime; need = Need();}
        votes.Add(username);
        if(votes.Count<need)return;
        Call();
        Cast(GameManager.Instance.player);
        said = votes[0]+(votes.Count>1 ? " +"+(votes.Count-1) : "")+": "+chatType+"!"; // Ann +2: earthquake!
        saidAt = Time.time;
        votes.Clear();
    }
    // crowdNeed x sqrt(chatters with a command in the last 2 min), at least 1
    static int Need(){
        int active = 0;
        foreach(var t in seen.Values)if(t>Time.time-activeTime)active++;
        return Mathf.Max(1, Mathf.RoundToInt(Experimental.I.crowdNeed*Mathf.Sqrt(active)));
    }
    // GameManager.Update
    public static void Keys(ChatSkill[] skills){
        var keys = UnityEngine.InputSystem.Keyboard.current;
        if(!Experimental.I.chatSkills || !Experimental.I.skillKeys || keys==null)return;
        for(int i=0;i<skills.Length && i<9;i++)
            if(keys[UnityEngine.InputSystem.Key.Digit1+i].wasPressedThisFrame)Vote(skills, "keys", skills[i].chatType, "key"); // as a chatter would
    }
}

// attack x2 at full HP up to x4 (Weapon.Damage), no healing (Character.Heal)
[System.Serializable]
public class Berserk : ChatSkill{
    public Berserk(){Set(ChatType.berserk, BuffType.berserk, 15);}
    public static float Damage(Character by)=>by.Has(BuffType.berserk) ? 4-2f*by.HP/by.maxHP : 1;
}
// no damage, no knockback (Player)
[System.Serializable]
public class Invincible : ChatSkill{
    public Invincible(){Set(ChatType.invincible, BuffType.invincible, 10);}
}
// blocks magic damage (Player.recieveDamage). Enemy data > isMagical: its touch is magic
[System.Serializable]
public class MagicShield : ChatSkill{
    public MagicShield(){Set(ChatType.magicShield, BuffType.magicShield, 15);}
}
// blocks physical damage (Player.recieveDamage)
[System.Serializable]
public class PhysicalShield : ChatSkill{
    public PhysicalShield(){Set(ChatType.physicalShield, BuffType.physicalShield, 15);}
}
// holes don't take the player (Map/Hole.cs); if it ends over a hole, the player falls.
// The player goes up, a shadow stays below (like a thrown weapon's)
[System.Serializable]
public class Floating : ChatSkill{
    [Tooltip("How high the player floats above its shadow")]
    public float height = .5f;
    bool up; // lifted now
    public Floating(){Set(ChatType.floating, BuffType.floating, 10);}
    public override void Cast(Player p){
        base.Cast(p);
        p.rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // so it falls when the float ends over a hole
        if(!up)p.StartCoroutine(Lift(p));
    }
    IEnumerator Lift(Player p){
        var sr = p.GetComponentInChildren<SpriteRenderer>();
        if(sr==null)yield break;
        up = true;
        var shadow = ThrownWeapon.Shadow(sr, sr.transform.parent);
        shadow.transform.localScale = sr.transform.localScale;
        for(float h=0; h!=0 || p.Has(BuffType.floating);){
            float was = h;
            h = Mathf.MoveTowards(h, p.Has(BuffType.floating) ? height : 0, height*4*Time.deltaTime); // up and down in 1/4 s
            p.transform.position += Vector3.up*(h-was); // the player, not its sprite: the Animator moves that
            shadow.transform.position = sr.transform.position+Vector3.down*h;
            shadow.sprite = sr.sprite; shadow.flipX = sr.flipX; // it walks too
            yield return null;
        }
        Object.Destroy(shadow.gameObject);
        up = false;
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
