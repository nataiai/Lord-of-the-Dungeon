using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Potions: chat types "potion" or "potion health". They wait in this line with its spawnSkill,
// or (Experimental.potionSamePool) in the summon line with the enemies. A room gets potionsPerRoom, ever.
[System.Serializable]
public class ChatPotion : ChatLine
{
    public List<Loot> potions=new();
    public int potionsPerRoom = 3;
    public ChatPotion(){spawnSkill = new(){chatType = ChatType.potion, cooldown = 10};}

    public void Add(SimpleSummonDB db, string username, string name){
        var c = new SummonCommand("potion "+name.ToLowerInvariant(),username);
        if(Experimental.I.potionSamePool)db.Add(username,c);
        else Add(username,c);
    }
    // the potion whose turn it is, or null
    public Loot Pop(SimpleSummonDB db)=>PopFrom(this) ?? PopFrom(db);
    Loot PopFrom(ChatLine line){
        var c = line.spawnSkill.isReady ? line.Next() : null;
        if(c==null || !c.spawnableName.StartsWith("potion "))return null;
        line.Take();
        string name = c.spawnableName.Replace("potion ","");
        var p = potions.Find(x=>x && x.name.ToLowerInvariant().StartsWith(name));
        if(p==null || GameManager.Instance.currentRoom.potionsSpawned>=potionsPerRoom)return null; // no cooldown used
        line.spawnSkill.Call();
        return p;
    }
}
