using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>Full-screen vignette that can flash red on a crash and settle back to a base tint.</summary>
    public sealed class VignetteFlash : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Color baseColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private float flashAlpha = 0.85f;

        private Coroutine routine;

        private void Awake()
        {
            if (image != null && image.color.a <= 0.001f)
            {
                image.color = baseColor;
            }
        }

        public void SetBase(Color color)
        {
            baseColor = color;
            if (image != null && routine == null)
            {
                image.color = baseColor;
            }
        }

        /// <summary>Quick pulse in <paramref name="flashColor"/> then a fade back to the base tint.</summary>
        public void Flash(Color flashColor, float duration)
        {
            if (image == null)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(FlashRoutine(flashColor, duration));
        }

        private System.Collections.IEnumerator FlashRoutine(Color flashColor, float duration)
        {
            Color start = baseColor;
            Color peak = flashColor;
            peak.a = flashAlpha;

            float elapsed = 0f;
            float inTime = duration * 0.25f;
            float outTime = duration * 0.75f;

            while (elapsed < inTime)
            {
                elapsed += Time.unscaledDeltaTime;
                image.color = Color.Lerp(start, peak, elapsed / inTime);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < outTime)
            {
                elapsed += Time.unscaledDeltaTime;
                image.color = Color.Lerp(peak, baseColor, elapsed / outTime);
                yield return null;
            }

            image.color = baseColor;
            routine = null;
        }
    }
}
