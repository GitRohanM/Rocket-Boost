using UnityEngine;

namespace RocketBoost
{
    /// <summary>
    /// Minimal bridge between gameplay and the UI layer so screens can own the debrief and
    /// crash flow without the gameplay assembly referencing UI assemblies.
    /// </summary>
    public static class UiPresence
    {
        /// <summary>True while the level-complete debrief is on screen.</summary>
        public static bool LevelDebriefActive { get; set; }

        /// <summary>True while the crash / retry screen is on screen.</summary>
        public static bool GameOverActive { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LevelDebriefActive = false;
            GameOverActive = false;
        }
    }
}
