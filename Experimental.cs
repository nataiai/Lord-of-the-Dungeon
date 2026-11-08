using UnityEngine;

// All experimental and test features, on/off in one place. Editable in the Inspector while playing
// (put it on any object in the scene, or it is made on first use: look for "Experimental" in the Hierarchy)
public class Experimental : MonoBehaviour{
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

    [Header("Enemy AI")]
    [Tooltip("Off: no pathfinding, enemies walk straight at the player")]
    public bool pathFinding = false;
    [Tooltip("Off: no separation, enemies may stack")]
    public bool separation = true;

    [Header("Chat")]
    [Tooltip("On: AI/UnifiedChat.cs reads chat. Off: Lexone TwitchChat reads chat")]
    public bool unifiedChat = false;
    [Tooltip("No chat: spawn mockEnemy every testWithoutChatSpawnCooldown seconds (hold L to pause)")]
    public bool testWithoutChat = false;
    public float testWithoutChatSpawnCooldown = 5f;

    [Header("Inspector")]
    [Tooltip("On: [PlayModeOnly] fields show (and can be edited) while playing. Off: always hidden")]
    public bool showPlayModeOnly = true;
}
