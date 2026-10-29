using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Slow vertical float for logos and hero art (unscaled, so it moves in pause menus too).</summary>
    public sealed class UIFloat : MonoBehaviour
    {
        [SerializeField] private float amplitude = 10f;
        [SerializeField] private float speed = 0.6f;

        private Vector2 home;
        private float phase;

        private void Awake()
        {
            RectTransform rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchoredPosition = home + new Vector2(0f, Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude);
        }
    }
}
