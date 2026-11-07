using UnityEngine;
using UnityEngine.Events;

// On a locked door or chest. A key (Weapon.isKey) opens it: swing the key at it up close, or throw the key into it.
//   door:  put the closed door object in hideOnUnlock (it can be this object)
//   chest: a locked Chest ignores hits; once unlocked, the key's hit also opens it
public class Lock : MonoBehaviour
{
    public bool isLocked = true;
    [Tooltip("Empty = any key opens it. Otherwise only keys with the same keyId")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey = true;
    [Tooltip("Hidden when unlocked, e.g. the closed door")]
    public GameObject[] hideOnUnlock;
    public AudioClip unlockSound;
    public UnityEvent onUnlock;

    public bool Fits(Weapon key) => isLocked && key!=null && key.isKey && (string.IsNullOrEmpty(keyId) || keyId==key.keyId);
    public void Unlock(){
        isLocked = false;
        if(unlockSound!=null)SoundPlayer.PlaySound(unlockSound);
        foreach(var go in hideOnUnlock)if(go!=null)go.SetActive(false);
        onUnlock?.Invoke();
        PathFinding.Invalidate(); // a door may be gone
    }
}
