using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace RocketBoost.UI.Data
{
    /// <summary>
    /// Single source of truth for the visual language: palette, typography, spacing,
    /// motion timings, rounded-panel sprites and UI audio. Every screen reads from this
    /// asset instead of hard-coding colors or durations.
    /// </summary>
    [CreateAssetMenu(menuName = "Rocket Boost/UI Theme", fileName = "UITheme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Palette")]
        [ColorUsage(false, true)] public Color background = new Color(0.027f, 0.043f, 0.094f, 1f);
        [ColorUsage(false, true)] public Color backgroundDeep = new Color(0.010f, 0.016f, 0.040f, 1f);
        [ColorUsage(false, true)] public Color surface = new Color(0.055f, 0.082f, 0.149f, 0.92f);
        [ColorUsage(false, true)] public Color surfaceRaised = new Color(0.078f, 0.114f, 0.196f, 0.96f);
        [ColorUsage(false, true)] public Color primary = new Color(0.207f, 0.902f, 1f, 1f);
        [ColorUsage(false, true)] public Color primaryDim = new Color(0.090f, 0.400f, 0.560f, 1f);
        [ColorUsage(false, true)] public Color secondary = new Color(1f, 0.478f, 0.184f, 1f);
        [ColorUsage(false, true)] public Color danger = new Color(1f, 0.302f, 0.369f, 1f);
        [ColorUsage(false, true)] public Color success = new Color(0.357f, 0.894f, 0.608f, 1f);
        [ColorUsage(false, true)] public Color warning = new Color(1f, 0.776f, 0.298f, 1f);
        [ColorUsage(false, true)] public Color textPrimary = new Color(0.925f, 0.957f, 1f, 1f);
        [ColorUsage(false, true)] public Color textSecondary = new Color(0.608f, 0.690f, 0.812f, 1f);
        [ColorUsage(false, true)] public Color textMuted = new Color(0.396f, 0.478f, 0.604f, 1f);
        [ColorUsage(false, true)] public Color border = new Color(0.207f, 0.902f, 1f, 0.35f);
        [ColorUsage(false, true)] public Color borderDim = new Color(0.380f, 0.470f, 0.620f, 0.22f);
        [ColorUsage(false, true)] public Color scrim = new Color(0.004f, 0.007f, 0.020f, 0.78f);
        [ColorUsage(false, true)] public Color starGold = new Color(1f, 0.812f, 0.361f, 1f);
        [ColorUsage(false, true)] public Color starEmpty = new Color(0.180f, 0.227f, 0.318f, 1f);

        [Header("Typography")]
        public TMP_FontAsset displayFont;
        public TMP_FontAsset bodyFont;
        public float displayTracking = 6f;
        public float titleSize = 104f;
        public float headingSize = 40f;
        public float bodySize = 26f;
        public float labelSize = 20f;

        [Header("Spacing Scale (px @1920x1080)")]
        public float spaceXs = 4f;
        public float spaceSm = 8f;
        public float spaceMd = 16f;
        public float spaceLg = 24f;
        public float spaceXl = 40f;
        public float spaceXxl = 64f;
        public float screenMargin = 72f;

        [Header("Corner Radii")]
        public float radiusSm = 10f;
        public float radiusMd = 16f;
        public float radiusLg = 24f;

        [Header("Motion (unscaled seconds)")]
        public float durationFast = 0.12f;
        public float durationNormal = 0.22f;
        public float durationSlow = 0.42f;
        public float durationScreenFade = 0.3f;
        public float countUpDuration = 0.9f;
        public float hoverScale = 1.035f;
        public float pressScale = 0.965f;
        public float stagger = 0.06f;

        [Header("Panels & Buttons")]
        public Sprite panelSprite;
        public Sprite panelDeepSprite;
        public Sprite cardSprite;
        public Sprite buttonNormalSprite;
        public Sprite buttonHoverSprite;
        public Sprite buttonPressedSprite;
        public Sprite buttonSelectedSprite;
        public Sprite buttonBorderSprite;
        public Sprite buttonDisabledSprite;
        public Sprite pillSprite;
        public Sprite gaugeTrackSprite;
        public Sprite gaugeFillSprite;
        public Sprite sliderHandleSprite;
        public Sprite backgroundGradientSprite;

        [Header("Decorations")]
        public Sprite glowSprite;
        public Sprite softDotSprite;
        public Sprite starSprite;
        public Sprite starOutlineSprite;
        public Sprite starSmallSprite;
        public Sprite vignetteSprite;
        public Sprite scanlineSprite;
        public Sprite nebulaSprite;
        public Sprite ringSprite;
        public Sprite logoRimSprite;

        [Header("Icons")]
        public Sprite iconPlay;
        public Sprite iconPause;
        public Sprite iconRestart;
        public Sprite iconSettings;
        public Sprite iconHome;
        public Sprite iconChevronLeft;
        public Sprite iconChevronRight;
        public Sprite iconLock;
        public Sprite iconCheck;
        public Sprite iconVolume;
        public Sprite iconFullscreen;
        public Sprite iconKeyboard;
        public Sprite iconGamepad;
        public Sprite iconStar;

        [Header("UI Audio")]
        public AudioClip hoverClip;
        public AudioClip clickClip;
        public AudioClip backClip;
        public AudioClip confirmClip;
        public AudioClip levelCompleteClip;
        public AudioClip crashClip;
        public AudioClip menuMusic;

        [Header("Audio Mixer Routing")]
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;

        /// <summary>Returns a copy of <paramref name="color"/> with a different alpha.</summary>
        public static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>Mixes two palette entries, useful for subtle gradients built at runtime.</summary>
        public static Color Blend(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));
    }
}
