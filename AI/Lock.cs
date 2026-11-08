using UnityEngine;
using UnityEngine.Events;

// On a locked door or chest. A key (a weapon with isKey) opens it: swing the key at it up close, or throw it in (AI/ThrownWeapon.cs).
//   door:  put the closed door object in hideOnUnlock (it can be this object)
//   chest: a locked Chest ignores hits; once unlocked, the key's hit also opens it
// Which key: Lock.keyId must match the key's id (set on the key's Loot, or the weapon's keyId). Empty = any key.
public class Lock : MonoBehaviour
{
    public bool isLocked = true;
    [Tooltip("Empty = any key opens it. Otherwise only keys with the same id")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey = true;
    [Tooltip("Hidden when unlocked, e.g. the closed door")]
    public GameObject[] hideOnUnlock;
    public AudioClip unlockSound;
    public UnityEvent onUnlock;
    static readonly Collider2D[] touching = new Collider2D[8];

    void Start(){if(!Experimental.I.locks && isLocked)Unlock();} // ON/OFF in Experimental: off = no keys needed

    // id: the key's own id (empty = the weapon's keyId)
    public bool Fits(Weapon key, string id){
        if(!isLocked || key==null || !key.isKey)return false;
        string k = string.IsNullOrEmpty(id) ? key.keyId : id;
        return string.IsNullOrEmpty(keyId) || keyId==k;
    }
    public void Unlock(){
        isLocked = false;
        if(unlockSound!=null)SoundPlayer.PlaySound(unlockSound);
        foreach(var go in hideOnUnlock)if(go!=null)go.SetActive(false);
        onUnlock?.Invoke();
        PathFinding.Invalidate(); // a door may be gone
    }

    // opens the Lock on `other` if this key fits. usedUp: the key is gone
    public static bool TryOpen(Collider2D other, Weapon key, string id, out bool usedUp){
        usedUp = false;
        if(!other.TryGetComponent(out Lock lk) || !lk.Fits(key,id))return false;
        lk.Unlock();
        usedUp = lk.consumeKey;
        return true;
    }
    // a key's swing (HoldingWeapons.ForceUse): opens a Lock its hitbox touches. Checked here because a static door sends no trigger event
    public static void Swing(HitBox hitBox, HoldingWeapons hand){
        if(hitBox==null || hitBox.weapon==null || !hitBox.weapon.isKey || !hitBox.TryGetComponent(out Collider2D col))return;
        int n = col.OverlapCollider(new ContactFilter2D().NoFilter(), touching);
        for(int i=0;i<n;i++)
            if(TryOpen(touching[i], hitBox.weapon, hand.keyId, out bool usedUp) && usedUp && hand.weapon==hitBox.weapon){
                hand.weapon = GameManager.Instance.fist; hand.keyId = ""; // the key leaves the hand
                GameManager.Instance.ui.UpdateWeap();
            }
    }
}
