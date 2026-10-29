using System.Collections;
using RocketBoost.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RocketBoost
{
    /// <summary>
    /// Applies the rocket's collision rules: friendly scenery is ignored, the finish pad
    /// completes the level, and anything else destroys the rocket and reloads the level.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class CollisionHandler : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds to wait before reloading or advancing, so the effects can play.")]
        [SerializeField] private float delay = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip successSFX;
        [SerializeField] private AudioClip crashSFX;

        [SerializeField, Range(0f, 1f)] private float successVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float crashVolume = 1f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem crashParticles;

        private AudioSource audioSource;
        private Movement movement;
        private Coroutine transitionRoutine;

        private bool isControllable = true;
        private bool isCollidable = true;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            movement = GetComponent<Movement>();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RespondToDebugKeys();
#endif
        }

        private void OnDisable() => CancelTransition();

        private void RespondToDebugKeys()
        {
            if (Keyboard.current == null || !isControllable)
            {
                return;
            }

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                NextLevel();
            }
            else if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                isCollidable = !isCollidable;
                Debug.Log($"[CollisionHandler] Collisions {(isCollidable ? "enabled" : "disabled")}.");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isControllable || !isCollidable)
            {
                return;
            }

            if (collision.gameObject.CompareTag(GameTags.Friendly))
            {
                return;
            }

            if (collision.gameObject.CompareTag(GameTags.Finish))
            {
                StartNextLevelSequence();
                return;
            }

            StartCrashSequence();
        }

        private void StartNextLevelSequence()
        {
            if (!isControllable)
            {
                return;
            }

            isControllable = false;

            // Disable movement first: its OnDisable stops the engine loop, which would
            // otherwise cut off the success sting.
            if (movement != null)
            {
                movement.enabled = false;
            }

            if (successParticles != null)
            {
                successParticles.Play();
            }

            PlayOneShot(successSFX, successVolume);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSuccess();
            }

            // When the UI debrief is on screen it owns the transition to the next level.
            if (!UiPresence.LevelDebriefActive)
            {
                transitionRoutine = StartCoroutine(RunAfterDelay(NextLevel));
            }
        }

        private void StartCrashSequence()
        {
            if (!isControllable)
            {
                return;
            }

            isControllable = false;

            if (movement != null)
            {
                movement.enabled = false;
            }

            if (crashParticles != null)
            {
                crashParticles.Play();
            }

            if (crashSFX != null)
            {
                PlayOneShot(crashSFX, crashVolume);
            }
            else
            {
                Debug.LogWarning("[CollisionHandler] No crash SFX assigned on the player rocket.", this);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerCrashed();
            }

            // The crash screen offers an explicit Retry; only auto-reload without it.
            if (!UiPresence.GameOverActive)
            {
                transitionRoutine = StartCoroutine(RunAfterDelay(ReloadLevel));
            }
        }

        /// <summary>Plays a one-shot on the rocket's own source, replacing the engine loop.</summary>
        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.PlayOneShot(clip, volume);
        }

        private IEnumerator RunAfterDelay(System.Action action)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            transitionRoutine = null;
            action?.Invoke();
        }

        private void CancelTransition()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        /// <summary>Advances to the next level, or wraps to the menu on the final one.</summary>
        public void NextLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextLevel();
                return;
            }

            if (LevelProgression.TryGetNextIndex(
                    SceneManager.GetActiveScene().buildIndex,
                    SceneManager.sceneCountInBuildSettings,
                    out int nextSceneIndex))
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
        }

        /// <summary>Reloads the active level.</summary>
        public void ReloadLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartCurrentLevel();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
