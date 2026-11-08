// All experimental features, on/off in one place
public static class Experimental{
    public static bool stopMoveOnAttack = false; // attacking stops player movement until the attack animation ends
    public static bool pathFinding = false; // false = no pathfinding, enemies walk straight at the player
    public static bool separation = true; // false = no separation, enemies may stack
    public static bool unifiedChat = false; // AI/UnifiedChat.cs reads chat; false = Lexone TwitchChat reads chat instead
}
