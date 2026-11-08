using System.Collections;
using UnityEngine;

// The player, enemies and loot that touch this trigger fall in: they play their fall clip (or shrink away), then enemies and loot are gone
// and each player comes back where it entered the room. Flying enemies (ignoreCollide, ghosts) pass over.
[RequireComponent(typeof(Collider2D))]
public class Hole : MonoBehaviour
{
    const float fallShrinkTime = .5f; // when it has no fall clip

    void Awake(){ // loot has no Rigidbody2D: a kinematic one here lets it touch the hole too
        if(!TryGetComponent(out Rigidbody2D rb))rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }
    void OnTriggerEnter2D(Collider2D other){
        Behaviour faller = other.TryGetComponent(out Character c) ? c : (Behaviour)other.GetComponent<Loot>();
        if(faller==null || !faller.enabled || c is Enemy e && e.enemyData.ignoreCollide)return; // not a faller, already falling, or flies
        StartCoroutine(Fall(faller, other));
    }
    IEnumerator Fall(Behaviour faller, Collider2D col){
        faller.enabled = col.enabled = false; // no moving, hitting or picking up while falling
        if(faller.TryGetComponent(out Rigidbody2D rb))rb.velocity = Vector2.zero;
        Transform t = faller.transform;
        Vector3 scale = t.localScale;
        var clip = faller switch{Player p => p.fallClip, Enemy e => e.enemyData.fallClip, Loot l => l.fallClip, _ => null};
        var animator = faller.GetComponentInChildren<Animator>();
        if(clip!=null && animator!=null){animator.speed = 1; animator.Play(clip.name, 0, 0f); yield return new WaitForSeconds(clip.length);} // fall down animation
        else for(float time=0; time<fallShrinkTime; time+=Time.deltaTime){t.localScale = scale*(1-time/fallShrinkTime); yield return null;} // no clip: shrink away
        if(faller is not Player player){Destroy(t.gameObject); yield break;}
        t.localScale = scale;
        player.Respawn();
        faller.enabled = col.enabled = true;
    }
}
