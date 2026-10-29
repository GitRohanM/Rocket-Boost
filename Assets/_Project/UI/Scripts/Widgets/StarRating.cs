using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Three-star rating row used on level cards and the debrief. Stars can animate in one by
    /// one (pop-in) and always show empty pips for unearned stars so the goal stays legible.
    /// </summary>
    public sealed class StarRating : MonoBehaviour
    {
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private float emptyScale = 0.72f;
        [SerializeField] private float popDelay = 0.14f;

        private CanvasGroup group;

        public int Count => stars != null ? stars.Length : 0;

        private void Awake()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        /// <summary>Applies a rating immediately. Stars above <paramref name="value"/> stay empty.</summary>
        public void Set(int value, UITheme theme, bool animate, float startDelay = 0f)
        {
            if (stars == null || theme == null)
            {
                return;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null)
                {
                    continue;
                }

                bool earned = i < value;
                star.sprite = earned ? theme.starSprite : theme.starOutlineSprite;
                star.color = earned ? theme.starGold : theme.starEmpty;
                star.transform.localScale = Vector3.one;

                if (!animate)
                {
                    star.transform.localScale = earned ? Vector3.one : Vector3.one * emptyScale;
                }
            }

            if (!animate)
            {
                return;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null || i >= value)
                {
                    continue;
                }

                star.transform.localScale = Vector3.zero;
                float delay = startDelay + i * popDelay;
                UITween.Run(PopRoutine(star.transform, delay, theme.durationSlow));
            }
        }

        private static System.Collections.IEnumerator PopRoutine(Transform target, float delay, float duration)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (target == null)
            {
                yield break;
            }

            Vector3 startScale = Vector3.zero;
            float t = 0f;
            while (t < duration && target != null)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / duration);
                target.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, UITween.Ease(UIEase.OutBack, progress));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }
    }

}
