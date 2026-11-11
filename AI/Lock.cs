using UnityEngine;

// Something a key opens: swing a key weapon (isKey) at it, or throw it in (AI/ThrownWeapon.cs). ON/OFF: Experimental.locks
// Door is a Lock (opens with OpenDoor). A Lock on a Chest: the chest ignores hits until a key opens it.
public class Lock : MonoBehaviour
{
    public bool hasLock;
    [Tooltip("With hasLock: opens with a key weapon of the same keyId. Empty = any key")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey = true;
    static readonly Collider2D[] touching = new Collider2D[8];

    protected virtual void Start(){if(!Experimental.I.locks && hasLock)Unlock();} // off in Experimental: no keys needed

    public bool Fits(Weapon key) => hasLock && key!=null && key.isKey && (string.IsNullOrEmpty(keyId) || keyId==key.keyId);
    public virtual void Unlock(){ // Door: also opens
        hasLock = false;
        PathFinding.Invalidate(); // a door may be open now
    }

    // opens the Lock on `other` if this key fits. usedUp: the key is gone
    public static bool TryOpen(Collider2D other, Weapon key, out bool usedUp){
        usedUp = false;
        var lk = other.GetComponentInParent<Lock>();
        if(lk==null || !lk.Fits(key))return false;
        lk.Unlock();
        usedUp = lk.consumeKey;
        return true;
    }
    // a key's swing (HoldingWeapons.ForceUse): opens a Lock its hitbox touches. Checked here because a static door sends no trigger event
    public static void Swing(HitBox hitBox, HoldingWeapons hand){
        if(hitBox==null || hitBox.weapon==null || !hitBox.weapon.isKey || !hitBox.TryGetComponent(out Collider2D col))return;
        int n = col.OverlapCollider(new ContactFilter2D().NoFilter(), touching);
        for(int i=0;i<n;i++)
            if(TryOpen(touching[i], hitBox.weapon, out bool usedUp) && usedUp && hand.weapon==hitBox.weapon){
                hand.Unequip(); // the key leaves the hand
                GameManager.Instance.ui.UpdateWeap();
            }
    }
}
