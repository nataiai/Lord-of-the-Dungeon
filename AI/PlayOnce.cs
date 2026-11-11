using System.Collections;
using UnityEngine;

// Play-once animations, for anyone: wait for an Animator's animation, then the object is gone.
//     PlayOnce.ThenHide(this, anim);                        // a door: plays its open animation, then hides
//     PlayOnce.Spawn(effectPrefab, pos, parent);            // an effect: plays once, then destroys itself
// Length = the longest clip in the Animator, so it works whether the clip loops or not.
public static class PlayOnce
{
    // how long its animation lasts (0: no Animator or no clip)
    public static float Length(Animator anim){
        if(anim==null || anim.runtimeAnimatorController==null)return 0;
        float length = 0;
        foreach(var clip in anim.runtimeAnimatorController.animationClips)length = Mathf.Max(length, clip.length);
        return length;
    }
    // after the animation, host's object is hidden (SetActive false)
    public static void ThenHide(MonoBehaviour host, Animator anim) => host.StartCoroutine(Hide(host.gameObject, Length(anim)));
    // after the animation, `go` is destroyed
    public static void ThenDestroy(GameObject go, Animator anim, float fallback=0) => Object.Destroy(go, Or(Length(anim), fallback));
    // makes `prefab` (an effect), plays its animation once, then destroys it. fallback: seconds if it has no animation
    public static GameObject Spawn(GameObject prefab, Vector3 pos, Transform parent=null, float fallback=.5f){
        var go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
        ThenDestroy(go, go.GetComponentInChildren<Animator>(), fallback);
        return go;
    }

    static float Or(float length, float fallback) => length>0 ? length : fallback;
    static IEnumerator Hide(GameObject go, float seconds){
        yield return new WaitForSeconds(seconds);
        go.SetActive(false);
    }
}
