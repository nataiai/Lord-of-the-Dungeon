using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chat skills: chat types a word (3+ letters: "ber" works), its buff goes on the player. Summondb > Skills.
// earthquake / quake: its stun and a push go on every enemy of the room; the player is stunned too,
// unless invincible (buff or the frames after a hit) or floating. Never pushed.
// On/off: Experimental.chatSkills. Experimental.skillKeys: keys 1-6 cast them, for testing.
[System.Serializable]
public class ChatSkill : Skill{
    public BuffEffect.Buff buff = new();
    [Tooltip("Earthquake: how hard enemies are pushed away from the player")]
    public float knockback;
    static string said; static float saidAt;
    public static string Text()=>Time.time<saidAt+4 ? said : ""; // UI.chatText: chat can't be answered, the screen can

    // Chat.addToDB: one chatter casts it, its cooldown limits how often
    public static void Cast(List<ChatSkill> skills, string username, ChatType type, string msg){
        var s = skills.Find(x=>x.chatType==type);
        if(!Experimental.I.chatSkills || s==null || msg.Trim().Length<3 || !s.isReady)return; // "b" or "i" alone is no cast
        var p = GameManager.Instance.player;
        bool onPlayer = true;
        if(type==ChatType.earthquake){
            var enemies = System.Array.FindAll(GameManager.Instance.currentRoom.GetComponentsInChildren<Enemy>(), e=>!(e is Chest));
            if(enemies.Length==0)return; // it would only stun the player
            foreach(var e in enemies){s.buff.CopyTo(e.gameObject); e.receiveKnockback(p.center, s.knockback);}
            onPlayer = !p.isInvincible && !p.Has(BuffType.invincible|BuffType.floating);
        }
        s.Call();
        if(onPlayer)s.buff.CopyTo(p.gameObject);
        if(type==ChatType.floating)p.rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // so it falls when the float ends over a hole
        said = username+": "+type+"!"; saidAt = Time.time;
    }
    // GameManager.Update
    public static void Keys(List<ChatSkill> skills){
        var keys = UnityEngine.InputSystem.Keyboard.current;
        if(!Experimental.I.chatSkills || !Experimental.I.skillKeys || keys==null)return;
        for(int i=0;i<skills.Count && i<9;i++)
            if(keys[UnityEngine.InputSystem.Key.Digit1+i].wasPressedThisFrame)Cast(skills, "keys", skills[i].chatType, "key"); // as a chatter would
    }

    public static List<ChatSkill> Defaults() => new(){
        New(ChatType.berserk, BuffType.berserk, 15), // attack x2 to x4 (more with less HP), no healing
        New(ChatType.invincible, BuffType.invincible, 10), // no damage, no knockback
        New(ChatType.magicShield, BuffType.magicShield, 15),
        New(ChatType.physicalShield, BuffType.physicalShield, 15),
        New(ChatType.floating, BuffType.floating, 10, 30), // holes don't take the player
        New(ChatType.earthquake, BuffType.stun, 3, 45, 6),
    };
    static ChatSkill New(ChatType word, BuffType type, float duration, float cooldown=60, float knockback=0)
        => new(){chatType = word, cooldown = cooldown, knockback = knockback, buff = new(){type = type, duration = duration}};
}
