using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>Visual role of a button; drives palette and sprite selection from the theme.</summary>
    public enum UIButtonVariant
    {
        Default,
        Primary,
        Ghost,
        Danger,

        /// <summary>Boxless centred text row: only the label, its glow streak and a scale change read as focus.</summary>
        List
    }

    /// <summary>
    /// Shared button behaviour: themed normal / hover / pressed / selected / disabled states,
    /// scale juice, focus ring and UI click audio. Works with mouse, keyboard and gamepad
    /// because it listens to pointer and selection events.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ThemedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image focusRing;
        [SerializeField] private Image border;
        [SerializeField] private Image selectionBar;
        [SerializeField] private UIButtonVariant variant = UIButtonVariant.Default;
        [SerializeField] private bool playClickSound = true;
        [SerializeField, TextArea] private string hintText;

        private UITheme theme;
        private Color backgroundColor;
        private Color labelColor;
        private Color iconColor;
        private Vector3 baseScale = Vector3.one;
        private bool hovered;
        private bool selected;
        private bool pressed;
        private Coroutine scaleRoutine;
        private Coroutine barRoutine;
        private bool applied;

        public Button Button => button;
        public TMP_Text Label => label;

        /// <summary>Description shown by <see cref="MenuHint"/> while this row holds focus.</summary>
        public string HintText => hintText;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            baseScale = transform.localScale;

            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                if (playClickSound)
                {
                    button.onClick.AddListener(HandleClick);
                }
            }

            Refresh(true);
        }

        private void OnEnable()
        {
            pressed = false;
            hovered = false;
            // The EventSystem may already point at this button when a screen is shown, in which case
            // it never re-sends OnSelect, so read the real selection instead of assuming none.
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            Refresh(true);
        }

        /// <summary>Applies palette, fonts and sprites. Safe to call in edit mode while building prefabs.</summary>
        public void ApplyTheme(UITheme uiTheme)
        {
            if (uiTheme == null)
            {
                return;
            }

            theme = uiTheme;
            applied = true;

            if (label != null)
            {
                bool list = variant == UIButtonVariant.List;
                label.font = list ? uiTheme.displayFont : uiTheme.bodyFont;
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = list ? uiTheme.menuListTracking : 2f;
                if (list)
                {
                    label.fontSize = uiTheme.menuListSize;
                }
            }

            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.disabledColor = Color.white;
                colors.fadeDuration = uiTheme.durationFast;
                button.colors = colors;
            }

            ResolveColors();
            Refresh(true);
        }

        private void ResolveColors()
        {
            if (theme == null)
            {
                return;
            }

            switch (variant)
            {
                case UIButtonVariant.Primary:
                    backgroundColor = theme.primary;
                    labelColor = theme.backgroundDeep;
                    iconColor = theme.backgroundDeep;
                    break;
                case UIButtonVariant.Ghost:
                    backgroundColor = UITheme.Alpha(theme.surface, 0.35f);
                    labelColor = theme.textPrimary;
                    iconColor = theme.primary;
                    break;
                case UIButtonVariant.Danger:
                    backgroundColor = UITheme.Alpha(theme.danger, 0.9f);
                    labelColor = Color.white;
                    iconColor = Color.white;
                    break;
                case UIButtonVariant.List:
                    // Nothing is painted behind the label: the row is pure text plus its streak.
                    backgroundColor = Color.clear;
                    labelColor = theme.textPrimary;
                    iconColor = theme.primary;
                    break;
                default:
                    backgroundColor = theme.surfaceRaised;
                    labelColor = theme.textPrimary;
                    iconColor = theme.primary;
                    break;
            }
        }

        private void HandleClick()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.Audio.PlayClick();
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }

            Refresh(true);
        }

        public void SetLabel(string text)
        {
            if (label != null)
            {
                label.SetText(text);
            }
        }

        public void SetVariant(UIButtonVariant newVariant)
        {
            variant = newVariant;
            ResolveColors();
            Refresh(true);
        }

        private void Refresh(bool instant)
        {
            if (!applied)
            {
                return;
            }

            bool interactable = button != null && button.interactable;
            bool highlight = interactable && (hovered || selected);
            bool focused = interactable && selected;

            if (background != null)
            {
                background.sprite = ResolveSprite(interactable, pressed, highlight);
                Color tint = backgroundColor;

                if (pressed)
                {
                    tint = UITheme.Blend(backgroundColor, theme.backgroundDeep, 0.25f);
                }
                else if (highlight)
                {
                    tint = UITheme.Blend(backgroundColor, theme.primary, variant == UIButtonVariant.Primary ? 0.1f : 0.16f);
                }

                tint.a = interactable ? backgroundColor.a : backgroundColor.a * 0.38f;
                background.color = tint;
            }

            Color foreground = interactable ? (highlight ? theme.primary : labelColor) : UITheme.Alpha(labelColor, 0.35f);
            if (variant == UIButtonVariant.Primary && highlight)
            {
                foreground = theme.backgroundDeep;
            }
            else if (variant == UIButtonVariant.List)
            {
                // Three tiers: the focused row is cyan, a merely hovered row only whitens, idle rows stay dim.
                foreground = !interactable ? UITheme.Alpha(labelColor, 0.35f)
                    : focused ? theme.primary
                    : hovered ? labelColor
                    : UITheme.Alpha(labelColor, 0.62f);
            }

            if (label != null)
            {
                label.color = foreground;
            }

            if (iconImage != null)
            {
                Color iconTint = highlight && variant != UIButtonVariant.Primary ? theme.primary : iconColor;
                iconImage.color = interactable ? iconTint : UITheme.Alpha(iconColor, 0.35f);
            }

            if (border != null)
            {
                border.color = variant == UIButtonVariant.List
                    ? Color.clear
                    : ResolveBorderColor(interactable, highlight);
            }

            if (focusRing != null)
            {
                focusRing.color = variant == UIButtonVariant.List
                    ? Color.clear
                    : UITheme.Alpha(theme.primary, selected ? 0.55f : 0f);
            }

            if (selectionBar != null)
            {
                // The streak is the focus marker; hovering must not light a second one.
                bool lit = focused;
                selectionBar.color = UITheme.Alpha(theme.primary, lit ? 0.92f : 0f);

                Vector3 barTarget = new Vector3(lit ? 1f : 0.42f, 1f, 1f);
                UITween.Stop(ref barRoutine);
                if (instant)
                {
                    selectionBar.transform.localScale = barTarget;
                }
                else
                {
                    barRoutine = UITween.Scale(selectionBar.transform, barTarget, theme.durationFast, UIEase.OutCubic);
                }
            }

            float focusScale = variant == UIButtonVariant.List ? theme.menuSelectedScale : theme.hoverScale;
            bool elevated = variant == UIButtonVariant.List ? focused : highlight;
            Vector3 target = interactable && pressed ? baseScale * theme.pressScale
                : elevated ? baseScale * focusScale
                : baseScale;

            UITween.Stop(ref scaleRoutine);
            if (instant)
            {
                transform.localScale = target;
            }
            else
            {
                scaleRoutine = UITween.Scale(transform, target,
                    highlight ? theme.durationFast : theme.durationNormal, UIEase.OutCubic);
            }
        }

        private Sprite ResolveSprite(bool interactable, bool isPressed, bool highlight)
        {
            if (!interactable)
            {
                return theme.buttonDisabledSprite;
            }

            if (isPressed)
            {
                return theme.buttonPressedSprite;
            }

            return highlight ? theme.buttonHoverSprite : theme.buttonNormalSprite;
        }

        private Color ResolveBorderColor(bool interactable, bool highlight)
        {
            if (!interactable)
            {
                return UITheme.Alpha(theme.borderDim, 0.35f);
            }

            switch (variant)
            {
                case UIButtonVariant.Primary:
                    return UITheme.Alpha(theme.backgroundDeep, highlight ? 0.85f : 0.55f);
                case UIButtonVariant.Danger:
                    return UITheme.Alpha(Color.white, highlight ? 0.7f : 0.35f);
                default:
                    return highlight
                        ? UITheme.Alpha(theme.primary, 0.9f)
                        : UITheme.Alpha(theme.borderDim, 0.75f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (button != null && button.interactable)
            {
                UIManager.Instance?.PlayHover();
            }

            Refresh(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
            Refresh(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.interactable)
            {
                pressed = true;
                Refresh(false);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Refresh(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Refresh(false);
            UIManager.Instance?.PlayHover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
            Refresh(false);
        }
    }
}
