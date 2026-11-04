// [System.Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "EnemySO",
    menuName = "Enemy"
)]
public class EnemySO : ScriptableObject
{
    public string enemyName;
    [FormerlySerializedAs("initSpeed")] public float moveSpeed;
    public int maxHP;
    [FormerlySerializedAs("cooldown")] public float attackCooldown;
    public CombatStat combatStat;
    public float knockbackResistance; // 1 = immune to knockback; 0 = get full knockback
    [FormerlySerializedAs("knocbackRecovery")] public float knockbackRecovery = 4;

    public float attackKnockback;
    public bool ignoreCollide;
    public float noticeRange=9999f;

    // public Loot loot;

}