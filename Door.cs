using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider2D coll;
    public bool hasLock;
    [Tooltip("With hasLock: a key weapon with this keyId opens it, swung or thrown (AI/Lock.cs). Empty = any key")]
    public string keyId;
    [Tooltip("The key is used up when it opens this door")]
    public bool consumeKey = true;

    public void OpenDoor()
    {
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_door_open);
        anim.enabled = true;
        coll.enabled = false;
    }
}
