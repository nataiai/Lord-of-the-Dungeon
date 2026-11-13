using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// tap a weapon button: normal attack. hold it for chargeSettings.chargeTime, let go: the weapon's skill
// settings: Weapon > Charge Settings. on/off: Experimental.chargeSkills
public enum WeaponSkill{None, Big, Spin, Volley, Flurry, Lunge, Multi}

public static class WeaponCharge
{
    static readonly Dictionary<SpriteRenderer,Color> baseColor = new();

    // Player.TryAttack: true = no normal attack this frame
    public static bool Blocks(Player p, HoldingWeapons hand, ControllerButton button){
        var w = hand.weapon;
        if(!Experimental.I.chargeSkills || w==null || w.chargeSettings.autoAttack){
            if(hand.charging){hand.charging = false; Glow(p);} // weapon changed mid-charge
            return false;
        }
        if(button.isPressed){
            if(!hand.charging){
                hand.charging = true;
                hand.chargeStart = Time.time;
                return false; // the press itself: normal attack
            }
            Glow(p);
            return true; // held: no repeat attacks
        }
        if(hand.charging){
            bool charged = IsCharged(hand);
            hand.charging = false;
            Glow(p);
            if(charged && hand.isUsable)Fire(p, hand);
        }
        return true;
    }
    static bool IsCharged(HoldingWeapons hand)=>hand.getChargePercent()>=1;
    static void Glow(Player p){ // yellow while a hand is charged
        var sr = p.GetComponentInChildren<SpriteRenderer>();
        if(sr==null)return;
        if(!baseColor.ContainsKey(sr))baseColor[sr] = sr.color;
        bool on = IsCharged(p.lWeapon) || IsCharged(p.rWeapon);
        sr.color = on ? Color.Lerp(baseColor[sr], Color.yellow, .5f) : baseColor[sr];
    }

    static void Fire(Player p, HoldingWeapons hand){
        var w = hand.weapon;
        var cs = w.chargeSettings;
        if(cs.skill==WeaponSkill.Multi){p.StartCoroutine(Multi(hand, p, cs)); return;}
        hand.lastUse = Time.time; // cooldown starts after the skill
        Vector2 dir = Experimental.I.fourWayAttack || !p.isLooking ? Direction.vec2MoveDict[p.animFacing] : p.looking;
        if(w.useSound!=null)SoundPlayer.PlaySound(w.useSound);
        hand.PlayAnim(p);
        p.ForceAnim(hand);
        switch(cs.skill){
            case WeaponSkill.Big:
            Spawn(hand, p, dir, cs.damage, cs.size);
            break;
            case WeaponSkill.Spin:
            for(int i=0;i<cs.count;i++)Spawn(hand, p, Turn(dir, 360f*i/cs.count), cs.damage, 1);
            break;
            case WeaponSkill.Volley:
            for(int i=0;i<cs.count;i++)Spawn(hand, p, Turn(dir, (i-(cs.count-1)/2f)*cs.spread), cs.damage, 1);
            break;
            case WeaponSkill.Flurry:
            p.StartCoroutine(Flurry(hand, p, dir, cs));
            break;
            case WeaponSkill.Lunge:
            p.StartCoroutine(Lunge(hand, p, dir, cs));
            break;
        }
    }
    static IEnumerator Flurry(HoldingWeapons hand, Player p, Vector2 dir, ChargeSettings cs){
        for(int i=0;i<cs.count;i++){
            Spawn(hand, p, dir, cs.damage, 1);
            yield return new WaitForSeconds(.08f);
        }
    }
    static ChargeSettings multi; // while a Multi attacks: OnAttack changes its hitbox
    static IEnumerator Multi(HoldingWeapons hand, Player p, ChargeSettings cs){ // normal attacks in a row
        var w = hand.weapon;
        for(int i=0;i<cs.count;i++){
            if(i>0)yield return new WaitForSeconds(Gap(w, cs));
            if(hand.weapon!=w)yield break; // thrown or swapped
            multi = cs;
            p.ForceAttack(hand);
            multi = null;
        }
    }
    static float Gap(Weapon w, ChargeSettings cs) => Mathf.Max(w.cooldown+cs.interval, 0); // between a Multi's attacks
    // AIHooks.OnAttack: a Multi's hit can skip knockback, and stun until the next hit
    public static void OnAttack(HitBox hb){
        if(multi==null)return;
        if(multi.noKnockback)hb.knockback = 0;
        if(multi.stun)new BuffEffect.Buff{type = BuffType.stun, duration = Gap(hb.weapon, multi)+.1f, isStackable = true}.CopyTo(hb.gameObject);
    }
    static IEnumerator Lunge(HoldingWeapons hand, Player p, Vector2 dir, ChargeSettings cs){
        p.rb.velocity = dir*cs.dashSpeed;
        p.froze(); // no walking while it dashes
        yield return new WaitForSeconds(.15f);
        Spawn(hand, p, dir, cs.damage, 1);
    }

    static Vector2 Turn(Vector2 dir, float angle) => Quaternion.Euler(0,0,angle)*dir;

    static void Spawn(HoldingWeapons hand, Player p, Vector2 dir, float damage, float size){
        if(hand.weapon!=null)hand.SpawnHitBox(p, dir, dir, damage, size); // null: thrown mid-flurry
    }
}

public partial class HoldingWeapons{ // fields: AI/Fields.cs
    // 0 to 1 while a charge skill's button is held, 1 = ready. 0 = not charging (for ui, like getReloadPercent)
    public float getChargePercent(){
        if(!charging || weapon==null)return 0;
        return Mathf.Min((Time.time-chargeStart)/Mathf.Max(weapon.chargeSettings.chargeTime,0.0001f),1);
    }
}
