using UnityEngine;
using UnityEngine.Events;

public class EventCertainObjectDestroyed : MonoBehaviour
{
    public Loot sword;
    public UnityEvent onCollectedSword;

    void Update(){
        if(sword!=null)return;
        onCollectedSword?.Invoke();
        print("SWORD COLLECTED");
        Destroy(gameObject);
    }
}
