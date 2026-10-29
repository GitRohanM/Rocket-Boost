using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>0..1 progress bar with a bright leading edge, used by the loading screen.</summary>
    public sealed class UIProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private float width = 760f;
        [SerializeField] private float smoothing = 3.5f;

        private float target;
        private float current;

        public void Set(float normalized, bool instant = false)
        {
            target = Mathf.Clamp01(normalized);
            if (instant)
            {
                current = target;
                Apply();
            }
        }

        /// <summary>Snaps back to empty without easing, used when a new load begins.</summary>
        public void Reset()
        {
            target = 0f;
            current = 0f;
            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(current, target))
            {
                return;
            }

            current = Mathf.MoveTowards(current, target, smoothing * Time.unscaledDeltaTime);
            Apply();
        }

        private void Apply()
        {
            if (fill != null)
            {
                fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * current);
            }
        }
    }
}
