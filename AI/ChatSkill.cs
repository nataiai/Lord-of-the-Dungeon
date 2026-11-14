using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chat skills (design: AI/CHAT_SKILLS.md). On/off: Experimental.chatSkills.
// Teleport: "teleport 3 4" = tile 3 4 of the room (1 1 = its bottom left floor tile; a ruler on the room's edge shows the numbers).
// One request per chatter, the fair line (the chatter with the fewest turns first), teleportSkill's cooldown.
// Buff words (berserk, invincible, magicshield, physicalshield; 3+ letters): the buff goes on the player. Summondb > Skills
public partial class SimpleSummonDB{
    string said; float saidAt;
    readonly HashSet<DungeonRoom> ruled = new();
    public void Say(string s){said = s; saidAt = Time.time;}
    public string ChatText()=>Time.time<saidAt+4 ? said : ""; // UI.chatText: chat can't be answered, the screen can

    // Chat.addToDB
    public void AddTeleport(string username, int x, int y){
        if(!Experimental.I.chatSkills){GameManager.Instance.TeleportSkill(x,y); return;} // as before: at once, a world position
        teleportQ.RemoveAll(c=>c.username==username); // a newer one replaces it
        teleportQ.Add(new SummonCommand("teleport",username,x+" "+y));
    }
    // Chat.addToDB: a buff word. One chatter casts it, its cooldown limits how often
    public void Call(string username, ChatType type, string msg){
        var s = skills.Find(x=>x.chatType==type);
        if(!Experimental.I.chatSkills || s==null || msg.Trim().Length<3 || !s.Call())return; // "b" or "i" alone is no cast
        s.buff.CopyTo(GameManager.Instance.player.gameObject);
        Say(username+": "+type+"!");
    }
    // GameManager.Update
    public void ChatUpdate(){
        var gm = GameManager.Instance;
        if(!Experimental.I.chatSkills || gm.map==null || gm.currentRoom==null)return;
        if(Experimental.I.teleportRoomTiles)Ruler(gm.currentRoom);
        if(teleportQ.Count==0 || !teleportSkill.isReady || !gm.player.enabled || InDoorway(gm.player))return; // falling, in a doorway: it waits
        var c = Count(Next(teleportQ,0));
        teleportQ.RemoveAt(0);
        string[] xy = c.spawnableLocation.Split(' ');
        Vector2 pos = new(int.Parse(xy[0]), int.Parse(xy[1]));
        if(Experimental.I.teleportRoomTiles)pos += (Vector2)gm.currentRoom.bounds.min - Vector2.one*.5f; // the tile's middle
        if(!gm.TeleportSkill(pos.x,pos.y)){Say(c.username+": "+c.spawnableLocation+" is blocked"); return;} // no cooldown: the next asker is tried
        teleportSkill.Call();
        teleportQ.Clear(); // ask again: no request is older than a cooldown
        Say(c.username+" teleports you to "+c.spawnableLocation);
    }
    static bool InDoorway(Player p){ // a teleport out of a door trigger would switch rooms
        foreach(var c in Physics2D.OverlapPointAll(p.center))if(c.GetComponent<RoomTrigger>())return true;
        return false;
    }
    void Ruler(DungeonRoom room){ // tile numbers below and left of the floor, once per room
        if(!ruled.Add(room))return;
        var b = room.bounds;
        for(int i=1;i<=Mathf.RoundToInt(b.size.x);i++)Number(room, i, new Vector2(b.min.x+i-.5f, b.min.y-.5f));
        for(int i=1;i<=Mathf.RoundToInt(b.size.y);i++)Number(room, i, new Vector2(b.min.x-.5f, b.min.y+i-.5f));
    }
    static void Number(DungeonRoom room, int n, Vector2 at){
        var t = new GameObject("ruler "+n).AddComponent<TMPro.TextMeshPro>();
        t.transform.SetParent(room.transform, true);
        t.transform.position = at;
        t.text = n.ToString();
        t.fontSize = 4;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.sortingOrder = 50;
    }
}

[System.Serializable]
public class ChatSkill : Skill{ // a chat word that puts its buff on the player
    public BuffEffect.Buff buff = new();
    public static List<ChatSkill> Defaults() => new(){
        New(ChatType.berserk, BuffType.berserk, 15), // attack x2 to x4 (more with less HP), no healing
        New(ChatType.invincible, BuffType.invincible, 10), // no damage, no knockback
        New(ChatType.magicShield, BuffType.magicShield, 15),
        New(ChatType.physicalShield, BuffType.physicalShield, 15),
    };
    static ChatSkill New(ChatType word, BuffType type, float duration) => new(){chatType = word, cooldown = 60, buff = new(){type = type, duration = duration}};
}
