namespace RocketBoost
{
    /// <summary>
    /// Tag names used by gameplay code.
    /// <para>
    /// Tags are plain strings in Unity, so a typo compiles and then fails at runtime
    /// (or throws for an undefined tag). Centralising them keeps the collision rules
    /// in one reviewable place.
    /// </para>
    /// </summary>
    public static class GameTags
    {
        /// <summary>Safe scenery the rocket can touch without crashing, e.g. the launch pad.</summary>
        public const string Friendly = "Friendly";

        /// <summary>Level goal. Landing on it completes the level.</summary>
        public const string Finish = "Finish";

        /// <summary>Physics layer name used by the player rocket.</summary>
        public const string Player = "Player";
    }
}
