using System;
using UnityEngine;

namespace RocketBoost.UI.Data
{
    /// <summary>
    /// Authored metadata for every playable sector: display name, scene order, star
    /// thresholds and marketing copy. The UI never hard-codes level names or par times.
    /// </summary>
    [CreateAssetMenu(menuName = "Rocket Boost/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class LevelDefinition
        {
            [Tooltip("Build-settings index of the sector scene. 0 is the main menu.")]
            public int buildIndex = 1;

            [Tooltip("Scene asset name, used to match the active scene when needed.")]
            public string sceneName = "Scene 1";

            [Tooltip("Short sector code shown on cards, e.g. SECTOR 01.")]
            public string sectorCode = "SECTOR 01";

            public string displayName = "FLY OVER";

            [TextArea(2, 3)]
            public string tagline = "Thread the canyon and kiss the landing pad.";

            [Header("Star Thresholds (seconds)")]
            [Tooltip("Best time at or under this value awards three stars.")]
            public float threeStarTime = 24f;

            [Tooltip("Best time at or under this value awards two stars.")]
            public float twoStarTime = 40f;
        }

        [SerializeField] private LevelDefinition[] levels = Array.Empty<LevelDefinition>();

        public int Count => levels != null ? levels.Length : 0;

        public LevelDefinition Get(int index) =>
            levels != null && index >= 0 && index < levels.Length ? levels[index] : null;

        /// <summary>Finds the definition whose build index matches, otherwise null.</summary>
        public LevelDefinition FindByBuildIndex(int buildIndex)
        {
            if (levels == null)
            {
                return null;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].buildIndex == buildIndex)
                {
                    return levels[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a recorded best time into a star count (0 when the level was never
        /// completed). Thresholds live on the level definition so designers can tune them.
        /// </summary>
        public int EvaluateStars(int levelIndex, bool hasBestTime, float bestTime)
        {
            if (!hasBestTime)
            {
                return 0;
            }

            LevelDefinition level = Get(levelIndex);
            if (level == null)
            {
                return 0;
            }

            if (bestTime <= level.threeStarTime)
            {
                return 3;
            }

            return bestTime <= level.twoStarTime ? 2 : 1;
        }

        /// <summary>The first sector is always open; each later sector needs the previous one cleared.</summary>
        public bool IsUnlocked(int levelIndex, Func<int, bool> isLevelCleared)
        {
            if (levelIndex <= 0)
            {
                return levelIndex == 0;
            }

            return isLevelCleared != null && isLevelCleared(levelIndex - 1);
        }
    }
}
