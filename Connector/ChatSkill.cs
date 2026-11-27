using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chat skills: chat types a word (3+ letters: "ber" works). One class per skill (AI/ChatSkills.cs), a Summondb field each.
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
    // Chat.addToDB: a command makes the chatter active; a skill word (3+ letters, "F" isn't one) is a vote
    public static void Read(ChatSkill[] skills, string username, ChatType type, string msg){
        if(type!=ChatType.invalid && (msg.Trim().Length>=3 || !System.Array.Exists(skills, x=>x.chatType==type)))seen[username] = Time.time;
        Vote(skills, username, type, msg);
    }
    // a vote (Read, Keys). On cooldown: no votes
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
}
