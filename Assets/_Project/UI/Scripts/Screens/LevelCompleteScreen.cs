using RocketBoost.Core;
using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Debrief shown after a successful landing: stars awarded, rank, count-up flight time,
    /// sector record and the next / retry / menu actions.
    /// </summary>
    public sealed class LevelCompleteScreen : UIScreen
    {
        [Header("Copy")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text sectorLabel;
        [SerializeField] private TMP_Text rankLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private TMP_Text attemptsLabel;
        [SerializeField] private TMP_Text nextHintLabel;

        [Header("Stats")]
        [SerializeField] private CountUpText timeText;
        [SerializeField] private StarRating stars;
        [SerializeField] private GameObject newRecordBadge;

        [Header("Actions")]
        [SerializeField] private ThemedButton nextButton;
        [SerializeField] private ThemedButton retryButton;
        [SerializeField] private ThemedButton menuButton;

        [Header("Presentation")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private UIConfetti confetti;

        private GameManager.LevelResult result;
        private bool hasResult;
        private bool isFinalLevel;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            Wire(nextButton, Advance);
            Wire(retryButton, () => Manager.RestartLevel());
            Wire(menuButton, () => Manager.OpenMainMenu());

            SetDefaultSelection(nextButton != null ? nextButton.Button : null);
        }

        /// <summary>Feeds the finished run to the screen; the manager shows it immediately after.</summary>
        public void Present(GameManager.LevelResult levelResult)
        {
            result = levelResult;
            hasResult = true;
        }

        protected override void OnShown()
        {
            if (!hasResult)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            LevelCatalog catalog = Manager != null ? Manager.Catalog : null;
            LevelCatalog.LevelDefinition definition = catalog != null ? catalog.FindByBuildIndex(scene.buildIndex) : null;

            isFinalLevel = LevelProgression.IsLastLevel(scene.buildIndex, SceneManager.sceneCountInBuildSettings);

            if (titleLabel != null)
            {
                titleLabel.SetText(isFinalLevel ? "CAMPAIGN COMPLETE" : "SECTOR CLEARED");
            }

            if (sectorLabel != null)
            {
                sectorLabel.SetText(definition != null
                    ? $"{definition.sectorCode}  //  {definition.displayName}"
                    : scene.name.ToUpperInvariant());
            }

            MissionRank rank = MissionRankCalculator.Evaluate(result.Attempts, result.Time);
            if (rankLabel != null)
            {
                rankLabel.SetText(MissionRankCalculator.GetLabel(rank));
                rankLabel.color = RankColor(rank);
            }

            int starCount = catalog != null
                ? catalog.EvaluateStars(IndexOf(catalog, scene.buildIndex), true, result.Time)
                : 1;

            stars?.Set(starCount, Theme, animate: true, startDelay: 0.18f);

            if (timeText != null)
            {
                timeText.PlayTime(result.Time, Theme.countUpDuration);
            }

            if (bestLabel != null)
            {
                bestLabel.SetText(result.BestTime > 0f
                    ? $"SECTOR RECORD  {MissionTimeFormatter.Format(result.BestTime)}"
                    : "FIRST RECORDED FLIGHT");
            }

            if (attemptsLabel != null)
            {
                attemptsLabel.SetText($"ATTEMPTS  {result.Attempts}");
            }

            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(result.IsNewRecord);
                if (result.IsNewRecord)
                {
                    UITween.Punch(newRecordBadge.transform, 0.18f, 0.5f);
                }
            }

            if (nextButton != null)
            {
                nextButton.SetLabel(isFinalLevel ? "RETURN TO BASE" : "NEXT SECTOR");
            }

            if (nextHintLabel != null)
            {
                nextHintLabel.SetText(isFinalLevel
                    ? "All sectors secured. Outstanding flying, pilot."
                    : "Continue to the next sector when ready.");
            }

            PlayReveal();
            SelectDefault();
        }

        private void PlayReveal()
        {
            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.86f;
                UITween.Scale(panel, Vector3.one, Theme.durationSlow, UIEase.OutBack);
            }

            if (stars != null)
            {
                UITween.Punch(stars.transform, 0.06f, 0.5f);
            }

            confetti?.Burst(46, Theme);
        }

        private void Advance()
        {
            if (isFinalLevel)
            {
                Manager.OpenMainMenu();
                return;
            }

            Manager.LoadNextLevel();
        }

        private Color RankColor(MissionRank rank)
        {
            switch (rank)
            {
                case MissionRank.S: return Theme.primary;
                case MissionRank.A: return Theme.starGold;
                case MissionRank.B: return Theme.success;
                default: return Theme.textSecondary;
            }
        }

        private static int IndexOf(LevelCatalog catalog, int buildIndex)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelCatalog.LevelDefinition level = catalog.Get(i);
                if (level != null && level.buildIndex == buildIndex)
                {
                    return i;
                }
            }

            return 0;
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
