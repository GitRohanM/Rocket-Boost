using RocketBoost.Core;
using TMPro;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>
    /// Animates a numeric readout. Times use the project's culture-invariant
    /// <see cref="MissionTimeFormatter"/> so the debrief matches the HUD exactly.
    /// </summary>
    public sealed class CountUpText : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;

        private int generation;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<TMP_Text>();
            }
        }

        /// <summary>Shows the final value with no animation.</summary>
        public void SetTime(float seconds)
        {
            generation++;
            if (target != null)
            {
                target.SetText(MissionTimeFormatter.Format(seconds));
            }
        }

        /// <summary>Rolls the displayed time up from zero.</summary>
        public void PlayTime(float seconds, float duration)
        {
            generation++;
            UITween.Run(RollTime(seconds, Mathf.Max(0.01f, duration), generation));
        }

        private System.Collections.IEnumerator RollTime(float seconds, float duration, int token)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (token != generation)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = UITween.Ease(UIEase.OutCubic, Mathf.Clamp01(elapsed / duration));
                if (target != null)
                {
                    target.SetText(MissionTimeFormatter.Format(seconds * t));
                }

                yield return null;
            }

            if (token == generation && target != null)
            {
                target.SetText(MissionTimeFormatter.Format(seconds));
            }
        }
    }
}
