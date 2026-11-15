using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Potions: chat types "potion" or "potion health". They wait in potionQ with potionSkill,
// or (Experimental.potionSamePool) in summonQ with the enemies and spawnSkill. A room gets potionsPerRoom, ever.
public static class ChatPotion
{
    public static void Add(SimpleSummonDB db, string username, string name){
        var c = new SummonCommand("potion "+name.ToLowerInvariant(),username);
        if(Experimental.I.potionSamePool)db.Add(username,c);
        else db.potionQ.Add(c);
    }
    // GameManager.Update, before GetToSpawn: the potion whose turn it is, or null
    public static Loot Pop(SimpleSummonDB db)=>Pop(db,db.potionQ,0,db.potionSkill) ?? Pop(db,db.summonQ,db.indexQ,db.spawnSkill);
    static Loot Pop(SimpleSummonDB db, List<SummonCommand> q, int from, Skill skill){
        if(!skill.isReady || q.Count<=from || !db.Next(q,from).spawnableName.StartsWith("potion "))return null;
        string name = db.Count(q[from]).spawnableName.Replace("potion ","");
        q.RemoveAt(from);
        var p = db.potions.Find(x=>x && x.name.ToLowerInvariant().StartsWith(name));
        if(p==null || GameManager.Instance.currentRoom.potionsSpawned>=db.potionsPerRoom)return null; // no cooldown used
        skill.Call();
        return p;
    }
}
