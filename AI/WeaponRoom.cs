using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Weapons room (RoomType.weapons, DungeonRoom > Weapon Room): while the player is in it, chat votes the right hand
// ("vote 2", "vote spear", AI/ChatVote.cs). Time up: the streamer's vote is last, the spot they stand at.
// The winner stays in the right hand (no swap, no throw) until the next weapons room; the old one drops on its spot.
[System.Serializable]
public class WeaponRoom{
    [Tooltip("What chat picks from, in vote order (vote 1, vote 2...). Not keys: a used key empties the hand")]
    public List<Weapon> weapons = new();
    [Tooltip("Optional: one SpriteRenderer per weapon (a pedestal with its number). Shows it; the streamer votes by standing there")]
    public List<SpriteRenderer> spots = new();
    public ChatVote vote = new();
    [Tooltip("When chat has picked, e.g. the exit Door's OpenDoor")]
    public UnityEngine.Events.UnityEvent onVoted;
    bool done;

    // DungeonRoom.OnEnable: the player came in
    public void Enter(){
        if(done || weapons.Count==0)return;
        vote.Open("RIGHT HAND", weapons.ConvertAll(w=>w.name.ToLowerInvariant()), 0, Pick);
        for(int i=0;i<spots.Count && i<weapons.Count;i++)if(spots[i])spots[i].sprite = weapons[i].uiWep;
    }
    public void Leave()=>vote.Close(); // DungeonRoom.OnDisable: it starts again next time

    void Pick(){
        var p = GameManager.Instance.player;
        int at = spots.FindIndex(s=>s && Vector2.Distance(s.transform.position, p.center)<1);
        if(at>=0 && at<weapons.Count)vote.Add("(streamer)", at); // the last vote; "(" isn't in Twitch names
        int win = vote.Winner();
        var hand = p.rWeapon;
        var spot = win<spots.Count && spots[win] ? spots[win].transform : p.transform;
        if(hand.weapon!=null && hand.weapon!=PrefabDatabase.Instance.fist)Loot.Drop(null, hand.weapon, spot);
        hand.weapon = weapons[win]; hand.chatFixed = true;
        foreach(var s in spots)if(s)s.sprite = null;
        done = true;
        GameManager.Instance.ui.UpdateWeap();
        GameManager.Instance.ui.ShowText("chat picked "+vote.options[win]);
        onVoted.Invoke();
    }
}
