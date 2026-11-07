using System;
using UnityEngine;

// Moves something that slides: one physics step at a time, slowing down, bouncing off (or stopping at) solid things.
// Sweeps a circle ahead, so it never pushes characters and needs no layers. Used by Loot (dropped and thrown); reuse it in FixedUpdate:
//     Slide.Step(rb, ref velocity, radius, deceleration, bounciness, bounce);
public static class Slide
{
    public static readonly ContactFilter2D solid = new ContactFilter2D(); // default: triggers are ignored
    static readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    // solid = blocks a slide: anything not a trigger and not a character (chests count as solid)
    public static bool IsSolid(Collider2D col){var ch = col.GetComponentInParent<Character>(); return ch==null || ch is Chest;}

    // onTouch: called for each collider in the way, characters too (return false = stop right there)
    public static void Step(Rigidbody2D rb, ref Vector2 velocity, float radius, float deceleration, float bounciness, bool bounce, Func<Collider2D,bool> onTouch=null){
        if(velocity==Vector2.zero)return;
        float dt = Time.fixedDeltaTime;
        Vector2 pos = rb.position, move = velocity*dt;
        bool blocked = false;
        int n = Physics2D.CircleCast(pos, radius, move.normalized, solid, hits, move.magnitude);
        for(int i=0;i<n && !blocked;i++){
            Collider2D col = hits[i].collider;
            bool keepGoing = onTouch==null || onTouch(col);
            if(keepGoing && !IsSolid(col))continue; // characters don't block
            pos = hits[i].centroid;
            blocked = true;
            velocity = keepGoing && bounce ? Vector2.Reflect(velocity, hits[i].normal)*bounciness : Vector2.zero;
        }
        if(!blocked)pos += move;
        rb.MovePosition(pos);
        velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration*dt);
    }

    // flies straight at `target` through walls, speeding up by `acceleration` per second (0 = at maxSpeed at once). Returns the distance left
    public static float Toward(Rigidbody2D rb, ref Vector2 velocity, Vector2 target, float acceleration, float maxSpeed){
        Vector2 to = target - rb.position;
        float speed = acceleration>0 ? Mathf.Min(velocity.magnitude + acceleration*Time.fixedDeltaTime, maxSpeed) : maxSpeed;
        velocity = to.normalized*speed;
        rb.MovePosition(rb.position + Vector2.ClampMagnitude(velocity*Time.fixedDeltaTime, to.magnitude));
        return to.magnitude;
    }

    // turns it while it flies: spinSpeed degrees per second at fullSpeed (slower as it slows), or 0 = points the way it flies
    // spriteAngle: which way the art points (0 = right, 90 = up)
    public static void Turn(Rigidbody2D rb, Vector2 velocity, float spinSpeed, float fullSpeed, float spriteAngle){
        if(spinSpeed!=0)rb.MoveRotation(rb.rotation + spinSpeed*Time.fixedDeltaTime*Mathf.Clamp01(velocity.magnitude/Mathf.Max(fullSpeed,0.01f)));
        else if(velocity.sqrMagnitude>1e-6f)rb.MoveRotation(PointAngle(velocity, spriteAngle));
    }
    public static float PointAngle(Vector2 dir, float spriteAngle) => Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg - spriteAngle;
}
