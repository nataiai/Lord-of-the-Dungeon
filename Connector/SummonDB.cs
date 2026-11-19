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

    public void Reset(){summonQ.Clear();indexQ=0;}
    public void Add(string name,SummonCommand command){
        if(summonQ.Count>300)Reset();
        summonQ.Add(command);
    }
    // whose turn it is (moved to summonQ[indexQ]), or null
    public SummonCommand Next(){
        if(summonQ.Count<=indexQ)return null;
        int best = indexQ;
        for(int i=indexQ+1;i<summonQ.Count;i++)if(Turns(summonQ[i])<Turns(summonQ[best]))best = i;
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
}

[System.Serializable]
public class SimpleSummonDB : ChatLine{
    public List<Enemy> summonableEnemy;
    public int limitPerPlayer = 10;
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
        return c==null?null:getEnemy(c.spawnableName);
    }
    public void Unlock(EnemySO e){if(!unlocked.Contains(e))unlocked.Add(e);}
    public Enemy getEnemy(string enemyName){
        string n = enemyName.ToLowerInvariant().Trim();
        foreach(Enemy e in summonableEnemy)if(unlocked.Contains(e.enemyData) && e.enemyData.enemyName.ToLowerInvariant().Trim()==n)return e;
        return null;
    }
}
