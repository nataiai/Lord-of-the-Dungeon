using UnityEngine;

public partial class Experimental : MonoBehaviour{ // Weapons switches: AI/Fields.cs
    static Experimental _i;
    public static Experimental I => _i ? _i : (_i = FindFirstObjectByType<Experimental>() ?? new GameObject("Experimental").AddComponent<Experimental>());

    [Header("Player")]
    [Tooltip("Attacking stops player movement until the attack animation ends")]
    public bool stopMoveOnAttack = true;
    [Tooltip("On: player always faces where it walks. Off: faces the look input (arrows, mouse, right stick) and keeps it when there is none")]
    public bool lookFollowMovement = true;
    [Tooltip("On: attacks fly up, right, down or left only (the way the sprite faces), never diagonal")]
    public bool fourWayAttack = true;
    [Tooltip("Player is invincible for invincibleCooldownTime after a hit")]
    public bool enableInvincible = false;
    [Tooltip("Turn the mouse off: no mouse aim or clicks")]
    public bool disableMouse = true;
    [Tooltip("On: hold a weapon button to charge its skill, let go to use it (AI/WeaponCharge.cs)")]
    public bool chargeSkills = true;
    [Tooltip("Off: no corner map (the big map on M still works)")]
    public bool miniMap = false;
    [Tooltip("On: the big map (M / gamepad Select) pauses the game (AI/MiniMap.cs)")]
    public bool bigMapPauses = true;

    [Header("Enemy AI")]
    [Tooltip("Off: no pathfinding, enemies walk straight at the player")]
    public bool pathFinding = true;
    [Tooltip("Off: no separation, enemies may stack")]
    public bool separation = true;
    [ShowIf("pathFinding", true)]
    [Tooltip("Space enemies keep from walls and rocks when the room allows it (tight spots use the exact body)")]
    public float wallSpacing = 0.25f;
    [ShowIf("separation", true)]
    [Tooltip("Space enemies keep from each other, times their own size (1 = half their diagonal: 0.71 for a 1x1 enemy)")]
    public float enemySpacing = 1f;

    [Header("Enemy reset: while you're out of its room")]
    [Tooltip("Enemies go back to where they started")]
    public bool resetPosition = true;
    [ShowIf("resetPosition", true)]
    [Tooltip("On: they walk back, as far as their speed gets them while you're away. Off: straight back there")]
    public bool walkBack = true;
    [Tooltip("Enemies heal")]
    public bool resetHealth = false;
    [ShowIf("resetHealth", true)]
    [Tooltip("HP per second while you're away. 0 = back to full at once")]
    public float enemyRegen = 1;
    [Tooltip("Enemies forget you: they wait until you come close again (their noticeRange)")]
    public bool forgetPlayer = true;
    [Tooltip("Buffs on enemies (burn, slow...) end")]
    public bool clearBuffs = true;
    [Tooltip("Their life span (they die after 24 hours) doesn't count while you're away")]
    public bool pauseLifeSpan = false;

    [Header("Chat")]
    [Tooltip("On: AI/UnifiedChat.cs reads chat. Off: Lexone TwitchChat reads chat")]
    public bool unifiedChat = true;
    [Tooltip("No chat: spawn mockEnemy every testWithoutChatSpawnCooldown seconds (hold L to pause)")]
    public bool testWithoutChat = false;
    [ShowIf("testWithoutChat", true)]
    public float testWithoutChatSpawnCooldown = 5f;
    [Tooltip("On: chat potions wait in the summon line with the enemies and use its cooldown. Off: their own line and cooldown (AI/ChatPotion.cs)")]
    public bool potionSamePool = true;
    [Tooltip("Off: chat skill words (berserk, float, earthquake...) do nothing (AI/ChatSkill.cs)")]
    public bool chatSkills = true;
    [ShowIf("chatSkills", true)]
    [Tooltip("Test: keys 1-6 cast berserk, invincible, magic shield, physical shield, float, earthquake (Summondb order)")]
    public bool skillKeys = true;
    [ShowIf("chatSkills", true)]
    [Tooltip("Skill votes need this x sqrt(chatters of the last 2 min), at least 1. 0 = one is enough")]
    public float crowdNeed = 1;

    [Header("Inspector")]
    [Tooltip("On: [PlayModeOnly] fields show (and can be edited) while playing. Off: always hidden")]
    public bool showPlayModeOnly = true;
    [Tooltip("On: [ShowIf] fields hide when their setting doesn't use them. Off: every field shows")]
    public bool useShowIf = true;
}
