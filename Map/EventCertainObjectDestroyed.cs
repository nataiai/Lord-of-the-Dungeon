using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventCertainObjectDestroyed : MonoBehaviour
{
    public Loot sword;
    public UnityEvent onCollectedSword;

    void Update()
    {
        if (IsConditionTrue())
        {
            onCollectedSword?.Invoke();
            PathFinding.Invalidate();
            print("SWORD COLLECTED");
            Destroy(gameObject);
        }
    }

    bool IsConditionTrue()
    {
        if (sword == null) return true;
        else return false;
    }

}
