using UnityEngine;

// Dropped loot (enemy, chest, crate, swapped-out weapon) slides away, slows down and bounces off solid things (AI/Slide.cs).
// Loot.Drop and Loot's swap start it; loot placed in the level stays put.
public class LootYoink : MonoBehaviour
{
    public static bool use = true;          // ON/OFF: false = dropped loot stays where it falls
    public static float speed = 4;          // speed when dropped
    public static float deceleration = 8;   // speed lost per second
    public static float bounciness = 0.6f;  // speed kept per bounce
    Vector2 velocity;
    Rigidbody2D rb;
    float radius;

    public static void Push(GameObject loot, Vector2 dir=default){ // dir zero = a random way
        if(!use || loot==null)return;
        if(dir.sqrMagnitude<1e-6f)dir = Random.insideUnitCircle;
        if(dir.sqrMagnitude<1e-6f)dir = Vector2.right;
        if(!loot.TryGetComponent(out LootYoink y))y = loot.AddComponent<LootYoink>();
        y.velocity = dir.normalized*speed;
    }
    void Start(){rb = Slide.Body(gameObject); radius = Slide.Radius(gameObject);}
    void FixedUpdate(){
        if(rb==null)return;
        if(velocity==Vector2.zero){Destroy(this); return;} // stopped
        Slide.Step(rb, ref velocity, radius, deceleration, bounciness, true);
    }
}
