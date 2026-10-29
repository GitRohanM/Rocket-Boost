using TMPro;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Main menu: animated logo, primary navigation and the quit action.</summary>
    public sealed class MainMenuScreen : UIScreen
    {
        [Header("Actions")]
        [SerializeField] private ThemedButton playButton;
        [SerializeField] private ThemedButton levelSelectButton;
        [SerializeField] private ThemedButton settingsButton;
        [SerializeField] private ThemedButton quitButton;

        [Header("Presentation")]
        [SerializeField] private RectTransform logoGroup;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private GameObject campaignBadge;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            Wire(playButton, StartCampaign);
            Wire(levelSelectButton, OpenLevelSelect);
            Wire(settingsButton, OpenSettings);
            Wire(quitButton, QuitGame);

            SetDefaultSelection(playButton != null ? playButton.Button : null);

            if (versionLabel != null)
            {
                versionLabel.SetText($"BUILD {Application.version}  //  UNITY {Application.unityVersion}");
            }

            if (subtitleLabel != null && Theme != null)
            {
                subtitleLabel.color = Theme.textSecondary;
            }
        }

        protected override void OnShown()
        {
            SetDefaultSelection(playButton != null ? playButton.Button : null);
            SelectDefault();
            PlayEntrance();
        }

        private void PlayEntrance()
        {
            if (logoGroup != null)
            {
                Vector2 home = logoGroup.anchoredPosition;
                logoGroup.anchoredPosition = home + new Vector2(-40f, 0f);
                UITween.Move(logoGroup, home, Theme != null ? Theme.durationSlow : 0.42f, UIEase.OutCubic);
            }

            if (menuGroup == null)
            {
                return;
            }

            menuGroup.alpha = 0f;
            UITween.Fade(menuGroup, 1f, Theme != null ? Theme.durationNormal : 0.22f);
        }

        private void StartCampaign()
        {
            if (Manager == null)
            {
                return;
            }

            Data.LevelCatalog catalog = Manager.Catalog;
            int buildIndex = 1;
            if (catalog != null && catalog.Count > 0 && catalog.Get(0) != null)
            {
                buildIndex = catalog.Get(0).buildIndex;
            }

            Manager.GoToLevel(buildIndex);
        }

        private void OpenLevelSelect()
        {
            Manager?.ShowPrimary(UIScreenType.LevelSelect);
        }

        private void OpenSettings()
        {
            Manager?.ShowOverlay(UIScreenType.Settings);
        }

        private void QuitGame()
        {
            Manager?.Audio.PlayBack();

            ApplicationQuit quit = FindFirstObjectByType<ApplicationQuit>();
            if (quit != null)
            {
                quit.Quit();
                return;
            }

            Application.Quit();
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
