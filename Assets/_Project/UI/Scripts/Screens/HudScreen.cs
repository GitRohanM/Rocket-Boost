using RocketBoost.Core;
using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RocketBoost.UI
{
    /// <summary>
    /// In-game HUD: sector chip, live timer, attempt count, speed readout, boost meter and the
    /// pause button. All readouts are change-driven so the HUD does not rebuild strings every frame.
    /// </summary>
    public sealed class HudScreen : UIScreen
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text sectorLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text attemptLabel;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private GaugeBar boostBar;
        [SerializeField] private GaugeBar speedBar;

        [Header("Chrome")]
        [SerializeField] private ThemedButton pauseButton;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private CanvasGroup hintGroup;

        [SerializeField] private float boostAttack = 7f;
        [SerializeField] private float boostRelease = 2.5f;

        private Movement movement;
        private int lastCentiseconds = -1;
        private int lastAttempts = -1;
        private int lastSpeed = -1;
        private float boost;
        private float hintTimer;
        private bool hintShown;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            if (pauseButton != null && pauseButton.Button != null)
            {
                pauseButton.Button.onClick.RemoveAllListeners();
                pauseButton.Button.onClick.AddListener(() => Manager.OpenPause());
            }

            SetDefaultSelection(pauseButton != null ? pauseButton.Button : null);
        }

        protected override void OnShown()
        {
            lastCentiseconds = -1;
            lastAttempts = -1;
            lastSpeed = -1;
            boost = 0f;
            hintTimer = 0f;
            hintShown = false;

            if (hintGroup != null)
            {
                // The keyboard hint has no meaning in portrait and its pill overlaps the speed
                // readout once the canvas narrows, so it only shows in landscape.
                bool showHint = Screen.width >= Screen.height;
                hintGroup.alpha = showHint ? 1f : 0f;
                hintGroup.gameObject.SetActive(true);
                hintShown = !showHint;
            }

            RefreshSector();
            PlayEntrance();

            if (Manager != null)
            {
                Manager.Audio.DuckMusic(true);
            }
        }

        protected override void OnHidden()
        {
            if (Manager != null)
            {
                Manager.Audio.DuckMusic(false);
            }
        }

        public override bool HandleBack()
        {
            Manager.OpenPause();
            return true;
        }

        private void PlayEntrance()
        {
            if (topBar != null)
            {
                Vector2 home = topBar.anchoredPosition;
                topBar.anchoredPosition = home + new Vector2(0f, 64f);
                UITween.Move(topBar, home, Theme.durationSlow, UIEase.OutCubic);
            }

            if (bottomBar != null)
            {
                Vector2 home = bottomBar.anchoredPosition;
                bottomBar.anchoredPosition = home + new Vector2(0f, -72f);
                UITween.Move(bottomBar, home, Theme.durationSlow, UIEase.OutCubic);
            }
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureMovement();
            UpdateTimer();
            UpdateSpeed();
            UpdateBoost();
            UpdateHint();
        }

        private void EnsureMovement()
        {
            if (movement != null)
            {
                return;
            }

            movement = FindFirstObjectByType<Movement>();
        }

        private void RefreshSector()
        {
            if (sectorLabel == null)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            string label = scene.name.ToUpperInvariant();

            LevelCatalog catalog = Manager != null ? Manager.Catalog : null;
            LevelCatalog.LevelDefinition definition = catalog != null ? catalog.FindByBuildIndex(scene.buildIndex) : null;
            if (definition != null)
            {
                label = string.Concat(definition.sectorCode, "  //  ", definition.displayName);
            }

            sectorLabel.SetText(label);
        }

        private void UpdateTimer()
        {
            if (timerLabel == null || GameManager.Instance == null)
            {
                return;
            }

            int centiseconds = MissionTimeFormatter.ToCentiseconds(GameManager.Instance.LevelTime);
            if (centiseconds == lastCentiseconds)
            {
                return;
            }

            lastCentiseconds = centiseconds;
            timerLabel.SetText(
                "{0:00}:{1:00}.{2:00}",
                centiseconds / 6000,
                centiseconds / 100 % 60,
                centiseconds % 100);
        }

        private void UpdateSpeed()
        {
            float velocity = movement != null ? movement.VelocityMagnitude : 0f;
            int speed = Mathf.RoundToInt(velocity);

            if (speed != lastSpeed)
            {
                lastSpeed = speed;
                if (speedLabel != null)
                {
                    speedLabel.SetText("{0:000}", speed);
                }
            }

            if (speedBar != null)
            {
                speedBar.SetValue(Mathf.Clamp01(velocity / 60f));
            }
        }

        private void UpdateBoost()
        {
            bool thrusting = movement != null && movement.IsThrusting;
            float target = thrusting ? 1f : 0.12f;
            float rate = thrusting ? boostAttack : boostRelease;
            boost = Mathf.MoveTowards(boost, target, rate * Time.deltaTime);

            if (boostBar != null)
            {
                boostBar.SetValue(boost);
            }

            if (attemptLabel != null && GameManager.Instance != null)
            {
                int attempts = GameManager.Instance.AttemptsCount;
                if (attempts != lastAttempts)
                {
                    lastAttempts = attempts;
                    attemptLabel.SetText("ATTEMPT #{0}", attempts);
                }
            }
        }

        private void UpdateHint()
        {
            if (!hintShown && hintGroup != null)
            {
                hintTimer += Time.unscaledDeltaTime;
                if (hintTimer > 7f)
                {
                    hintShown = true;
                    UITween.Fade(hintGroup, 0f, Theme.durationSlow);
                }
            }
        }
    }
}
