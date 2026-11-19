using System.Collections.Generic;

[System.Serializable]
public class SummonCommand{
    public string username;
    public string spawnableName;
    public string spawnableLocation;
    public SummonCommand(string spawnableName, string username="", string spawnableLocation=""){
        this.username = username; this.spawnableName = spawnableName; this.spawnableLocation = spawnableLocation;
    }
}

// chat requests, one per cooldown: the chatter with the fewest turns first, then the oldest
[System.Serializable]
public class ChatLine{
    public Skill spawnSkill;
    public List<SummonCommand> summonQ=new();
    int indexQ=0;
    Dictionary<string,int> turns=new(); // per chatter
    [UnityEngine.Tooltip("Requests one chatter can have waiting. 0 = no limit")]
    public int limitPerPlayer = 10;
    [UnityEngine.Tooltip("Requests the whole line can have waiting; more are turned away. 0 = no limit")]
    public int limitTotal = 300;

    public void Reset(){summonQ.Clear();indexQ=0;}
    public void Add(string name,SummonCommand command){
        if(indexQ>limitTotal){summonQ.RemoveRange(0,indexQ);indexQ=0;} // forget what's done, not what waits
        int mine = 0;
        for(int i=indexQ;i<summonQ.Count;i++)if(summonQ[i].username==command.username)mine++;
        if(limitPerPlayer>0 && mine>=limitPerPlayer || limitTotal>0 && summonQ.Count-indexQ>=limitTotal)return; // full: turned away
        summonQ.Add(command);
    }
    // whose turn it is (moved to summonQ[indexQ]), or null
    public SummonCommand Next(){
        if(summonQ.Count<=indexQ)return null;
        int best = Fewest(summonQ, indexQ, Turns);
        var c = summonQ[best];
        summonQ.RemoveAt(best);
        summonQ.Insert(indexQ,c);
        return c;
    }
    // Next, and it's done: counts a turn
    public SummonCommand Take(){
        var c = Next();
        if(c==null)return null;
        indexQ++;
        turns[c.username] = Turns(c)+1;
        return c;
    }
    int Turns(SummonCommand c)=>turns.TryGetValue(c.username,out int n)?n:0;
    static int Fewest(List<SummonCommand> q, int from, System.Func<SummonCommand,int> turns){ // the first with the fewest turns
        int best = from;
        for(int i=from+1;i<q.Count;i++)if(turns(q[i])<turns(q[best]))best = i;
        return best;
    }
    // the next n, in order, without taking them (for the UI)
    public List<SummonCommand> Preview(int n){
        var wait = summonQ.GetRange(indexQ, summonQ.Count-indexQ);
        var more = new Dictionary<string,int>(); // turns they get on the way
        int Score(SummonCommand c)=>Turns(c)+(more.TryGetValue(c.username, out int m) ? m : 0);
        var next = new List<SummonCommand>();
        while(next.Count<n && wait.Count>0){
            var c = wait[Fewest(wait, 0, Score)];
            next.Add(c); wait.Remove(c);
            more.TryGetValue(c.username, out int had); more[c.username] = had+1;
        }
        return next;
    }
}

[System.Serializable]
public class SimpleSummonDB : ChatLine{
    public List<Enemy> summonableEnemy;
    public List<EnemySO> unlocked=new();
    public ChatPotion potion=new();
    public Berserk berserk = new();
    public Invincible invincible = new();
    public MagicShield magicShield = new();
    public PhysicalShield physicalShield = new();
    public Floating floating = new();
    public Earthquake earthquake = new();
    public ChatSkill[] skills => new ChatSkill[]{berserk, invincible, magicShield, physicalShield, floating, earthquake}; // keys 1-6

    public Enemy GetToSpawn(){
        if(!spawnSkill.isReady)return null;
        var output = _Pop();
        if(output!=null)spawnSkill.Call();
        return output;
    }
    public void NewRoom(){Reset();potion.Reset();}
    public void AddPotion(string username, string name)=>potion.Add(this,username,name);
    public Loot GetPotionToSpawn()=>potion.Pop(this); // before GetToSpawn
    public Enemy _Pop(){
        var c = Take();
        var e = c==null?null:getEnemy(c.spawnableName);
        if(e)ChatFeed.Add(c.username, "summoned "+c.spawnableName.ToLowerInvariant());
        return e;
    }
    public void Unlock(EnemySO e){if(!unlocked.Contains(e))unlocked.Add(e);}
    public Enemy getEnemy(string enemyName){
        string n = enemyName.ToLowerInvariant().Trim();
        foreach(Enemy e in summonableEnemy)if(unlocked.Contains(e.enemyData) && e.enemyData.enemyName.ToLowerInvariant().Trim()==n)return e;
        return null;
    }
}
