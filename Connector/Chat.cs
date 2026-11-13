using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChatType{summon=0,teleport=1,vote,work,invalid,potion} // new ones at the end (saved as numbers)

public abstract class Chat : MonoBehaviour
{
    public SimpleSummonDB summondb;
    static (string word, int minWords, int maxWords, ChatType type)[] commands = {
        ("summon",2,3,ChatType.summon), // summon enemy (north)
        ("spawn",2,3,ChatType.summon),
        ("teleport",3,3,ChatType.teleport), // teleport x y
        ("vote",2,2,ChatType.vote), // vote leftdoor
        ("work",2,2,ChatType.work), // work smith
        ("potion",1,3,ChatType.potion), // potion (healthbone)
    };

    public ChatType Categorize(string message){
        string[] words = message.ToLowerInvariant().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        foreach(var c in commands)
            if(words.Length>=c.minWords && words.Length<=c.maxWords && c.word.StartsWith(words[0], System.StringComparison.Ordinal))return c.type;
        return ChatType.invalid;
    }
    public void addToDB(string username,string msg){
        string[] words = msg.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        switch(Categorize(msg)){
            case ChatType.summon: summondb.Add(username,new SummonCommand(words[1],username)); break;
            case ChatType.teleport: if(int.TryParse(words[1],out int x) && int.TryParse(words[2],out int y))GameManager.Instance.TeleportSkill(x,y); break;
            case ChatType.potion: summondb.AddPotion(username,words.Length>1?words[1]:""); break;
        }
    }
}
