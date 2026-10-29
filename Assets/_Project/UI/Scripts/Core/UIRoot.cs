using UnityEngine;
using UnityEngine.EventSystems;

namespace RocketBoost.UI
{
    /// <summary>
    /// Marks the UI hierarchy that survives scene loads. One instance bootstraps the
    /// manager, audio and settings; duplicates placed in later scenes remove themselves.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private UIManager manager;
        [SerializeField] private UIAudio audioService;

        public static UIRoot Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Deactivate first so the duplicate's EventSystem and tweens never start
                // before the deferred Destroy lands at the end of the frame.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Only the surviving root registers an event system; scene copies ship it disabled.
            EventSystem eventSystem = GetComponentInChildren<EventSystem>(true);
            if (eventSystem != null && !eventSystem.enabled)
            {
                eventSystem.enabled = true;
            }

            if (manager == null)
            {
                manager = GetComponentInChildren<UIManager>(true);
            }

            if (audioService == null)
            {
                audioService = GetComponentInChildren<UIAudio>(true);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
