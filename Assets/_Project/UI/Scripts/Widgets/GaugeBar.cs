using RocketBoost.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Horizontal meter with an animated fill, used for the boost gauge and speed readout.
    /// The fill colour is interpolated from the theme palette as the value rises.
    /// </summary>
    public sealed class GaugeBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image track;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private float smoothing = 10f;
        [SerializeField] private bool showPercent = true;

        private UITheme theme;
        private float target;
        private float current;

        public float Value => current;

        public void ApplyTheme(UITheme uiTheme)
        {
            theme = uiTheme;
            if (track != null && theme.gaugeTrackSprite != null)
            {
                track.sprite = theme.gaugeTrackSprite;
                track.type = Image.Type.Sliced;
                track.color = theme.borderDim;
            }

            if (fill != null && theme.gaugeFillSprite != null)
            {
                fill.sprite = theme.gaugeFillSprite;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
            }

            UpdateVisual();
        }

        private void OnEnable()
        {
            if (Mathf.Approximately(current, target))
            {
                current = target;
            }
        }

        /// <summary>Sets the 0..1 target value; the bar eases towards it every frame.</summary>
        public void SetValue(float normalized, bool instant = false)
        {
            target = Mathf.Clamp01(normalized);
            if (instant)
            {
                current = target;
                UpdateVisual();
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(current, target))
            {
                return;
            }

            current = Mathf.MoveTowards(current, target, smoothing * Time.unscaledDeltaTime);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (fill != null)
            {
                fill.fillAmount = current;
                if (theme != null)
                {
                    fill.color = Color.Lerp(theme.primary, theme.secondary, Mathf.Clamp01(current));
                }
            }

            if (valueLabel != null)
            {
                valueLabel.SetText(showPercent ? "{0:0}%" : "{0:0}", current * 100f);
            }
        }
    }
}
