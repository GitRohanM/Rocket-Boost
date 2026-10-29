using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>Simple breathing glow used on titles, rings and the selected level card.</summary>
    public sealed class UIPulse : MonoBehaviour
    {
        [SerializeField] private float minAlpha = 0.25f;
        [SerializeField] private float maxAlpha = 0.6f;
        [SerializeField] private float speed = 1.6f;

        private Graphic graphic;
        private float phase;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (graphic == null)
            {
                return;
            }

            float sine = (Mathf.Sin(Time.unscaledTime * speed + phase) + 1f) * 0.5f;
            Color color = graphic.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, sine);
            graphic.color = color;
        }
    }
}
