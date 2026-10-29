using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Settings modal: master / music / SFX volume, quality, fullscreen and a controls
    /// reference. Changes apply live and persist to PlayerPrefs when the screen closes.
    /// </summary>
    public sealed class SettingsScreen : UIScreen
    {
        [Header("Audio")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text masterValue;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sfxValue;

        [Header("Display")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Text fullscreenValue;

        [Header("Chrome")]
        [SerializeField] private ThemedButton backButton;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text qualityHint;

        private bool suppressCallbacks;

        protected override void OnInitialize()
        {
            UIThemeBinder.Bind(gameObject, Theme);

            if (backButton != null && backButton.Button != null)
            {
                backButton.Button.onClick.RemoveAllListeners();
                backButton.Button.onClick.AddListener(Close);
            }

            PopulateQuality();
            BindSliders();

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveAllListeners();
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            SetDefaultSelection(backButton != null ? backButton.Button : null);
        }

        protected override void OnShown()
        {
            SyncFromSettings();

            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.94f;
                UITween.Scale(panel, Vector3.one, Theme.durationNormal, UIEase.OutBack);
            }

            SelectDefault();
        }

        protected override void OnHidden()
        {
            Manager?.Settings.Save();
        }

        public override bool HandleBack()
        {
            Close();
            return true;
        }

        private void Close()
        {
            Manager.Audio.PlayBack();
            Manager.CloseOverlay();
        }

        private void PopulateQuality()
        {
            if (qualityDropdown == null)
            {
                return;
            }

            List<string> options = new List<string>(QualitySettings.names);
            if (options.Count == 0)
            {
                options.Add("Default");
            }

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(options);
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        private void BindSliders()
        {
            BindSlider(masterSlider, OnMasterChanged);
            BindSlider(musicSlider, OnMusicChanged);
            BindSlider(sfxSlider, OnSfxChanged);
        }

        private void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                if (!suppressCallbacks)
                {
                    callback(value);
                }
            });
        }

        private void SyncFromSettings()
        {
            suppressCallbacks = true;

            UISettingsService settings = Manager.Settings;

            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(settings.Master);
            }

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(settings.Music);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(settings.Sfx);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.SetValueWithoutNotify(
                    Mathf.Clamp(settings.QualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1)));
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(settings.Fullscreen);
            }

            ApplyFullscreenVisual(settings.Fullscreen);

            if (qualityHint != null)
            {
                qualityHint.SetText("APPLIED IMMEDIATELY");
            }

            RefreshValueLabels();
            suppressCallbacks = false;
        }

        private void ApplyFullscreenVisual(bool isOn)
        {
            if (fullscreenValue != null)
            {
                fullscreenValue.SetText(isOn ? "ON" : "OFF");
            }

            if (fullscreenToggle != null && fullscreenToggle.graphic != null)
            {
                fullscreenToggle.graphic.canvasRenderer.SetAlpha(isOn ? 1f : 0f);
            }
        }

        private void RefreshValueLabels()
        {
            UISettingsService settings = Manager.Settings;

            if (masterValue != null)
            {
                masterValue.SetText("{0:0}%", Mathf.RoundToInt(settings.Master * 100f));
            }

            if (musicValue != null)
            {
                musicValue.SetText("{0:0}%", Mathf.RoundToInt(settings.Music * 100f));
            }

            if (sfxValue != null)
            {
                sfxValue.SetText("{0:0}%", Mathf.RoundToInt(settings.Sfx * 100f));
            }
        }

        private void OnMasterChanged(float value)
        {
            Manager.Settings.Master = value;
            ApplyAudio();
            RefreshValueLabels();
        }

        private void OnMusicChanged(float value)
        {
            Manager.Settings.Music = value;
            ApplyAudio();
            RefreshValueLabels();
        }

        private void OnSfxChanged(float value)
        {
            Manager.Settings.Sfx = value;
            ApplyAudio();
            RefreshValueLabels();
            Manager.Audio.PlayHover();
        }

        private void ApplyAudio() => Manager.Settings.ApplyAudio(Manager.Theme != null && Manager.Theme.masterGroup != null
            ? Manager.Theme.masterGroup.audioMixer
            : null);

        private void OnQualityChanged(int index)
        {
            if (suppressCallbacks)
            {
                return;
            }

            Manager.Settings.QualityLevel = index;
            Manager.Settings.ApplyDisplay();
        }

        private void OnFullscreenChanged(bool isOn)
        {
            if (suppressCallbacks)
            {
                return;
            }

            Manager.Settings.Fullscreen = isOn;
            Manager.Settings.ApplyDisplay();
            ApplyFullscreenVisual(isOn);
        }
    }
}
