using RocketBoost.Core;
using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Sector select: builds one card per catalog entry, each showing lock state, best time
    /// and earned stars. Unlocking is derived from recorded best times.
    /// </summary>
    public sealed class LevelSelectScreen : UIScreen
    {
        [Header("Actions")]
        [SerializeField] private ThemedButton backButton;

        [Header("Layout")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private LevelCard cardPrefab;

        [Header("Copy")]
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text summaryLabel;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            if (backButton != null && backButton.Button != null)
            {
                backButton.Button.onClick.RemoveAllListeners();
                backButton.Button.onClick.AddListener(GoBack);
            }

            SetDefaultSelection(backButton != null ? backButton.Button : null);
        }

        protected override void OnShown()
        {
            Rebuild();
            SetDefaultSelection(backButton != null ? backButton.Button : null);
            SelectDefault();
        }

        public override bool HandleBack()
        {
            GoBack();
            return true;
        }

        private void GoBack()
        {
            Manager.Audio.PlayBack();
            Manager.ShowPrimary(UIScreenType.MainMenu);
        }

        private void Rebuild()
        {
            if (cardContainer == null || cardPrefab == null || Manager == null)
            {
                return;
            }

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }

            LevelCatalog catalog = Manager.Catalog;
            if (catalog == null || catalog.Count == 0)
            {
                return;
            }

            int clearedCount = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelCatalog.LevelDefinition definition = catalog.Get(i);
                if (definition == null)
                {
                    continue;
                }

                bool hasBest = TryGetBestTime(definition.buildIndex, out float best);
                bool unlocked = catalog.IsUnlocked(i, index =>
                {
                    LevelCatalog.LevelDefinition previous = catalog.Get(index);
                    return previous != null && TryGetBestTime(previous.buildIndex, out _);
                });

                if (hasBest)
                {
                    clearedCount++;
                }

                LevelCard card = Instantiate(cardPrefab, cardContainer);
                card.name = $"LevelCard_{i + 1:00}";
                int starCount = catalog.EvaluateStars(i, hasBest, best);
                card.Bind(Theme, i, definition, unlocked, hasBest, best, starCount, OnCardSelected);
            }

            if (progressLabel != null)
            {
                progressLabel.SetText("CLEARED {0} / {1}", clearedCount, catalog.Count);
            }

            if (summaryLabel != null)
            {
                summaryLabel.SetText(clearedCount >= catalog.Count
                    ? "ALL SECTORS SECURED"
                    : "COMPLETE A SECTOR TO UNLOCK THE NEXT");
            }
        }

        private void OnCardSelected(int index)
        {
            LevelCatalog catalog = Manager.Catalog;
            LevelCatalog.LevelDefinition definition = catalog.Get(index);
            if (definition == null)
            {
                return;
            }

            Manager.GoToLevel(definition.buildIndex);
        }

        private static bool TryGetBestTime(int buildIndex, out float bestTime)
        {
            bestTime = 0f;
            return GameManager.Instance != null && GameManager.Instance.TryGetBestTime(buildIndex, out bestTime);
        }
    }
}
