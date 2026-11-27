using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chat skills (Connector/ChatSkill.cs), what the game doesn't need: how they look, and test keys 1-6
public static class ChatSkillExtras{
    static SpriteRenderer Body(Player p)=>System.Array.Find(p.GetComponentsInChildren<SpriteRenderer>(), s=>s.sprite); // the sprite you see
    static void Copy(SpriteRenderer copy, SpriteRenderer of, Vector3 offset){ // it walks too
        copy.transform.position = of.transform.position+offset; copy.transform.localScale = of.transform.localScale;
        copy.sprite = of.sprite; copy.flipX = of.flipX;
    }
    // GameManager.Update
    public static void Keys(ChatSkill[] skills){
        var keys = UnityEngine.InputSystem.Keyboard.current;
        if(!Experimental.I.chatSkills || !Experimental.I.skillKeys || keys==null)return;
        for(int i=0;i<skills.Length && i<9;i++)
            if(keys[UnityEngine.InputSystem.Key.Digit1+i].wasPressedThisFrame)ChatSkill.Vote(skills, "keys", skills[i].chatType, "key"); // as a chatter would
    }

    // Berserk: a red copy on top, for now (the Animator keeps the sprite's own color)
    public static void Red(Player p){var sr = Body(p); if(sr)p.StartCoroutine(Red(sr, p));}
    static IEnumerator Red(SpriteRenderer sr, Player p){
        var red = ThrownWeapon.Shadow(sr, sr.transform.parent);
        red.name = "berserk"; red.color = new Color(1,0,0,.5f); red.sortingOrder = sr.sortingOrder+1;
        while(p.Has(BuffType.berserk)){Copy(red, sr, Vector3.zero); yield return null;}
        Object.Destroy(red.gameObject);
    }

    static bool up; static float ends; // floating: lifted now; when it ends
    // Floating: the player floats up, a shadow stays below (like a thrown weapon's)
    public static void Lift(Floating f, Player p){
        ends = Time.time+f.buff.duration;
        if(!up)p.StartCoroutine(Lifting(f, p));
    }
    static IEnumerator Lifting(Floating f, Player p){
        var sr = p.GetComponentInChildren<SpriteRenderer>(); // its sorting layer, like a thrown weapon's shadow
        if(sr==null)yield break;
        var body = Body(p) ?? sr;
        up = true;
        var shadow = ThrownWeapon.Shadow(sr, body.transform.parent);
        float rise = Mathf.Max(f.riseTime,.01f), t = 0, h = 0, left = 1; // t: 0 = on the ground, 1 = up
        while(left>0){
            left = p.Has(BuffType.floating) ? ends-Time.time : 0;
            t = Mathf.Min(Mathf.MoveTowards(t, 1, Time.deltaTime/rise), left/rise); // up at the start, lands as it ends
            float was = h;
            h = f.height*Mathf.SmoothStep(0, 1, t);
            p.rb.position += Vector2.up*(h-was); // the player, not its sprite: the Animator moves that
            Copy(shadow, body, Vector3.down*h);
            yield return null;
        }
        Object.Destroy(shadow.gameObject);
        up = false;
    }
}
