using RocketBoost.UI.Data;
using RocketBoost.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Crash debrief: short, punchy and one tap away from retrying. Adds a red vignette flash
    /// and a short panel shake on reveal.
    /// </summary>
    public sealed class GameOverScreen : UIScreen
    {
        [Header("Copy")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private TMP_Text attemptLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text retryHintLabel;

        [Header("Actions")]
        [SerializeField] private ThemedButton retryButton;
        [SerializeField] private ThemedButton menuButton;

        [Header("Presentation")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private VignetteFlash vignette;
        [SerializeField] private Image accentGlow;

        private int attempts;

        public int Attempts => attempts;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            Wire(retryButton, () => Manager.RestartLevel());
            Wire(menuButton, () => Manager.OpenMainMenu());

            SetDefaultSelection(retryButton != null ? retryButton.Button : null);
        }

        public void Present(int attemptCount)
        {
            attempts = attemptCount;
        }

        protected override void OnShown()
        {
            if (attemptLabel != null)
            {
                attemptLabel.SetText($"ATTEMPT  {Mathf.Max(1, attempts)}");
            }

            if (timeLabel != null && GameManager.Instance != null)
            {
                timeLabel.SetText($"FLIGHT TIME  {MissionTimeFormatter.Format(GameManager.Instance.LevelTime)}");
            }

            if (reasonLabel != null)
            {
                reasonLabel.SetText(ReasonCopy(attempts));
            }

            if (titleLabel != null)
            {
                titleLabel.color = Theme.danger;
            }

            if (accentGlow != null)
            {
                accentGlow.color = UITheme.Alpha(Theme.danger, 0.35f);
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.9f;
                UITween.Scale(panel, Vector3.one, Theme.durationNormal, UIEase.OutBack);
                UIShake.Play(panel, 18f, 0.4f);
            }

            vignette?.Flash(Theme.danger, 0.6f);
            SelectDefault();
        }

        public override bool HandleBack()
        {
            Manager.RestartLevel();
            return true;
        }

        private static string ReasonCopy(int attemptCount)
        {
            string[] lines =
            {
                "HULL INTEGRITY LOST",
                "SIGNAL LOST",
                "CRAFT DESTROYED"
            };

            return lines[Mathf.Abs(attemptCount) % lines.Length];
        }

        private void Wire(ThemedButton themed, UnityEngine.Events.UnityAction action)
        {
            if (themed == null || themed.Button == null)
            {
                return;
            }

            themed.Button.onClick.RemoveAllListeners();
            themed.Button.onClick.AddListener(action);
        }
    }
}
