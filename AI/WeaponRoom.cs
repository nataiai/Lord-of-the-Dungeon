using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Weapons room: while the player is in it, chat votes the right hand ("vote 2" or "vote spear", again = move it).
// Time up: the streamer's vote is last, the spot they stand at. Tie or no votes: random.
// The winner stays in the right hand (no swap, no throw) until the next weapons room; the old one drops on its spot.
// Put it on an object inside the room. Text: UI.voteText
public class WeaponRoom : MonoBehaviour
{
    [Tooltip("What chat picks from, in vote order (vote 1, vote 2...). Not keys: a used key empties the hand")]
    public List<Weapon> weapons = new();
    [Tooltip("Optional: one SpriteRenderer per weapon (a pedestal with its number). Shows it; the streamer votes by standing there")]
    public List<SpriteRenderer> spots = new();
    public float voteTime = 30;
    [Tooltip("When chat has picked, e.g. the exit Door's OpenDoor")]
    public UnityEngine.Events.UnityEvent onVoted;
    static WeaponRoom open; // the vote now
    Dictionary<string,int> votes = new(); // chatter: weapon index
    float end;
    bool done;

    void OnEnable(){ // the player came in
        if(done || weapons.Count==0)return;
        votes.Clear(); end = Time.time+voteTime; open = this;
        for(int i=0;i<spots.Count && i<weapons.Count;i++)if(spots[i])spots[i].sprite = weapons[i].uiWep;
    }
    void OnDisable(){if(open==this)open = null;} // left: it starts again next time
    void Update(){if(open==this && Time.time>end)Pick();}

    // Chat.addToDB
    public static void Vote(string username, string word){
        if(open==null)return;
        int i = int.TryParse(word, out int n) ? n-1 : open.weapons.FindIndex(w=>w.name.ToLowerInvariant().StartsWith(word.ToLowerInvariant()));
        if(i>=0 && i<open.weapons.Count)open.votes[username] = i;
    }
    int[] Count(){var c = new int[weapons.Count]; foreach(var v in votes.Values)c[v]++; return c;}
    // UI.voteText: RIGHT HAND  1 sword 4 · 2 spear 2  22s
    public static string Text(){
        if(open==null)return "";
        var c = open.Count();
        var t = new List<string>();
        for(int i=0;i<c.Length;i++)t.Add($"{i+1} {open.weapons[i].name.ToLowerInvariant()} {c[i]}");
        return $"RIGHT HAND  {string.Join(" · ", t)}  {open.end-Time.time:0}s";
    }

    void Pick(){
        var p = GameManager.Instance.player;
        var c = Count();
        int at = spots.FindIndex(s=>s && Vector2.Distance(s.transform.position, p.center)<1); // the streamer
        if(at>=0 && at<c.Length)c[at]++;
        var best = new List<int>();
        int most = Mathf.Max(c);
        for(int i=0;i<c.Length;i++)if(c[i]==most)best.Add(i);
        int win = best[Random.Range(0, best.Count)];
        var hand = p.rWeapon;
        var spot = win<spots.Count && spots[win] ? spots[win].transform : transform;
        if(hand.weapon!=null && hand.weapon!=PrefabDatabase.Instance.fist)Loot.Drop(null, hand.weapon, spot);
        hand.weapon = weapons[win]; hand.chatFixed = true;
        foreach(var s in spots)if(s)s.sprite = null;
        done = true; open = null;
        GameManager.Instance.ui.UpdateWeap();
        GameManager.Instance.ui.ShowText("chat picked "+weapons[win].name.ToLowerInvariant());
        onVoted.Invoke();
    }
}
