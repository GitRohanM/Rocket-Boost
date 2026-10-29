using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Lightweight screen-space confetti. Pieces are plain UI Images animated with unscaled
    /// time, so it reads as particles without needing a particle system in overlay space.
    /// </summary>
    public sealed class UIConfetti : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private Sprite[] shapes = new Sprite[0];
        [SerializeField] private Vector2 pieceSize = new Vector2(14f, 22f);
        [SerializeField] private float lifetime = 2.6f;
        [SerializeField] private float gravity = -1500f;
        [SerializeField] private float spread = 900f;

        public void Burst(int count, Data.UITheme theme)
        {
            if (area == null || count <= 0)
            {
                return;
            }

            Color[] palette =
            {
                theme.primary,
                theme.secondary,
                theme.starGold,
                theme.success,
                theme.textPrimary
            };

            for (int i = 0; i < count; i++)
            {
                GameObject piece = new GameObject("confetti", typeof(RectTransform), typeof(Image));
                piece.transform.SetParent(area, false);

                RectTransform rect = (RectTransform)piece.transform;
                rect.sizeDelta = pieceSize * Random.Range(0.6f, 1.25f);
                rect.anchoredPosition = new Vector2(
                    Random.Range(area.rect.xMin, area.rect.xMax),
                    area.rect.yMax + Random.Range(0f, 80f));
                rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                Image image = piece.GetComponent<Image>();
                image.raycastTarget = false;
                image.sprite = shapes.Length > 0 ? shapes[Random.Range(0, shapes.Length)] : null;
                image.color = palette[Random.Range(0, palette.Length)];

                Vector2 velocity = new Vector2(
                    Random.Range(-spread * 0.35f, spread * 0.35f),
                    Random.Range(-120f, 260f));

                StartCoroutine(AnimatePiece(rect, image, velocity, Random.Range(-320f, 320f), Random.Range(1.6f, lifetime)));
            }
        }

        private System.Collections.IEnumerator AnimatePiece(
            RectTransform rect,
            Image image,
            Vector2 velocity,
            float angularSpeed,
            float pieceLifetime)
        {
            float elapsed = 0f;

            while (elapsed < pieceLifetime && rect != null)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;

                velocity.y += gravity * dt;
                rect.anchoredPosition += velocity * dt;
                rect.Rotate(0f, 0f, angularSpeed * dt);

                if (image != null)
                {
                    Color color = image.color;
                    color.a = Mathf.Clamp01(1f - Mathf.InverseLerp(pieceLifetime * 0.55f, pieceLifetime, elapsed));
                    image.color = color;
                }

                yield return null;
            }

            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }
    }
}
