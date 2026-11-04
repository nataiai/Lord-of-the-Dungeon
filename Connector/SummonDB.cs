using System.Collections.Generic;

[System.Serializable]
public class SummonCommand{
    public string username;
    public string spawnableName;
    public string spawnableLocation;
    // public SummonCommand(string spawnableName) : this(spawnableName,"",""){}
    // public SummonCommand(string spawnableName, string username) : this(spawnableName,username,""){}
    public SummonCommand(string spawnableName, string username="", string spawnableLocation=""){
        this.username = username;
        this.spawnableName = spawnableName;
        this.spawnableLocation = spawnableLocation;
    }
}
[System.Serializable]
public class SimpleSummonDB{
    public List<Enemy> summonableEnemy;
    public int limitPerPlayer = 10;
    public Skill spawnSkill;
    public List<SummonCommand> summonQ=new();
    int indexQ=0;
    
    public Enemy GetToSpawn(){
        if(spawnSkill.isReady){
            var output = _Pop();
            if(output!=null)
                spawnSkill.Call();
            return output;
        }
        return null;
    }

    public void Reset(){
        summonQ.Clear();
        indexQ=0;
    }
    public void Add(string name,SummonCommand command){
        if(summonQ.Count>300)Reset();
        summonQ.Add(command);
    }
    public Enemy _Pop(){
        if(summonQ.Count>indexQ)return getEnemy(summonQ[indexQ++].spawnableName);
        return null;
    }

    public Enemy getEnemy(string enemyName){
        foreach(Enemy e in summonableEnemy){
            if(e.enemyData.enemyName.ToLowerInvariant().Trim() == (enemyName.ToLowerInvariant().Trim()))return e;
        }
        return null;
    }
}
// [System.Serializable]
// public class SummonDB:SimpleSummonDB{
//     // Dictionary<string,Enemy> summonableEnemyDict = new();
//     public List<string> userQ = new();
//     public Dictionary<string,SummonCommand> userDict = new();
//     public List<SummonCommand> testSummon = new();
//     public int indexTest;
//     public bool isCommandEmpty=>indexQ>=userQ.Count;
//     public void Reset(){
//         userQ.Clear();
//         userDict.Clear();
//         indexQ=0;
//     }
//     public void Add(string name,SummonCommand command){
//         testSummon.Add(command);
//         if(!userDict.ContainsKey(name)) userQ.Add(name);
//         userDict[name] = command;
//     }
//     // public Enemy _popTest()=>testSummon.Count>indexTest?getEnemy(testSummon[indexTest++].spawnableName):null;
//     public Enemy _popTest(){
//         if(testSummon.Count>indexTest)return getEnemy(testSummon[indexTest++].spawnableName);else return null;
//     }
//     public SummonCommand Pop(){
//         if(isCommandEmpty)return null;
//         return userDict[userQ[indexQ++]];
//     }
// }