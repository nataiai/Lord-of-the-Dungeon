using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChatType{
    summon=0,
    teleport=1,
    vote,
    work,
    invalid
}

[System.Serializable]
public class ChatMsg{
    public float time;
    public string name;
    public string message;
}

public abstract class Chat : MonoBehaviour
{
    public SimpleSummonDB summondb;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public ChatType Categorize(string message){
        string msg = message.ToLowerInvariant();
        string[] words = msg.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        int len = words.Length;
        int w0len = words[0].Length;
        switch(words[0][0]){
            case 's': //s(ummon) enemy (north)
            if(len>=2 && len<=3 && w0len<=6 && words[0] == "summon"[..w0len])return ChatType.summon;
            if(len>=2 && len<=3 && w0len<=5 && words[0] == "spawn"[..w0len])return ChatType.summon;
            break;
            case 't': //t(eleport) x y
            if(len==3 && w0len<=8 && words[0] == "teleport"[..w0len])return ChatType.teleport;
            break;
            case 'v': //v(ote) leftdoor
            if(len==2 && w0len<=4 && words[0] == "vote"[..w0len]) return ChatType.vote;
            break;
            case 'w': //w(ork) smith
            if(len==2 && w0len<=4 && words[0] == "work"[..w0len]) return ChatType.work;
            break;
        }
        return ChatType.invalid;
    }

    public void addToDB(string username,string msg){
        string[] words = msg.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        int len = words.Length;
        ChatType skill = Categorize(msg);
        // if(!GameManager.Instance.availableSkill.Contains(skill))return; // who care
        switch(skill){
            case ChatType.summon:
            // string location = len==3?words[2]:"";
            // summondb.Add(username,new SummonCommand(words[1],location));
            summondb.Add(username,new SummonCommand(words[1],username));
            break;
            case ChatType.teleport:
            if(int.TryParse(words[1],out int x) && int.TryParse(words[2],out int y))GameManager.Instance.TeleportSkill(x,y);
            break;
        }
    }

}
