using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Weapons room (RoomType.weapons, DungeonRoom > Weapon Room): while the player is in it, chat votes the right hand
// ("vote 2", "vote spear", AI/ChatVote.cs). Coming in stuns the player a moment. Time up: the streamer's vote is last
// (the spot they stand at), then the player walks to the winner. It stays in the right hand (no swap, no throw)
// until the next weapons room; the old one drops on its spot. The others: Experimental.destroyUnpicked
[System.Serializable]
public class WeaponRoom{
    [Tooltip("What chat picks from, in vote order (vote 1, vote 2...). Not keys: a used key empties the hand")]
    public List<Weapon> weapons = new();
    [Tooltip("Optional: one SpriteRenderer per weapon (a pedestal with its number). Shows it; the streamer votes by standing there")]
    public List<SpriteRenderer> spots = new();
    [Tooltip("Where the old weapon drops when its spot is empty: an object inside the room")]
    public Transform dropAt;
    public ChatVote vote = new();
    [Tooltip("Seconds the player can't walk after coming in (0 = free)")]
    public float stunTime = 3;
    [Tooltip("When chat has picked, e.g. the exit Door's OpenDoor")]
    public UnityEngine.Events.UnityEvent onVoted;
    bool done;

    // DungeonRoom.OnEnable: the player came in
    public void Enter(){
        if(done || weapons.Count==0)return;
        vote.Open("RIGHT HAND", weapons.ConvertAll(w=>w.name.ToLowerInvariant()), 0, Pick);
        new BuffEffect.Buff{type = BuffType.stun, duration = stunTime}.CopyTo(GameManager.Instance.player.gameObject);
        for(int i=0;i<spots.Count && i<weapons.Count;i++)if(spots[i])spots[i].sprite = weapons[i].uiWep;
    }
    public void Leave()=>vote.Close(); // DungeonRoom.OnDisable: it starts again next time

    void Pick(){
        var p = GameManager.Instance.player;
        int at = spots.FindIndex(s=>s && Vector2.Distance(s.transform.position, p.center)<1);
        if(at>=0 && at<weapons.Count)vote.Add("(streamer)", at); // the last vote; "(" isn't in Twitch names
        int win = vote.Winner();
        done = true;
        ChatFeed.Add("chat", "picked "+vote.options[win]);
        var spot = win<spots.Count && spots[win] ? spots[win].transform : null;
        p.StartCoroutine(WalkTo(p, spot, ()=>Take(p, win, spot ? spot : dropAt ? dropAt : p.transform)));
    }
    IEnumerator WalkTo(Player p, Transform spot, System.Action then){ // the player walks to the winner, then is free
        var room = GameManager.Instance.currentRoom;
        for(float t=0; spot && t<5 && GameManager.Instance.currentRoom==room && Vector2.Distance(p.center, spot.position)>.3f; t+=Time.deltaTime){ // stuck 5 s or died: it's given anyway
            p.forceWalk = PathFinding.Direction(p.center, spot.position, room.bounds, p.bodyCollider);
            yield return null;
        }
        p.forceWalk = Vector2.zero;
        then();
    }
    void Take(Player p, int win, Transform at){
        var hand = p.rWeapon;
        if(hand.weapon!=null && hand.weapon!=PrefabDatabase.Instance.fist)Loot.Drop(null, hand.weapon, at);
        hand.weapon = weapons[win]; hand.chatFixed = true;
        for(int i=0;i<spots.Count;i++)if(spots[i]){
            if(i!=win && i<weapons.Count && !Experimental.I.destroyUnpicked)Loot.Drop(null, weapons[i], spots[i].transform); // left as loot
            spots[i].sprite = null;
        }
        GameManager.Instance.ui.UpdateWeap();
        onVoted.Invoke();
    }
}
