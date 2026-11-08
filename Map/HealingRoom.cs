using UnityEngine;

// Standing in this trigger heals the player HPHealPerSecond times a second (1 HP each), with a sound and the heal animation
public class HealingRoom : MonoBehaviour
{
    public float HPHealPerSecond = 3f;
    [SerializeField] AudioClip heal_sound;
    Player player; // the player while inside, else null
    float nextHeal;

    void OnTriggerEnter2D(Collider2D other){
        if(!other.TryGetComponent(out player))return;
        player.isHealing = true;
        nextHeal = Time.time + 1/HPHealPerSecond;
    }
    void OnTriggerExit2D(Collider2D other){
        if(!other.TryGetComponent(out Player p))return;
        p.isHealing = false;
        player = null;
    }
    void Update(){
        if(player==null || Time.time<nextHeal)return;
        nextHeal = Time.time + 1/HPHealPerSecond;
        if(player.HP>=player.maxHP)return; // full: no sound or animation
        player.HealWithEffect(1);
        SoundPlayer.PlaySound(heal_sound);
    }
}
