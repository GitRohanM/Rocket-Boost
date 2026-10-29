using System.Collections;
using System.Collections.Generic;
using RocketBoost.Core;
using RocketBoost.UI.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Owns the UI state machine: one primary screen at a time plus a stack of modal
    /// overlays (pause, debrief, game over, settings). Screen prefabs are instantiated
    /// once and reused; the manager also drives loading transitions and scene routing.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UITheme theme;
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private UIScreen[] screenPrefabs = new UIScreen[0];

        [Header("Scene References")]
        [SerializeField] private RectTransform screenRoot;
        [SerializeField] private CanvasGroup backdropGroup;
        [SerializeField] private CanvasGroup transitionGroup;
        [SerializeField] private UIAudio audioService;
        [SerializeField] private UISettingsService settings = new UISettingsService();

        [Header("Flow")]
        [SerializeField] private float minimumLoadingTime = 1.15f;
        [SerializeField] private float transitionDuration = 0.32f;
        [SerializeField] private bool startOnHudInLevelScenes = true;

        private readonly Dictionary<UIScreenType, UIScreen> screens = new Dictionary<UIScreenType, UIScreen>();
        private readonly List<UIScreenType> overlayStack = new List<UIScreenType>();

        private UIScreen activePrimary;
        private bool subscribed;
        private bool transitioning;
        private float lastHoverTime;

        public static UIManager Instance { get; private set; }

        public UITheme Theme => theme;
        public LevelCatalog Catalog => levelCatalog;
        public UIAudio Audio => audioService;
        public UISettingsService Settings => settings;
        public bool IsTransitioning => transitioning;
        public UIScreenType ActivePrimary => activePrimary != null ? activePrimary.ScreenType : UIScreenType.MainMenu;
        public UIScreen ActiveOverlay =>
            overlayStack.Count > 0 && screens.TryGetValue(overlayStack[overlayStack.Count - 1], out UIScreen overlay)
                ? overlay
                : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

            Instance = this;
            settings.Load();

            if (audioService != null && theme != null)
            {
                audioService.Initialize(theme, settings);
            }

            BuildScreens();
            transitionGroup.alpha = 1f;
            transitionGroup.blocksRaycasts = true;
        }

        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            TrySubscribe();

            UIScreenType first = IsMenuScene(SceneManager.GetActiveScene())
                ? UIScreenType.MainMenu
                : startOnHudInLevelScenes ? UIScreenType.Hud : UIScreenType.MainMenu;

            ShowPrimary(first, instant: true, animateBackdrop: false);
            StartCoroutine(FadeInOnBoot());
            audioService.PlayMenuMusic();
        }

        private IEnumerator FadeInOnBoot()
        {
            yield return UITween.Run(UITween.FadeRoutine(transitionGroup, 0f, transitionDuration, null));
            transitionGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
                GameManager.Instance.OnPlayerCrashedEvent -= HandlePlayerCrashed;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (Instance != this)
            {
                return;
            }

            TrySubscribe();
            HandleSystemInput();
        }

        private void TrySubscribe()
        {
            if (subscribed || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnLevelCompleted += HandleLevelCompleted;
            GameManager.Instance.OnPlayerCrashedEvent += HandlePlayerCrashed;
            subscribed = true;
        }

        // ---------------------------------------------------------------- screens

        private void BuildScreens()
        {
            foreach (UIScreen prefab in screenPrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                UIScreen instance = Instantiate(prefab, screenRoot);
                instance.name = prefab.name;
                instance.Initialize(this, theme);
                instance.Hide(instant: true);
                screens[instance.ScreenType] = instance;
            }
        }

        /// <summary>Shows a full-screen view, dismissing any overlay on top of it.</summary>
        public void ShowPrimary(UIScreenType type, bool instant = false, bool animateBackdrop = true)
        {
            if (!screens.TryGetValue(type, out UIScreen next))
            {
                Debug.LogError($"[UIManager] No screen prefab registered for {type}.", this);
                return;
            }

            CloseAllOverlays();

            if (activePrimary != null && activePrimary != next)
            {
                activePrimary.Hide(instant);
            }

            activePrimary = next;
            next.Show(instant);
            UpdateBackdrop(instant || !animateBackdrop);
            UpdatePresenceFlags();
        }

        /// <summary>Shows a modal overlay on top of the current primary screen.</summary>
        public void ShowOverlay(UIScreenType type, bool instant = false)
        {
            if (!screens.TryGetValue(type, out UIScreen overlay))
            {
                Debug.LogError($"[UIManager] No screen prefab registered for {type}.", this);
                return;
            }

            if (overlayStack.Count > 0 && overlayStack[overlayStack.Count - 1] == type)
            {
                return;
            }

            if (overlayStack.Count > 0 && screens.TryGetValue(overlayStack[overlayStack.Count - 1], out UIScreen previous))
            {
                previous.Hide(instant);
            }

            overlayStack.Add(type);
            overlay.Show(instant);
            UpdateBackdrop(instant);
            UpdatePresenceFlags();
        }

        /// <summary>Closes the topmost overlay, restoring focus to what was underneath.</summary>
        public void CloseOverlay(bool instant = false)
        {
            if (overlayStack.Count == 0)
            {
                return;
            }

            UIScreenType top = overlayStack[overlayStack.Count - 1];
            overlayStack.RemoveAt(overlayStack.Count - 1);

            if (screens.TryGetValue(top, out UIScreen overlay))
            {
                overlay.Hide(instant);
            }

            if (overlayStack.Count > 0 && screens.TryGetValue(overlayStack[overlayStack.Count - 1], out UIScreen restored))
            {
                restored.Show(instant);
                UpdateBackdrop(instant);
            }
            else
            {
                activePrimary?.SelectDefault();
                UpdateBackdrop(instant);
            }

            UpdatePresenceFlags();
        }

        public void CloseAllOverlays()
        {
            while (overlayStack.Count > 0)
            {
                CloseOverlay(instant: true);
            }
        }

        private void UpdateBackdrop(bool instant)
        {
            bool visible = activePrimary != null &&
                           (activePrimary.ShowBackdrop || overlayStack.Count > 0 && IsMenuPrimary());

            if (backdropGroup == null)
            {
                return;
            }

            backdropGroup.blocksRaycasts = false;

            if (instant)
            {
                backdropGroup.alpha = visible ? 1f : 0f;
                backdropGroup.gameObject.SetActive(visible);
                return;
            }

            if (visible && !backdropGroup.gameObject.activeSelf)
            {
                backdropGroup.gameObject.SetActive(true);
                backdropGroup.alpha = 0f;
            }

            UITween.Run(UITween.FadeRoutine(backdropGroup, visible ? 1f : 0f, theme != null ? theme.durationScreenFade : 0.3f, () =>
            {
                if (backdropGroup != null && backdropGroup.alpha <= 0.01f)
                {
                    backdropGroup.gameObject.SetActive(false);
                }
            }));
        }

        private bool IsMenuPrimary() =>
            activePrimary != null &&
            (activePrimary.ScreenType == UIScreenType.MainMenu || activePrimary.ScreenType == UIScreenType.LevelSelect);

        private void UpdatePresenceFlags()
        {
            UiPresence.LevelDebriefActive = overlayStack.Contains(UIScreenType.LevelComplete);
            UiPresence.GameOverActive = overlayStack.Contains(UIScreenType.GameOver);
        }

        // ---------------------------------------------------------------- flow

        public void OpenPause()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            GameManager.Instance.SetPaused(true);
            audioService.PlayBack();
            ShowOverlay(UIScreenType.Pause);
        }

        public void ResumeGame()
        {
            CloseOverlay();
            GameManager.Instance?.SetPaused(false);
            audioService.PlayConfirm();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            GoToLevel(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            int next = 0;
            if (LevelProgression.TryGetNextIndex(
                    SceneManager.GetActiveScene().buildIndex,
                    SceneManager.sceneCountInBuildSettings,
                    out int computed))
            {
                next = computed;
            }

            GoToLevel(next);
        }

        public void OpenMainMenu() => GoToLevel(0);

        public void GoToLevel(int buildIndex)
        {
            if (transitioning)
            {
                return;
            }

            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[UIManager] Scene index {buildIndex} is not in Build Settings.", this);
                return;
            }

            StartCoroutine(LoadLevelRoutine(buildIndex));
        }

        private IEnumerator LoadLevelRoutine(int buildIndex)
        {
            transitioning = true;
            audioService.PlayConfirm();

            yield return UITween.FadeRoutine(transitionGroup, 1f, transitionDuration, null);
            transitionGroup.blocksRaycasts = true;

            Time.timeScale = 1f;
            ShowPrimary(UIScreenType.Loading, instant: true, animateBackdrop: false);

            if (screens.TryGetValue(UIScreenType.Loading, out UIScreen loadingScreen) && loadingScreen is LoadingScreen loading)
            {
                loading.Begin();
            }

            yield return null;

            // Lift the curtain again so the progress screen is actually visible while loading.
            yield return UITween.FadeRoutine(transitionGroup, 0f, transitionDuration, null);
            transitionGroup.blocksRaycasts = false;

            AsyncOperation operation = LoadSceneAsync(buildIndex);
            if (operation == null)
            {
                transitioning = false;
                yield return UITween.FadeRoutine(transitionGroup, 0f, transitionDuration, null);
                yield break;
            }

            operation.allowSceneActivation = false;
            float startedAt = Time.unscaledTime;

            while (operation.progress < 0.9f)
            {
                ReportProgress(operation.progress / 0.9f);
                yield return null;
            }

            ReportProgress(1f);

            while (Time.unscaledTime - startedAt < minimumLoadingTime)
            {
                yield return null;
            }

            // Curtain back down for the swap, so the old scene never flashes.
            yield return UITween.FadeRoutine(transitionGroup, 1f, transitionDuration, null);
            transitionGroup.blocksRaycasts = true;

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;

            UIScreenType destination = IsMenuScene(SceneManager.GetActiveScene())
                ? UIScreenType.MainMenu
                : UIScreenType.Hud;

            ShowPrimary(destination, instant: true, animateBackdrop: false);
            RefreshPresence();

            yield return UITween.FadeRoutine(transitionGroup, 0f, transitionDuration, null);
            transitionGroup.blocksRaycasts = false;
            transitioning = false;
        }

        private AsyncOperation LoadSceneAsync(int buildIndex)
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.LoadLevelAsync(buildIndex);
            }

            return SceneManager.LoadSceneAsync(buildIndex);
        }

        private void ReportProgress(float normalized)
        {
            if (screens.TryGetValue(UIScreenType.Loading, out UIScreen screen) && screen is LoadingScreen loading)
            {
                loading.SetProgress(normalized);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UiPresence.LevelDebriefActive = false;
            UiPresence.GameOverActive = false;

            if (transitioning)
            {
                return;
            }

            UIScreenType destination = IsMenuScene(scene) ? UIScreenType.MainMenu : UIScreenType.Hud;
            ShowPrimary(destination, instant: false);
            RefreshPresence();

            if (audioService != null)
            {
                audioService.PlayMenuMusic();
            }
        }

        private void RefreshPresence()
        {
            UiPresence.LevelDebriefActive = false;
            UiPresence.GameOverActive = false;
        }

        private static bool IsMenuScene(Scene scene) =>
            scene.name.IndexOf("menu", System.StringComparison.OrdinalIgnoreCase) >= 0;

        // ---------------------------------------------------------------- events

        private void HandleLevelCompleted(GameManager.LevelResult result)
        {
            if (screens.TryGetValue(UIScreenType.LevelComplete, out UIScreen screen) && screen is LevelCompleteScreen complete)
            {
                complete.Present(result);
            }

            ShowOverlay(UIScreenType.LevelComplete);
            audioService.PlayLevelComplete();
        }

        private void HandlePlayerCrashed(int attempts)
        {
            if (screens.TryGetValue(UIScreenType.GameOver, out UIScreen screen) && screen is GameOverScreen gameOver)
            {
                gameOver.Present(attempts);
            }

            ShowOverlay(UIScreenType.GameOver);
            audioService.PlayCrash();
        }

        // ---------------------------------------------------------------- input

        private void HandleSystemInput()
        {
            if (transitioning)
            {
                return;
            }

            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            UnityEngine.InputSystem.Gamepad gamepad = UnityEngine.InputSystem.Gamepad.current;

            pressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame
                      || gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame);
#endif

            if (!pressed)
            {
                return;
            }

            if (overlayStack.Count > 0)
            {
                UIScreen overlay = ActiveOverlay;
                if (overlay != null && overlay.HandleBack())
                {
                    return;
                }

                if (overlayStack.Contains(UIScreenType.Settings))
                {
                    CloseOverlay();
                    audioService.PlayBack();
                    return;
                }

                if (overlay is PauseScreen)
                {
                    ResumeGame();
                    return;
                }

                return;
            }

            if (activePrimary == null)
            {
                return;
            }

            if (activePrimary.HandleBack())
            {
                return;
            }

            if (activePrimary.ScreenType == UIScreenType.Hud)
            {
                OpenPause();
            }
            else if (activePrimary.ScreenType == UIScreenType.LevelSelect)
            {
                audioService.PlayBack();
                ShowPrimary(UIScreenType.MainMenu);
            }
        }

        // ---------------------------------------------------------------- UI audio helpers

        /// <summary>Rate-limited hover sound so scanning a list does not machine-gun the mixer.</summary>
        public void PlayHover()
        {
            if (Time.unscaledTime - lastHoverTime < 0.045f)
            {
                return;
            }

            lastHoverTime = Time.unscaledTime;
            audioService.PlayHover();
        }
    }
}
