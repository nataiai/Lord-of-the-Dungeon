using UnityEngine;
using UnityEngine.Events;

public class EventAllEnemiesDefeated : MonoBehaviour
{
    public UnityEvent onAllEnemiesDefeated;
    Enemy alive; // checked first, so the scene is searched again only when it is gone

    void Update(){
        if(alive!=null && alive.isActiveAndEnabled)return;
        alive = FindAnyObjectByType<Enemy>(); // also finds newly summoned enemies
        if(alive!=null)return;
        onAllEnemiesDefeated?.Invoke();
        print("ALL ENEMIES DEFEATED!");
        Destroy(gameObject);
    }
}
