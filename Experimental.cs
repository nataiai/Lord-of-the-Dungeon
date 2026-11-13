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

    [Header("Chat")]
    [Tooltip("On: AI/UnifiedChat.cs reads chat. Off: Lexone TwitchChat reads chat")]
    public bool unifiedChat = false;
    [Tooltip("No chat: spawn mockEnemy every testWithoutChatSpawnCooldown seconds (hold L to pause)")]
    public bool testWithoutChat = false;
    [ShowIf("testWithoutChat", true)]
    public float testWithoutChatSpawnCooldown = 5f;

    [Header("Inspector")]
    [Tooltip("On: [PlayModeOnly] fields show (and can be edited) while playing. Off: always hidden")]
    public bool showPlayModeOnly = true;
    [Tooltip("On: [ShowIf] fields hide when their setting doesn't use them. Off: every field shows")]
    public bool useShowIf = true;
}
