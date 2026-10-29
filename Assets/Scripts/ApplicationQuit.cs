using UnityEngine;
using UnityEngine.InputSystem;

namespace RocketBoost
{
    /// <summary>
    /// Quits the application from the Escape key and works in both builds and play mode.
    /// <para>
    /// Escape is also the gameplay pause key, so this component stands down whenever the
    /// game session owns Escape (playing, paused, or debrief). Previously both handlers
    /// fired on the same frame, so pausing in the final level also quit the game.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ApplicationQuit : MonoBehaviour
    {
        [Tooltip("Allow Escape to close the application while no gameplay screen owns the key.")]
        [SerializeField] private bool quitOnEscape = true;

        private bool isQuitting;

        private void Update()
        {
            if (isQuitting || !quitOnEscape)
            {
                return;
            }

            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (IsGameplayUsingEscape())
            {
                return;
            }

            Quit();
        }

        /// <summary>True when a game session is on a screen where Escape means pause or debrief.</summary>
        private static bool IsGameplayUsingEscape()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                return false;
            }

            switch (manager.CurrentState)
            {
                case GameState.Playing:
                case GameState.Paused:
                case GameState.LevelComplete:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Stops play mode in the Editor and closes the player in a build.</summary>
        public void Quit()
        {
            if (isQuitting)
            {
                return;
            }

            isQuitting = true;
            Debug.Log("[ApplicationQuit] Exiting application.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
