using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VoteMethod{plurality, runoff} // new ones at the end (saved as numbers)

// Every chat vote: crowd skills (AI/ChatSkill.cs), the weapons room (AI/WeaponRoom.cs). One ballot per chatter, again = moved.
// need > 0: it passes once an option has need votes; time up = nothing. need 0: time up, method picks (tie or none: random).
// For the UI: ChatVote.open, and each vote's Text() or title, options, Counts(), need, timeLeft, first, last
[System.Serializable]
public class ChatVote{
    [Tooltip("plurality: the most votes wins. runoff: votes can be ranked (\"vote 2 3\"); the fewest drop out until one has over half")]
    public VoteMethod method;
    [Tooltip("Seconds it stays open")]
    public float time = 30;
    [System.NonSerialized] public string title, first, last; // first and last to vote
    [System.NonSerialized] public List<string> options = new();
    [System.NonSerialized] public int need;
    System.Action onEnd; // time up
    float end;
    Dictionary<string,List<int>> ballots = new(); // chatter: options, first choice first
    public static readonly List<ChatVote> open = new();
    public bool isOpen => open.Contains(this) && timeLeft>=0;
    public float timeLeft => end-Time.time;
    public int voters => ballots.Count;

    public void Open(string title, List<string> options, int need=0, System.Action onEnd=null){
        this.title = title; this.options = options; this.need = need; this.onEnd = onEnd;
        ballots.Clear(); first = last = ""; end = Time.time+time;
        if(!open.Contains(this))open.Add(this);
    }
    public void Close()=>open.Remove(this); // no winner
    // GameManager.Update
    public static void Tick(){foreach(var v in open.ToArray())if(v.timeLeft<0){v.Close(); v.onEnd?.Invoke();}}

    // words: option numbers (1, 2...) or how their names start, first choice first. true: it passed (need)
    public bool Ballot(string username, params string[] words){
        var picks = new List<int>();
        foreach(var w in words){int i = Option(w); if(i>=0 && !picks.Contains(i))picks.Add(i);}
        if(!isOpen || picks.Count==0)return false;
        if(method==VoteMethod.plurality)picks.RemoveRange(1, picks.Count-1);
        if(ballots.Count==0)first = username;
        ballots[username] = picks; last = username;
        if(need<=0 || Mathf.Max(Counts())<need)return false;
        Close();
        return true;
    }
    public void Add(string username, int option){ballots[username] = new(){option}; last = username;} // e.g. the streamer, after time up
    int Option(string w)=>int.TryParse(w, out int n) ? (n>0 && n<=options.Count ? n-1 : -1)
        : options.FindIndex(o=>o.StartsWith(w.ToLowerInvariant()));
    public int[] Counts(){var c = new int[options.Count]; foreach(var b in ballots.Values)c[b[0]]++; return c;} // first choices

    // the winner by method; tie or no votes: random. -1 = no options
    public int Winner(){
        if(options.Count==0)return -1;
        var gone = new List<int>(); // runoff: dropped out
        while(true){
            var c = new int[options.Count]; int all = 0;
            foreach(var b in ballots.Values){int k = b.FindIndex(o=>!gone.Contains(o)); if(k>=0){c[b[k]]++; all++;}}
            var still = new List<int>();
            for(int i=0;i<c.Length;i++)if(!gone.Contains(i))still.Add(i);
            int most = 0, fewest = all;
            foreach(var i in still){most = Mathf.Max(most, c[i]); fewest = Mathf.Min(fewest, c[i]);}
            var top = still.FindAll(i=>c[i]==most);
            if(method==VoteMethod.plurality || most*2>all || top.Count==still.Count)return top[Random.Range(0, top.Count)];
            var low = still.FindAll(i=>c[i]==fewest);
            gone.Add(low[Random.Range(0, low.Count)]);
        }
    }

    // one line for the UI: "berserk 1/2 +Ann 9s" or "RIGHT HAND  1 sword 4 · 2 spear 2  22s"
    public string Text(){
        var c = Counts();
        if(need>0)return $"{title} {Mathf.Max(c)}/{need} +{last} {timeLeft:0}s";
        var t = new List<string>();
        for(int i=0;i<c.Length;i++)t.Add($"{i+1} {options[i]} {c[i]}");
        return $"{title}  {string.Join(" · ", t)}  {timeLeft:0}s";
    }
    public static string Texts()=>string.Join("\n", open.FindAll(v=>v.isOpen).ConvertAll(v=>v.Text()));
    // Chat.addToDB: "vote 2", "vote spear", ranked "vote 2 3": the newest open vote without a need
    public static void Vote(string username, string[] words){
        var v = open.FindLast(x=>x.isOpen && x.need<=0);
        v?.Ballot(username, new List<string>(words).GetRange(1, words.Length-1).ToArray());
    }
}
