using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Enemy")]
public class EnemySO : ScriptableObject
{
    public string enemyName; // what chat types to summon it: one word
    public float initSpeed;
    public int maxHP;
    public float cooldown; // seconds between hits on the player
    public CombatStat combatStat;
    public float knockbackResistance; // 1 = immune to knockback; 0 = get full knockback
    public float knocbackRecovery = 4;
    public float attackKnockback;
    public bool ignoreCollide;
    public float noticeRange=9999f;
}
