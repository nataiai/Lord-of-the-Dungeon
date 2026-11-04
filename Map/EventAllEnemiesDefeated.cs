using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventAllEnemiesDefeated : MonoBehaviour
{
    public UnityEvent onAllEnemiesDefeated;
    List<Enemy> allEnemies;

    private void Start()
    {
        var instances = GameObject.FindObjectsOfType<Enemy>();
        allEnemies = new List<Enemy>(instances);
        // print(allEnemies.Count); it bug (partly) because I yeet the first unique enemy that diead instead of destroying it
        // for spawning skill
    }

    void Update()
    {
        if (IsConditionTrue())
        {
            onAllEnemiesDefeated?.Invoke();
            print("ALL ENEMIES DEFEATED!");
            Destroy(gameObject);
        }
    }

    bool IsConditionTrue()
    {
        foreach(var e in allEnemies){
            if(e!=null && e.isActiveAndEnabled)return false;
        }
        if (true){ // this include newly spawned enemies too
            var instances = GameObject.FindObjectsOfType<Enemy>();
            allEnemies = new List<Enemy>(instances);
            if(allEnemies.Count>0)return false;}
        return true;
        // if (allEnemies.Count == 0)
        //     return true;
        // else return false;
    }

}
