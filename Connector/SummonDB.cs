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
public class SimpleSummonDB{ // chat's summon queue; spawnSkill is one cooldown for all of chat
    public List<Enemy> summonableEnemy;
    public int limitPerPlayer = 10;
    public Skill spawnSkill;
    public List<SummonCommand> summonQ=new();
    public List<EnemySO> unlocked=new(); // enemy types the streamer has killed; only these can be summoned
    int indexQ=0;

    public Enemy GetToSpawn(){ // the next enemy, or null; unknown or locked names are skipped without using the cooldown
        if(!spawnSkill.isReady)return null;
        var output = _Pop();
        if(output!=null)spawnSkill.Call();
        return output;
    }
    public void Reset(){summonQ.Clear();indexQ=0;}
    public void Add(string name,SummonCommand command){
        if(summonQ.Count>300)Reset();
        summonQ.Add(command);
    }
    public Enemy _Pop()=>summonQ.Count>indexQ?getEnemy(summonQ[indexQ++].spawnableName):null;
    public void Unlock(EnemySO e){if(!unlocked.Contains(e))unlocked.Add(e);}
    public Enemy getEnemy(string enemyName){
        string n = enemyName.ToLowerInvariant().Trim();
        foreach(Enemy e in summonableEnemy)if(unlocked.Contains(e.enemyData) && e.enemyData.enemyName.ToLowerInvariant().Trim()==n)return e;
        return null;
    }
}
