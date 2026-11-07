using UnityEngine;
using UnityEngine.Events;

public class EventAllEnemiesDefeated : MonoBehaviour
{
    public UnityEvent onAllEnemiesDefeated;
    Enemy alive; // checked first, so the scene is searched again only when it is gone


    void Update()
    {
        if (IsConditionTrue())
        {
            onAllEnemiesDefeated?.Invoke();
            PathFinding.Invalidate(); // doors opened
            print("ALL ENEMIES DEFEATED!");
            SoundPlayer.PlaySound(SoundPlayer.instance.sd_door_open);
            Destroy(gameObject);
        }
    }

    bool IsConditionTrue()
    {
        if(alive!=null && alive.isActiveAndEnabled)return false;
        alive = FindAnyObjectByType<Enemy>(); // also finds newly summoned enemies
        if(alive!=null)return false;
        return true;
    }
}
