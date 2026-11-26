using UnityEngine;

// Something a key opens: swing a key weapon (one with a keyId) at it, or throw it in (AI/ThrownWeapon.cs). ON/OFF: Experimental.locks
// Locked = keyId is not empty. Door is a Lock (opens with OpenDoor). Chest is an ILock: locked, it ignores hits until its key opens it.
public interface ILock{ // anything a key can open (Lock, Door, Chest)
    bool Fits(Weapon key);
    void Unlock();
    bool ConsumesKey{get;}
}

public class Lock : MonoBehaviour, ILock
{
    [Tooltip("Not empty: locked, opens with the key weapon of the same keyId")]
    public string keyId;
    [Tooltip("The key is used up when it opens this")]
    public bool consumeKey;
    static readonly Collider2D[] touching = new Collider2D[8];

    protected virtual void Start(){if(!Experimental.I.locks && IsLocked(keyId))Unlock();} // off in Experimental: no keys needed

    public bool Fits(Weapon key) => KeyFits(keyId, key);
    public bool ConsumesKey => consumeKey;
    public static bool IsLocked(string keyId) => !string.IsNullOrEmpty(keyId);
    public static bool KeyFits(string keyId, Weapon key) => IsLocked(keyId) && key!=null && key.keyId==keyId;
    public virtual void Unlock(){ // Door: also opens
        keyId = ""; // open now
        PathFinding.Invalidate(); // a door may be open now
    }

    // opens the Lock on `other` if this key fits. usedUp: the key is gone
    public static bool TryOpen(Collider2D other, Weapon key, out bool usedUp){
        usedUp = false;
        var lk = other.GetComponentInParent<ILock>();
        if(lk==null || !lk.Fits(key))return false;
        lk.Unlock();
        usedUp = lk.ConsumesKey;
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
