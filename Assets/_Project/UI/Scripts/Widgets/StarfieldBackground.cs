using System;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Scrolling starfield built from three tiled raw images. Layers move at different speeds
    /// for depth; movement uses scaled time so the field freezes while the game is paused.
    /// </summary>
    public sealed class StarfieldBackground : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public RawImage image;
            public float speed = 0.01f;
            public Vector2 direction = new Vector2(1f, 0.2f);
            public float sway = 0.004f;
            public float swaySpeed = 0.15f;
        }

        [SerializeField] private Layer[] layers = Array.Empty<Layer>();
        [SerializeField] private float globalSpeed = 1f;

        private Vector2[] offsets;
        private float[] phases;

        private void Awake()
        {
            if (layers == null)
            {
                return;
            }

            offsets = new Vector2[layers.Length];
            phases = new float[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                phases[i] = i * 1.7f;
                offsets[i] = layers[i].image != null ? layers[i].image.uvRect.position : Vector2.zero;
            }
        }

        private void Update()
        {
            if (layers == null || offsets == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                Layer layer = layers[i];
                if (layer.image == null)
                {
                    continue;
                }

                Vector2 dir = layer.direction.sqrMagnitude > 0.0001f ? layer.direction.normalized : Vector2.right;
                float sway = Mathf.Sin(Time.time * layer.swaySpeed + phases[i]) * layer.sway;

                offsets[i] += (dir * layer.speed * globalSpeed + new Vector2(0f, sway)) * Time.deltaTime;
                offsets[i].x = Mathf.Repeat(offsets[i].x, 1f);
                offsets[i].y = Mathf.Repeat(offsets[i].y, 1f);

                Rect uv = layer.image.uvRect;
                uv.position = offsets[i];
                layer.image.uvRect = uv;
            }
        }
    }

    /// <summary>Adds a short positional shake to a RectTransform (used by the crash debrief).</summary>
    public static class UIShake
    {
        public static void Play(RectTransform target, float magnitude, float duration)
        {
            if (target == null)
            {
                return;
            }

            UITween.Shake(target, magnitude, duration);
        }
    }
}
