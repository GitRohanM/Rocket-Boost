using System;
using System.Globalization;
using RocketBoost.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RocketBoost
{
    /// <summary>High-level state of the game session.</summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

    /// <summary>
    /// Central coordinator for game state, the level timer, attempt count, and scene
    /// transitions. One instance survives scene loads so the session keeps running
    /// across levels; per-scene duplicates destroy themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        /// <summary>Outcome of a completed level, published on <see cref="OnLevelCompleted"/>.</summary>
        public readonly struct LevelResult
        {
            public LevelResult(float time, float bestTime, int attempts, bool isNewRecord)
            {
                Time = time;
                BestTime = bestTime;
                Attempts = attempts;
                IsNewRecord = isNewRecord;
            }

            /// <summary>Flight time of this run, in seconds.</summary>
            public float Time { get; }

            /// <summary>Best time on record for this level, in seconds.</summary>
            public float BestTime { get; }

            /// <summary>Attempts used, including the successful one.</summary>
            public int Attempts { get; }

            /// <summary>True when this run beat the previous record.</summary>
            public bool IsNewRecord { get; }
        }

        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("Runtime Stats")]
        [SerializeField] private float levelTime;
        [SerializeField] private int attemptsCount = 1;
        [SerializeField] private bool isTimerRunning;

        public GameState CurrentState => currentState;
        public float LevelTime => levelTime;
        public int AttemptsCount => attemptsCount;
        public bool IsTimerRunning => isTimerRunning;
        public bool IsPlaying => currentState == GameState.Playing;

        /// <summary>Per-level best times, persisted through <see cref="PlayerPrefsKeyValueStore"/>.</summary>
        public BestTimeStore BestTimes { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<LevelResult> OnLevelCompleted;
        public event Action<int> OnPlayerCrashedEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Every scene ships a GameManager as a bootstrap, so the copies created by
                // each level load are expected: the first one wins, survives via
                // DontDestroyOnLoad and the later ones just clean themselves up.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BestTimes = new BestTimeStore(new PlayerPrefsKeyValueStore());
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start() => ApplySceneDefaults();

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        /// <summary>
        /// Clears the static reference between play sessions.
        /// The project runs with Domain Reload disabled (Enter Play Mode Options), so static
        /// fields survive a play-mode exit unless they are reset explicitly.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Update()
        {
            if (isTimerRunning && currentState == GameState.Playing)
            {
                levelTime += Time.deltaTime;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneDefaults();

        /// <summary>Resets per-scene session values and derives the state from the loaded scene.</summary>
        private void ApplySceneDefaults()
        {
            Time.timeScale = 1f;
            levelTime = 0f;

            if (IsMenuScene(SceneManager.GetActiveScene()))
            {
                attemptsCount = 1;
                isTimerRunning = false;
                SetState(GameState.MainMenu);
                return;
            }

            isTimerRunning = true;
            SetState(GameState.Playing);
        }

        private static bool IsMenuScene(Scene scene) =>
            scene.name.IndexOf("menu", StringComparison.OrdinalIgnoreCase) >= 0;

        public void SetState(GameState newState)
        {
            if (currentState == newState)
            {
                return;
            }

            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>Pauses while playing, or resumes while paused. No-op in any other state.</summary>
        public void TogglePause()
        {
            switch (currentState)
            {
                case GameState.Playing:
                    SetPaused(true);
                    break;
                case GameState.Paused:
                    SetPaused(false);
                    break;
            }
        }

        /// <summary>Pauses or resumes the session and freezes time with it.</summary>
        public void SetPaused(bool paused)
        {
            if (paused && currentState != GameState.Playing)
            {
                return;
            }

            if (!paused && currentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = paused ? 0f : 1f;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        /// <summary>Records a crash: increments the attempt counter and ends the run.</summary>
        public void OnPlayerCrashed()
        {
            if (currentState != GameState.Playing)
            {
                return;
            }

            attemptsCount++;
            isTimerRunning = false;
            SetState(GameState.GameOver);
            OnPlayerCrashedEvent?.Invoke(attemptsCount);
        }

        /// <summary>Records a successful landing, persists the best time, and publishes the result.</summary>
        public void OnPlayerSuccess()
        {
            if (currentState != GameState.Playing)
            {
                return;
            }

            isTimerRunning = false;
            SetState(GameState.LevelComplete);

            string levelId = GetLevelId(SceneManager.GetActiveScene());
            bool isNewRecord = BestTimes.Submit(levelId, levelTime, out float bestTime);

            OnLevelCompleted?.Invoke(new LevelResult(levelTime, bestTime, attemptsCount, isNewRecord));
        }

        /// <summary>Loads the next level, wrapping to the main menu after the final one.</summary>
        public void LoadNextLevel()
        {
            Time.timeScale = 1f;

            if (!LevelProgression.TryGetNextIndex(
                    SceneManager.GetActiveScene().buildIndex,
                    SceneManager.sceneCountInBuildSettings,
                    out int nextSceneIndex))
            {
                Debug.LogError("[GameManager] No scenes are configured in Build Settings; cannot load the next level.", this);
                return;
            }

            attemptsCount = 1;
            SceneManager.LoadScene(nextSceneIndex);
        }

        /// <summary>Reloads the active level, keeping the accumulated attempt count.</summary>
        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        /// <summary>Loads a level by build index, validating it first.</summary>
        public void LoadLevel(int sceneIndex)
        {
            Time.timeScale = 1f;

            int levelCount = SceneManager.sceneCountInBuildSettings;
            if (!LevelProgression.IsValidIndex(sceneIndex, levelCount))
            {
                Debug.LogError(
                    $"[GameManager] Scene index {sceneIndex} is outside Build Settings (0-{levelCount - 1}).",
                    this);
                return;
            }

            attemptsCount = 1;
            SceneManager.LoadScene(sceneIndex);
        }

        /// <summary>
        /// Starts loading a level without blocking the frame so the UI can present a loading
        /// screen. Mirrors <see cref="LoadLevel"/>: validates the index and resets attempts.
        /// </summary>
        /// <returns>The pending load, or null when the index is not in Build Settings.</returns>
        public AsyncOperation LoadLevelAsync(int sceneIndex)
        {
            Time.timeScale = 1f;

            int levelCount = SceneManager.sceneCountInBuildSettings;
            if (!LevelProgression.IsValidIndex(sceneIndex, levelCount))
            {
                Debug.LogError(
                    $"[GameManager] Scene index {sceneIndex} is outside Build Settings (0-{levelCount - 1}).",
                    this);
                return null;
            }

            attemptsCount = 1;
            return SceneManager.LoadSceneAsync(sceneIndex);
        }

        /// <summary>Async counterpart of <see cref="LoadNextLevel"/>, used by the loading screen.</summary>
        public AsyncOperation LoadNextLevelAsync()
        {
            Time.timeScale = 1f;

            if (!LevelProgression.TryGetNextIndex(
                    SceneManager.GetActiveScene().buildIndex,
                    SceneManager.sceneCountInBuildSettings,
                    out int nextSceneIndex))
            {
                Debug.LogError("[GameManager] No scenes are configured in Build Settings; cannot load the next level.", this);
                return null;
            }

            attemptsCount = 1;
            return SceneManager.LoadSceneAsync(nextSceneIndex);
        }

        /// <summary>Reads the best time for a level, if one was ever recorded.</summary>
        public bool TryGetBestTime(int sceneIndex, out float bestTime) =>
            BestTimes.TryGetBestTime(GetLevelId(sceneIndex), out bestTime);

        /// <summary>
        /// Stable identifier for a level. Build index keeps compatibility with records
        /// saved before this class existed.
        /// </summary>
        private static string GetLevelId(Scene scene) => GetLevelId(scene.buildIndex);

        private static string GetLevelId(int buildIndex) => buildIndex.ToString(CultureInfo.InvariantCulture);
    }
}
