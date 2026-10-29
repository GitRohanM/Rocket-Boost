using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RocketBoost.UI
{
    /// <summary>
    /// Captions whichever menu row currently holds focus, the way console menus print a line of
    /// help under the list. Polls the EventSystem so mouse, keyboard and gamepad focus all drive it.
    /// </summary>
    public sealed class MenuHint : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float fadeDuration = 0.18f;

        private CanvasGroup group;
        private ThemedButton current;
        private Coroutine routine;

        private void Awake()
        {
            if (label == null)
            {
                return;
            }

            group = label.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = label.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            // Force the next Update to re-read focus: the screen may have selected another row
            // while this caption was hidden.
            current = null;
        }

        private void Update()
        {
            if (label == null || group == null)
            {
                return;
            }

            ThemedButton focused = ResolveFocused();
            if (ReferenceEquals(focused, current))
            {
                return;
            }

            current = focused;
            Show(focused != null ? focused.HintText : string.Empty);
        }

        private ThemedButton ResolveFocused()
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
            {
                return null;
            }

            ThemedButton themed = selected.GetComponentInParent<ThemedButton>();
            return themed != null && !string.IsNullOrEmpty(themed.HintText) ? themed : null;
        }

        private void Show(string text)
        {
            UITween.Stop(ref routine);
            routine = UITween.Run(FadeTo(text));
        }

        private System.Collections.IEnumerator FadeTo(string text)
        {
            float half = Mathf.Max(0.01f, fadeDuration * 0.5f);
            float from = group.alpha;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, 0f, elapsed / half);
                yield return null;
            }

            group.alpha = 0f;
            label.SetText(string.IsNullOrEmpty(text) ? " " : text);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }

            group.alpha = 1f;
            routine = null;
        }
    }
}
