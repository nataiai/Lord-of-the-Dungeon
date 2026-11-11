using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Lock // keyId (locked), consumeKey: AI/Lock.cs
{
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider2D coll;

    public override void Unlock(){base.Unlock(); OpenDoor();} // a key opened it

    public void OpenDoor()
    {
        SoundPlayer.PlaySound(SoundPlayer.instance.sd_door_open);
        anim.enabled = true;
        coll.enabled = false;
        StartCoroutine(PlayOnceThenGone());
    }
    IEnumerator PlayOnceThenGone() // the open animation plays once, then the door is gone
    {
        float length = 0;
        if(anim!=null && anim.runtimeAnimatorController!=null)
            foreach(var clip in anim.runtimeAnimatorController.animationClips)length = Mathf.Max(length, clip.length);
        yield return new WaitForSeconds(length);
        gameObject.SetActive(false);
    }
}
