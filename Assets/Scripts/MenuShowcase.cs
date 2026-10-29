using DG.Tweening;
using UnityEngine;

namespace RocketBoost
{
    /// <summary>
    /// Uses DOTween to create a smooth, cinematic floating and rotating showcase effect
    /// for the 3D Rocket in the Main Menu scene.
    /// </summary>
    public class MenuShowcase : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverDistance = 0.35f;
        [SerializeField] private float hoverCycleDuration = 2.4f;

        [Header("Rotation Settings")]
        [SerializeField] private float fullRotationDuration = 20f;

        [Header("Thruster Light")]
        [SerializeField] private Light thrusterLight;
        [SerializeField] private float lightMin = 1.0f;
        [SerializeField] private float lightMax = 3.5f;

        private Tween hoverTween;
        private Tween rotTween;
        private Tween lightTween;

        private void Start()
        {
            Vector3 startPos = transform.position;

            // 1. Silky smooth hover loop
            hoverTween = transform.DOMoveY(startPos.y + hoverDistance, hoverCycleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // 2. Slow orbital rotation
            rotTween = transform.DORotate(new Vector3(0f, 360f, 0f), fullRotationDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);

            // 3. Thruster engine pulse
            if (thrusterLight != null)
            {
                thrusterLight.intensity = lightMin;
                lightTween = thrusterLight.DOIntensity(lightMax, 0.25f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnDestroy()
        {
            hoverTween?.Kill();
            rotTween?.Kill();
            lightTween?.Kill();
        }
    }
}
