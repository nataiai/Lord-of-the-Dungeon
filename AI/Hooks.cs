using System.Collections.Generic;
using UnityEngine;

// The only calls the game makes into AI/ (search "AIHooks." to find each one), plus the hit helper the AI weapons share.
public static class AIHooks
{
    // HoldingWeapons.ForceUse, right after the attack's hitbox is made
    public static void OnAttack(HitBox hb, HoldingWeapons hand, Character user, Vector2 dir){
        Lock.Swing(hb, hand);                  // a key opens a Lock it touches (AI/Lock.cs)
        WeaponCharge.OnAttack(hb);             // a Multi's hit: no knockback, stun (AI/WeaponCharge.cs)
        Boomerang.Launch(hb, hand, user, dir); // a boomerang comes back (AI/Boomerang.cs)
    }

    // Loot.Drop, for each loot it makes. from: what dropped it (an enemy or chest, a crate, a thrown weapon landing)
    public static void OnDrop(Loot loot, Transform from){
        if(loot==null)return;
        if(from.TryGetComponent(out ThrownWeapon _)){loot.transform.localScale = from.lossyScale; return;} // a thrown weapon landing: keeps its size, no slide
        LootYoink.Push(loot.gameObject); // slides away (AI/Yoink.cs)
    }

    // a flying weapon (thrown or boomerang) touches `other`: hurts it if it's an enemy of `by` not hit yet (a chest too: opens it),
    // damages a crate if it's not magic. Returns what it hit, so the caller can stop
    public enum Touched{Nothing, Enemy, Crate}
    public static Touched Hit(Collider2D other, Character by, Weapon w, int damage, float knockback, Vector2 from, HashSet<Character> already){
        if(by!=null && other.TryGetComponent(out Character c) && by.IsEnemyOf(c)){
            if(!already.Add(c))return Touched.Nothing; // each once
            SoundPlayer.PlaySound(w.hitSound);
            c.recieveDamage(damage, w.isMagical);
            c.receiveKnockback(from, knockback);
            if(w.buff!=null)w.buff.CopyTo(c.gameObject);
            return Touched.Enemy;
        }
        if(!w.isMagical && other.TryGetComponent(out Destructable d)){d.recieveDamage(damage, w.isMagical); return Touched.Crate;}
        return Touched.Nothing;
    }
}
