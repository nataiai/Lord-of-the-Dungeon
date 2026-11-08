using UnityEngine;

// All experimental and test features, on/off in one place. Editable in the Inspector while playing
// (put it on any object in the scene, or it is made on first use: look for "Experimental" in the Hierarchy)
public class Experimental : MonoBehaviour{
    static Experimental _i;
    public static Experimental I => _i ? _i : (_i = FindFirstObjectByType<Experimental>() ?? new GameObject("Experimental").AddComponent<Experimental>());
    [Tooltip("Attacking stops player movement until the attack animation ends")] public bool stopMoveOnAttack = false;
    [Tooltip("false = no pathfinding, enemies walk straight at the player")] public bool pathFinding = false;
    [Tooltip("false = no separation, enemies may stack")] public bool separation = true;
    [Tooltip("AI/UnifiedChat.cs reads chat; false = Lexone TwitchChat reads chat instead")] public bool unifiedChat = false;
    [Tooltip("No chat: spawn mockEnemy every testWithoutChatSpawnCooldown seconds (hold L to pause)")] public bool testWithoutChat = false;
    public float testWithoutChatSpawnCooldown = 5f;
    [Tooltip("Player is invincible for invincibleCooldownTime after a hit")] public bool enableInvincible = false;
    [Tooltip("Turn the mouse off: no mouse aim or clicks")] public bool disableMouse = true;
}
