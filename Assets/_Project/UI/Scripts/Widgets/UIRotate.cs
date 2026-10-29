using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Continuous unscaled rotation, used for the loading ring.</summary>
    public sealed class UIRotate : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = -55f;

        private void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
