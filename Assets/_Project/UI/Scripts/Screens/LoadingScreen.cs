using System.Collections;
using TMPro;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Loading view: progress bar, percentage readout and rotating mission tips.</summary>
    public sealed class LoadingScreen : UIScreen
    {
        [Header("Progress")]
        [SerializeField] private UIProgressBar progressBar;
        [SerializeField] private TMP_Text percentLabel;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Tips")]
        [SerializeField] private TMP_Text tipLabel;
        [SerializeField] private string[] tips = new string[0];
        [SerializeField] private float tipInterval = 2.6f;

        private Coroutine tipRoutine;
        private int tipIndex;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);
            SetProgress(0f);
        }

        public void Begin()
        {
            if (progressBar != null)
            {
                progressBar.Reset();
            }

            SetProgress(0f);
            tipIndex = Random.Range(0, Mathf.Max(1, tips.Length));
            ShowTip();

            if (tipRoutine != null)
            {
                StopCoroutine(tipRoutine);
            }

            tipRoutine = StartCoroutine(RotateTips());
        }

        public void SetProgress(float normalized)
        {
            float value = Mathf.Clamp01(normalized);

            if (progressBar != null)
            {
                progressBar.Set(value);
            }

            if (percentLabel != null)
            {
                percentLabel.SetText("{0:0}%", Mathf.RoundToInt(value * 100f));
            }

            if (statusLabel != null)
            {
                statusLabel.SetText(value >= 1f ? "READY FOR LAUNCH" : "PREPARING FLIGHT SYSTEMS");
            }
        }

        private IEnumerator RotateTips()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(tipInterval);
                tipIndex = (tipIndex + 1) % Mathf.Max(1, tips.Length);
                ShowTip();
            }
        }

        private void ShowTip()
        {
            if (tipLabel == null || tips.Length == 0)
            {
                return;
            }

            tipLabel.SetText(tips[Mathf.Clamp(tipIndex, 0, tips.Length - 1)]);
            tipLabel.transform.localScale = Vector3.one * 0.98f;
            UITween.Scale(tipLabel.transform, Vector3.one, Theme != null ? Theme.durationNormal : 0.22f, UIEase.OutCubic);
        }

        protected override void OnHidden()
        {
            if (tipRoutine != null)
            {
                StopCoroutine(tipRoutine);
                tipRoutine = null;
            }
        }
    }
}
