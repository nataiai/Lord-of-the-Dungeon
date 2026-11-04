using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill // a chat skill's cooldown (ChatType is in Chat.cs)
{
    public ChatType chatType;
    public float cooldown;
    float lastUse;
    public bool isReady=>lastUse+cooldown<Time.time;
    public float precReady=>Mathf.Min((Time.time-lastUse)/cooldown,1);
    public float getReloadPercent()=>precReady;
    void Start(){lastUse = Time.time;} // never called: Skill is not a MonoBehaviour
    public virtual bool Call(){
        if(!isReady)return false;
        lastUse = Time.time;
        return true;
    }
}
