using RocketBoost.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RocketBoost.UI
{
    /// <summary>Modal pause menu. Time is frozen by the manager; every tween here is unscaled.</summary>
    public sealed class PauseScreen : UIScreen
    {
        [Header("Actions")]
        [SerializeField] private ThemedButton resumeButton;
        [SerializeField] private ThemedButton restartButton;
        [SerializeField] private ThemedButton settingsButton;
        [SerializeField] private ThemedButton mainMenuButton;

        [Header("Readouts")]
        [SerializeField] private TMP_Text sectorLabel;
        [SerializeField] private TMP_Text timeLabel;

        [Header("Presentation")]
        [SerializeField] private RectTransform panel;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            Wire(resumeButton, () => Manager.ResumeGame());
            Wire(restartButton, () => Manager.RestartLevel());
            Wire(settingsButton, () => Manager.ShowOverlay(UIScreenType.Settings));
            Wire(mainMenuButton, () => Manager.OpenMainMenu());

            SetDefaultSelection(resumeButton != null ? resumeButton.Button : null);
        }

        protected override void OnShown()
        {
            RefreshCopy();
            SelectDefault();

            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.92f;
                UITween.Scale(panel, Vector3.one, Theme.durationNormal, UIEase.OutBack);
            }
        }

        public override bool HandleBack()
        {
            Manager.ResumeGame();
            return true;
        }

        private void RefreshCopy()
        {
            if (sectorLabel != null)
            {
                string label = SceneManager.GetActiveScene().name.ToUpperInvariant();
                Data.LevelCatalog.LevelDefinition definition = Manager.Catalog != null
                    ? Manager.Catalog.FindByBuildIndex(SceneManager.GetActiveScene().buildIndex)
                    : null;

                if (definition != null)
                {
                    label = string.Concat(definition.sectorCode, "  //  ", definition.displayName);
                }

                sectorLabel.SetText(label);
            }

            if (timeLabel != null && GameManager.Instance != null)
            {
                timeLabel.SetText($"MISSION TIME  {MissionTimeFormatter.Format(GameManager.Instance.LevelTime)}");
            }
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
