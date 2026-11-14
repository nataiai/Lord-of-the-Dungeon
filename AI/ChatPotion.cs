using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Summon order: each cooldown, the chatter with the fewest turns goes first, then the oldest request.
// Potions: chat types "potion" or "potion health". They wait in potionQ with potionSkill,
// or (Experimental.potionSamePool) in summonQ with the enemies and spawnSkill. A room gets potionsPerRoom, ever.
public partial class SimpleSummonDB{
    Dictionary<string,int> turns=new(); // per chatter

    // moves the next request to q[from]
    SummonCommand Next(List<SummonCommand> q, int from){
        int best = from;
        for(int i=from+1;i<q.Count;i++)if(Turns(q[i])<Turns(q[best]))best = i;
        var c = q[best];
        q.RemoveAt(best);
        q.Insert(from,c);
        return c;
    }
    int Turns(SummonCommand c)=>turns.TryGetValue(c.username,out int n)?n:0;
    SummonCommand Count(SummonCommand c){turns[c.username] = Turns(c)+1; return c;}
    public void NewRoom(){Reset();potionQ.Clear();}

    public void AddPotion(string username, string name){
        var c = new SummonCommand("potion "+name.Replace("\U000E0000","").ToLowerInvariant(),username); // Chatterino adds that to a repeat
        if(Experimental.I.potionSamePool)Add(username,c);
        else potionQ.Add(c);
    }
    // GameManager.Update, before GetToSpawn: the potion whose turn it is, or null
    public Loot GetPotionToSpawn()=>PopPotion(potionQ,0,potionSkill) ?? PopPotion(summonQ,indexQ,spawnSkill);
    Loot PopPotion(List<SummonCommand> q, int from, Skill skill){
        if(!skill.isReady || q.Count<=from || !Next(q,from).spawnableName.StartsWith("potion "))return null;
        string name = Count(q[from]).spawnableName.Replace("potion ","");
        q.RemoveAt(from);
        var p = potions.Find(x=>x && x.name.ToLowerInvariant().StartsWith(name));
        if(p==null || GameManager.Instance.currentRoom.potionsSpawned>=potionsPerRoom)return null; // no cooldown used
        skill.Call();
        return p;
    }
}
