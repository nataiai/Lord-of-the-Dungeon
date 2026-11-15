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

[System.Serializable]
public class SimpleSummonDB{
    public List<Enemy> summonableEnemy;
    public int limitPerPlayer = 10;
    public Skill spawnSkill;
    public List<SummonCommand> summonQ=new();
    public List<EnemySO> unlocked=new();
    [PlayModeOnly] public int indexQ=0;
    public List<Loot> potions=new();
    public int potionsPerRoom = 3;
    public Skill potionSkill = new(){chatType = ChatType.potion, cooldown = 10};
    public List<SummonCommand> potionQ=new();
    public List<ChatSkill> skills = ChatSkill.Defaults();
    Dictionary<string,int> turns=new(); // per chatter: the fewest go first

    public Enemy GetToSpawn(){
        if(!spawnSkill.isReady)return null;
        var output = _Pop();
        if(output!=null)spawnSkill.Call();
        return output;
    }
    public void Reset(){summonQ.Clear();indexQ=0;}
    public void NewRoom(){Reset();potionQ.Clear();}
    public void Add(string name,SummonCommand command){
        if(summonQ.Count>300)Reset();
        summonQ.Add(command);
    }
    public Enemy _Pop()=>summonQ.Count>indexQ?getEnemy(Count(Next(summonQ,indexQ++)).spawnableName):null;
    // moves the next request to q[from]: the chatter with the fewest turns, then the oldest
    public SummonCommand Next(List<SummonCommand> q, int from){
        int best = from;
        for(int i=from+1;i<q.Count;i++)if(Turns(q[i])<Turns(q[best]))best = i;
        var c = q[best];
        q.RemoveAt(best);
        q.Insert(from,c);
        return c;
    }
    int Turns(SummonCommand c)=>turns.TryGetValue(c.username,out int n)?n:0;
    public SummonCommand Count(SummonCommand c){turns[c.username] = Turns(c)+1; return c;}
    public void Unlock(EnemySO e){if(!unlocked.Contains(e))unlocked.Add(e);}
    public Enemy getEnemy(string enemyName){
        string n = enemyName.ToLowerInvariant().Trim();
        foreach(Enemy e in summonableEnemy)if(unlocked.Contains(e.enemyData) && e.enemyData.enemyName.ToLowerInvariant().Trim()==n)return e;
        return null;
    }
}
