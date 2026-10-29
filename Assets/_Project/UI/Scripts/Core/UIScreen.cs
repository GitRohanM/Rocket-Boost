using System;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Every full-screen view the UI state machine can display.</summary>
    public enum UIScreenType
    {
        MainMenu = 0,
        LevelSelect = 1,
        Hud = 2,
        Pause = 3,
        LevelComplete = 4,
        GameOver = 5,
        Settings = 6,
        Loading = 7
    }

    /// <summary>
    /// Base class for a full-screen UI view. Handles canvas-group visibility, entrance and
    /// exit tweens, default gamepad/keyboard selection and per-screen flags.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private UIScreenType screenType = UIScreenType.MainMenu;

        [Header("Behaviour")]
        [Tooltip("Menu-style screens keep the animated starfield visible behind them.")]
        [SerializeField] private bool showBackdrop = true;

        [Tooltip("Gameplay screens advance the level timer and should not block the HUD unexpectedly.")]
        [SerializeField] private bool gameplayScreen;

        [Tooltip("First control selected when the screen opens (keyboard / gamepad).")]
        [SerializeField] private UnityEngine.UI.Selectable defaultSelection;

        [SerializeField] private float entranceDuration = 0.24f;
        [SerializeField] private float exitDuration = 0.16f;
        [SerializeField] private float entranceOffset = 26f;

        private CanvasGroup canvasGroup;
        private RectTransform content;
        private int visibilityToken;
        private Vector2 contentHome;

        public UIScreenType ScreenType => screenType;
        public bool ShowBackdrop => showBackdrop;
        public bool IsGameplayScreen => gameplayScreen;
        public bool IsVisible { get; private set; }
        public CanvasGroup Group => canvasGroup;

        /// <summary>Theme injected by the manager before the first show.</summary>
        protected Data.UITheme Theme { get; private set; }

        /// <summary>Manager that owns this screen; set during registration.</summary>
        protected UIManager Manager { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            content = transform.childCount > 0 ? transform.GetChild(0) as RectTransform : null;
            if (content != null)
            {
                contentHome = content.anchoredPosition;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(true);
        }

        /// <summary>Called once by <see cref="UIManager"/> after instantiation.</summary>
        public void Initialize(UIManager manager, Data.UITheme theme)
        {
            Manager = manager;
            Theme = theme;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        /// <summary>Shows the screen with its entrance animation.</summary>
        public void Show(bool instant = false)
        {
            IsVisible = true;
            gameObject.SetActive(true);
            int token = ++visibilityToken;

            if (instant)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                if (content != null)
                {
                    content.anchoredPosition = contentHome;
                }

                OnShown();
                SelectDefault();
                return;
            }

            UITween.Run(ShowRoutine(token));
        }

        /// <summary>Hides the screen, optionally keeping the object active (for HUD overlays).</summary>
        public void Hide(bool instant = false)
        {
            IsVisible = false;
            int token = ++visibilityToken;

            if (instant)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                OnHidden();
                gameObject.SetActive(false);
                return;
            }

            UITween.Run(HideRoutine(token));
        }

        private System.Collections.IEnumerator ShowRoutine(int token)
        {
            canvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            if (content != null)
            {
                content.anchoredPosition = contentHome + new Vector2(0f, -entranceOffset);
            }

            while (elapsed < entranceDuration)
            {
                if (token != visibilityToken)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = UITween.Ease(UIEase.OutCubic, Mathf.Clamp01(elapsed / entranceDuration));
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                if (content != null)
                {
                    content.anchoredPosition = Vector2.LerpUnclamped(
                        contentHome + new Vector2(0f, -entranceOffset),
                        contentHome,
                        t);
                }

                yield return null;
            }

            if (token != visibilityToken)
            {
                yield break;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            if (content != null)
            {
                content.anchoredPosition = contentHome;
            }

            OnShown();
            SelectDefault();
        }

        private System.Collections.IEnumerator HideRoutine(int token)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < exitDuration)
            {
                if (token != visibilityToken)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / exitDuration));
                yield return null;
            }

            if (token != visibilityToken)
            {
                yield break;
            }

            canvasGroup.alpha = 0f;
            OnHidden();
            gameObject.SetActive(false);
        }

        /// <summary>Selects the authored default control so gamepad/keyboard navigation always has focus.</summary>
        public void SelectDefault()
        {
            if (defaultSelection == null || !defaultSelection.IsInteractable() || !defaultSelection.gameObject.activeInHierarchy)
            {
                return;
            }

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(defaultSelection.gameObject);
        }

        /// <summary>Called after the entrance animation completes.</summary>
        protected virtual void OnShown()
        {
        }

        /// <summary>Called when the screen has fully faded out.</summary>
        protected virtual void OnHidden()
        {
        }

        /// <summary>Escape / gamepad-B handling. Return true when the screen consumed the input.</summary>
        public virtual bool HandleBack()
        {
            return false;
        }

        /// <summary>True when the screen is allowed to be dismissed by Escape.</summary>
        public virtual bool CanHandleBack => true;

        protected void SetDefaultSelection(UnityEngine.UI.Selectable selectable) => defaultSelection = selectable;
    }
}
