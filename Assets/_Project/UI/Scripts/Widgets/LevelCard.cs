using RocketBoost.UI.Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>One sector entry in the level-select grid.</summary>
    public sealed class LevelCard : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        [SerializeField] private ThemedButton button;
        [SerializeField] private TMP_Text sectorLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text taglineLabel;
        [SerializeField] private TMP_Text bestTimeLabel;
        [SerializeField] private StarRating stars;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject newRecordBadge;
        [SerializeField] private Image accentBar;
        [SerializeField] private CanvasGroup contentGroup;

        private int levelIndex;
        private Action<int> onSelected;

        public int LevelIndex => levelIndex;
        public ThemedButton Button => button;

        private void Awake()
        {
            if (button != null)
            {
                button.Button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>Populates the card. Called once per level select open.</summary>
        public void Bind(
            UITheme theme,
            int index,
            RocketBoost.UI.Data.LevelCatalog.LevelDefinition level,
            bool unlocked,
            bool hasBestTime,
            float bestTime,
            int starCount,
            Action<int> selected)
        {
            levelIndex = index;
            onSelected = selected;

            button.ApplyTheme(theme);
            button.SetInteractable(unlocked);

            if (sectorLabel != null)
            {
                sectorLabel.SetText(level.sectorCode);
                sectorLabel.color = unlocked ? theme.primary : theme.textMuted;
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(level.displayName);
                nameLabel.color = unlocked ? theme.textPrimary : theme.textMuted;
            }

            if (taglineLabel != null)
            {
                taglineLabel.SetText(level.tagline);
                taglineLabel.color = unlocked ? theme.textSecondary : UITheme.Alpha(theme.textMuted, 0.7f);
            }

            if (bestTimeLabel != null)
            {
                bestTimeLabel.SetText(hasBestTime
                    ? $"BEST {RocketBoost.Core.MissionTimeFormatter.Format(bestTime)}"
                    : "NO RECORD");
                bestTimeLabel.color = hasBestTime ? theme.textPrimary : theme.textMuted;
            }

            if (accentBar != null)
            {
                accentBar.color = unlocked ? theme.primary : theme.borderDim;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!unlocked);
            }

            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(false);
            }

            if (contentGroup != null)
            {
                contentGroup.alpha = unlocked ? 1f : 0.55f;
            }

            stars?.Set(starCount, theme, animate: false);
        }

        private void HandleClick()
        {
            onSelected?.Invoke(levelIndex);
        }

        public void OnSelect(BaseEventData eventData)
        {
            UIManager.Instance?.PlayHover();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            UIManager.Instance?.PlayHover();
        }
    }
}
