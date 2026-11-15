using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Summon order: each cooldown, the chatter with the fewest turns goes first, then the oldest request.
// Potions: chat types "potion" or "potion health". They wait in potionQ with potionSkill,
// or (Experimental.potionSamePool) in summonQ with the enemies and spawnSkill. A room gets potionsPerRoom, ever.
public static class ChatPotion
{
    static readonly Dictionary<string,int> turns = new(); // per chatter

    // moves the next request to q[from]
    public static SummonCommand Next(List<SummonCommand> q, int from){
        int best = from;
        for(int i=from+1;i<q.Count;i++)if(Turns(q[i])<Turns(q[best]))best = i;
        var c = q[best];
        q.RemoveAt(best);
        q.Insert(from,c);
        return c;
    }
    static int Turns(SummonCommand c)=>turns.TryGetValue(c.username,out int n)?n:0;
    public static SummonCommand Count(SummonCommand c){turns[c.username] = Turns(c)+1; return c;}
    public static void NewRoom(SimpleSummonDB db){db.Reset();db.potionQ.Clear();}

    public static void Add(SimpleSummonDB db, string username, string name){
        var c = new SummonCommand("potion "+name.ToLowerInvariant(),username);
        if(Experimental.I.potionSamePool)db.Add(username,c);
        else db.potionQ.Add(c);
    }
    // GameManager.Update, before GetToSpawn: the potion whose turn it is, or null
    public static Loot Pop(SimpleSummonDB db)=>Pop(db,db.potionQ,0,db.potionSkill) ?? Pop(db,db.summonQ,db.indexQ,db.spawnSkill);
    static Loot Pop(SimpleSummonDB db, List<SummonCommand> q, int from, Skill skill){
        if(!skill.isReady || q.Count<=from || !Next(q,from).spawnableName.StartsWith("potion "))return null;
        string name = Count(q[from]).spawnableName.Replace("potion ","");
        q.RemoveAt(from);
        var p = db.potions.Find(x=>x && x.name.ToLowerInvariant().StartsWith(name));
        if(p==null || GameManager.Instance.currentRoom.potionsSpawned>=db.potionsPerRoom)return null; // no cooldown used
        skill.Call();
        return p;
    }
}
