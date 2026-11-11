using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Door : MonoBehaviour // keyId: AI/Fields.cs
{
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider2D coll;
    public bool hasLock;

    public void OpenDoor()
    {
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_door_open);
        anim.enabled = true;
        coll.enabled = false;
    }
}
