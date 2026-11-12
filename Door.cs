using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Lock
{
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider2D coll;

    public override void Unlock(){base.Unlock(); OpenDoor();}

    public void OpenDoor()
    {
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_door_open);
        anim.enabled = true;
        coll.enabled = false;
        PlayOnce.ThenHide(this, anim);
    }
}
