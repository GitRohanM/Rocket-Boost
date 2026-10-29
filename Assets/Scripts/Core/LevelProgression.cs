namespace RocketBoost.Core
{
    /// <summary>
    /// Pure helpers for navigating the build-settings scene list.
    /// <para>
    /// Scene indices come from Unity, but the wrap-around and "is this the last level"
    /// rules are plain arithmetic and are covered by unit tests.
    /// </para>
    /// </summary>
    public static class LevelProgression
    {
        /// <summary>True when <paramref name="index"/> addresses a scene in the build list.</summary>
        public static bool IsValidIndex(int index, int levelCount) =>
            levelCount > 0 && index >= 0 && index < levelCount;

        /// <summary>
        /// True when the index is the last level.
        /// An empty build list counts as "last" so callers fall back to the menu instead of looping.
        /// </summary>
        public static bool IsLastLevel(int index, int levelCount) =>
            levelCount <= 0 || index >= levelCount - 1;

        /// <summary>
        /// Calculates the next level, wrapping back to the first scene (the main menu)
        /// after the final level.
        /// </summary>
        /// <returns><c>false</c> when no scenes are configured in the build settings.</returns>
        public static bool TryGetNextIndex(int currentIndex, int levelCount, out int nextIndex)
        {
            if (levelCount <= 0)
            {
                nextIndex = -1;
                return false;
            }

            if (currentIndex < 0 || currentIndex + 1 >= levelCount)
            {
                nextIndex = 0;
                return true;
            }

            nextIndex = currentIndex + 1;
            return true;
        }
    }
}
