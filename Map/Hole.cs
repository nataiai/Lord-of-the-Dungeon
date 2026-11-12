using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hole : MonoBehaviour
{
    const float fallShrinkTime = .5f; // when it has no fall clip

    void Awake(){
        if(!TryGetComponent(out Rigidbody2D rb))rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }
    void OnTriggerEnter2D(Collider2D other){
        Behaviour faller = other.TryGetComponent(out Character c) ? c : (Behaviour)other.GetComponent<Loot>();
        if(faller==null || !faller.enabled || c is Enemy e && e.enemyData.ignoreCollide)return;
        StartCoroutine(Fall(faller, other));
    }
    IEnumerator Fall(Behaviour faller, Collider2D col){
        faller.enabled = col.enabled = false;
        if(faller.TryGetComponent(out Rigidbody2D rb))rb.velocity = Vector2.zero;
        Transform t = faller.transform;
        Vector3 scale = t.localScale;
        var clip = faller switch{Player p => p.fallClip, Enemy e => e.enemyData.fallClip, Loot l => l.fallClip, _ => null};
        var animator = faller.GetComponentInChildren<Animator>();
        if(clip!=null && animator!=null){animator.speed = 1; animator.Play(clip.name, 0, 0f); yield return new WaitForSeconds(clip.length);}
        else for(float time=0; time<fallShrinkTime; time+=Time.deltaTime){t.localScale = scale*(1-time/fallShrinkTime); yield return null;} // no clip: shrink away
        if(faller is not Player player){Destroy(t.gameObject); yield break;}
        t.localScale = scale;
        player.Respawn();
        faller.enabled = col.enabled = true;
    }
}
