using System.Collections;
using UnityEngine;

// The player, enemies and loot that touch this trigger fall in: they shrink away, then enemies and loot are gone
// and the player comes back where he entered the room. Flying enemies (ignoreCollide, ghosts) pass over.
[RequireComponent(typeof(Collider2D))]
public class Hole : MonoBehaviour
{
    const float fallTime = .5f;

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
        for(float time=0; time<fallTime; time+=Time.deltaTime){t.localScale = scale*(1-time/fallTime); yield return null;} // fall down animation
        if(faller is not Player){Destroy(t.gameObject); yield break;}
        t.localScale = scale;
        t.position = GameManager.Instance.map.roomEntryPos;
        faller.enabled = col.enabled = true;
    }
}
