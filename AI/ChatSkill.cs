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
    protected void Set(ChatType word, BuffType type, float duration, float cooldown=60){
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
// holes don't take the player (Map/Hole.cs); if it ends over a hole, the player falls
[System.Serializable]
public class Floating : ChatSkill{
    public Floating(){Set(ChatType.floating, BuffType.floating, 10, 30);}
    public override void Cast(Player p){
        base.Cast(p);
        p.rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // so it falls when the float ends over a hole
    }
}
// earthquake / quake: its stun and a push go on every enemy of the room; the player is stunned too,
// unless invincible (buff or the frames after a hit) or floating. Never pushed.
[System.Serializable]
public class Earthquake : ChatSkill{
    [Tooltip("How hard enemies are pushed away from the player")]
    public float knockback = 6;
    public Earthquake(){Set(ChatType.earthquake, BuffType.stun, 3, 45);}
    public override bool CanCast()=>Enemies().Length>0; // no enemies: it would only stun the player
    public override void Cast(Player p){
        foreach(var e in Enemies()){buff.CopyTo(e.gameObject); e.receiveKnockback(p.center, knockback);}
        bool immune = Experimental.I.enableInvincible && p.isInvincible || p.Has(BuffType.invincible|BuffType.floating);
        if(!immune)base.Cast(p);
    }
    static Enemy[] Enemies()=>System.Array.FindAll(GameManager.Instance.currentRoom.GetComponentsInChildren<Enemy>(), e=>!(e is Chest));
}
