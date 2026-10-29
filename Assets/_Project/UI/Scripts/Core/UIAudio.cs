using System.Collections;
using RocketBoost.UI.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace RocketBoost.UI
{
    /// <summary>
    /// Single entry point for UI sound. Routes one-shot effects to the SFX mixer group and
    /// the looping menu track to the Music group so the settings sliders affect both.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIAudio : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Mixing")]
        [SerializeField, Range(0f, 1f)] private float gameDuckLevel = 0.45f;
        [SerializeField] private float musicFadeDuration = 1.4f;

        private UITheme theme;
        private UISettingsService settings;
        private AudioMixer mixer;
        private Coroutine musicFade;

        public void Initialize(UITheme uiTheme, UISettingsService settingsService)
        {
            theme = uiTheme;
            settings = settingsService;
            mixer = theme != null ? theme.masterGroup != null ? theme.masterGroup.audioMixer : null : null;

            if (musicSource == null || sfxSource == null)
            {
                CreateSources();
            }

            if (theme != null)
            {
                if (sfxSource != null)
                {
                    sfxSource.outputAudioMixerGroup = theme.sfxGroup;
                }

                if (musicSource != null)
                {
                    musicSource.outputAudioMixerGroup = theme.musicGroup;
                    musicSource.clip = theme.menuMusic;
                }
            }

            settings?.ApplyAudio(mixer);
            settings?.ApplyDisplay();
        }

        private void CreateSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
            }
        }

        public void PlayMenuMusic()
        {
            if (musicSource == null || theme == null || theme.menuMusic == null)
            {
                return;
            }

            if (musicSource.clip != theme.menuMusic)
            {
                musicSource.clip = theme.menuMusic;
            }

            if (musicSource.isPlaying)
            {
                return;
            }

            musicSource.volume = 0f;
            musicSource.Play();
            FadeMusic(1f);
        }

        /// <summary>Lowers the music while flying so engine and UI feedback stay readable.</summary>
        public void DuckMusic(bool ducked) => FadeMusic(ducked ? gameDuckLevel : 1f);

        private void FadeMusic(float target)
        {
            if (musicSource == null)
            {
                return;
            }

            if (musicFade != null)
            {
                StopCoroutine(musicFade);
            }

            musicFade = StartCoroutine(FadeMusicRoutine(target));
        }

        private IEnumerator FadeMusicRoutine(float target)
        {
            float start = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration && musicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(start, target, elapsed / musicFadeDuration);
                yield return null;
            }

            if (musicSource != null)
            {
                musicSource.volume = target;
            }

            musicFade = null;
        }

        public void PlayHover()
        {
            Play(theme != null ? theme.hoverClip : null, 0.55f);
        }

        public void PlayClick()
        {
            Play(theme != null ? theme.clickClip : null, 0.9f);
        }

        public void PlayBack()
        {
            Play(theme != null ? theme.backClip : null, 0.8f);
        }

        public void PlayConfirm()
        {
            Play(theme != null ? theme.confirmClip : null, 0.95f);
        }

        public void PlayLevelComplete()
        {
            Play(theme != null ? theme.levelCompleteClip : null, 1f);
        }

        public void PlayCrash()
        {
            Play(theme != null ? theme.crashClip : null, 1f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
