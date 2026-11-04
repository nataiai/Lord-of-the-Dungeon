using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// skill use enum ChatType in Chat.cs

// this is mainly for cooldown
[System.Serializable]
public class Skill 
{
    public ChatType chatType;
    public float cooldown;
    public bool isReady=>lastUse+cooldown<Time.time;
    public float getReloadPercent()=>Mathf.Min((Time.time-lastUse)/cooldown,1);
    float lastUse;
    public virtual bool Call(){
        if(!isReady)return false;
        lastUse = Time.time;
        return true;
    }
}