using System;
using UnityEngine;
using UnityEngine.Audio;

namespace RocketBoost.UI
{
    /// <summary>
    /// Persists audio, graphics and display preferences in PlayerPrefs and applies them to
    /// the AudioMixer, QualitySettings and the window. Values are linear 0..1 in the UI and
    /// converted to decibels only at the mixer boundary.
    /// </summary>
    [Serializable]
    public sealed class UISettingsService
    {
        public const string MasterKey = "rb.settings.master";
        public const string MusicKey = "rb.settings.music";
        public const string SfxKey = "rb.settings.sfx";
        public const string QualityKey = "rb.settings.quality";
        public const string FullscreenKey = "rb.settings.fullscreen";

        public const float DefaultMaster = 0.85f;
        public const float DefaultMusic = 0.55f;
        public const float DefaultSfx = 0.8f;
        private const float MinDb = -80f;

        [SerializeField, Range(0f, 1f)] private float master = DefaultMaster;
        [SerializeField, Range(0f, 1f)] private float music = DefaultMusic;
        [SerializeField, Range(0f, 1f)] private float sfx = DefaultSfx;
        [SerializeField] private int qualityLevel;
        [SerializeField] private bool fullscreen = true;

        /// <summary>Raised whenever a value changes so open UI can refresh.</summary>
        public event Action Changed;

        public float Master
        {
            get => master;
            set => SetVolume(ref master, Mathf.Clamp01(value));
        }

        public float Music
        {
            get => music;
            set => SetVolume(ref music, Mathf.Clamp01(value));
        }

        public float Sfx
        {
            get => sfx;
            set => SetVolume(ref sfx, Mathf.Clamp01(value));
        }

        public int QualityLevel
        {
            get => qualityLevel;
            set
            {
                int clamped = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                if (qualityLevel == clamped)
                {
                    return;
                }

                qualityLevel = clamped;
                Changed?.Invoke();
            }
        }

        public bool Fullscreen
        {
            get => fullscreen;
            set
            {
                if (fullscreen == value)
                {
                    return;
                }

                fullscreen = value;
                ApplyDisplay();
                Changed?.Invoke();
            }
        }

        private void SetVolume(ref float field, float value)
        {
            if (Mathf.Approximately(field, value))
            {
                return;
            }

            field = value;
            Changed?.Invoke();
        }

        /// <summary>Reads persisted preferences, falling back to design defaults.</summary>
        public void Load()
        {
            master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultMaster));
            music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultMusic));
            sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultSfx));
            qualityLevel = Mathf.Clamp(
                PlayerPrefs.GetInt(QualityKey, QualitySettings.names.Length - 1),
                0,
                Mathf.Max(0, QualitySettings.names.Length - 1));
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        }

        /// <summary>Writes every preference to disk.</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, master);
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.SetFloat(SfxKey, sfx);
            PlayerPrefs.SetInt(QualityKey, qualityLevel);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Pushes volumes into the mixer's exposed parameters (linear to dB).</summary>
        public void ApplyAudio(AudioMixer mixer)
        {
            if (mixer == null)
            {
                return;
            }

            mixer.SetFloat(AudioParams.MasterVolume, LinearToDecibels(master));
            mixer.SetFloat(AudioParams.MusicVolume, LinearToDecibels(music));
            mixer.SetFloat(AudioParams.SfxVolume, LinearToDecibels(sfx));
        }

        /// <summary>Applies quality level and fullscreen mode to the running player.</summary>
        public void ApplyDisplay()
        {
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }

            if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }
        }

        public static float LinearToDecibels(float linear) =>
            linear <= 0.0001f ? MinDb : Mathf.Clamp(Mathf.Log10(linear) * 20f, MinDb, 0f);
    }

    /// <summary>Exposed AudioMixer parameter names shared by the mixer builder and runtime.</summary>
    public static class AudioParams
    {
        public const string MasterVolume = "MasterVolume";
        public const string MusicVolume = "MusicVolume";
        public const string SfxVolume = "SfxVolume";
    }
}
