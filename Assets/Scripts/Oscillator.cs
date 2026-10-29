using DG.Tweening;
using UnityEngine;

namespace RocketBoost
{
    /// <summary>
    /// Oscillates an obstacle between its start position and a target offset.
    /// Uses a DOTween loop for buttery interpolation, or ping-pong movement when the
    /// obstacle has a dynamic Rigidbody (tweens do not move dynamic bodies correctly).
    /// </summary>
    [DisallowMultipleComponent]
    public class Oscillator : MonoBehaviour
    {
        [Header("Movement Path")]
        [SerializeField] private Vector3 movementVector = new Vector3(0f, 0f, 10f);

        [Header("Timing")]
        [Tooltip("Oscillation cycle duration in seconds.")]
        [SerializeField] private float duration = 2.5f;

        [Tooltip("Phase offset (normalized 0-1) to desynchronize obstacles.")]
        [Range(0f, 1f)]
        [SerializeField] private float phaseOffset;

        [Header("Interpolation Method")]
        [Tooltip("Tween the transform. Ignored when a dynamic Rigidbody is present.")]
        [SerializeField] private bool useDOTween = true;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Rigidbody rb;
        private Tween moveTween;
        private bool isTweenDriven;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            startPosition = transform.position;
            endPosition = startPosition + movementVector;

            // A dynamic Rigidbody must be moved through the physics system, so a transform
            // tween would fight the simulation. Previously this combination silently never
            // moved at all; now it falls back to physics movement with a warning.
            isTweenDriven = useDOTween && (rb == null || rb.isKinematic);
            if (useDOTween && !isTweenDriven)
            {
                Debug.LogWarning(
                    "[Oscillator] useDOTween is on but the Rigidbody is dynamic. Falling back to physics movement; " +
                    "enable Is Kinematic on the Rigidbody if you want a tween.",
                    this);
            }

            if (!isTweenDriven)
            {
                return;
            }

            moveTween = transform.DOMove(endPosition, Mathf.Max(0.01f, duration) * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Mathf.Max(0.01f, duration) * phaseOffset);
        }

        private void FixedUpdate()
        {
            if (isTweenDriven || rb == null || !rb.isKinematic)
            {
                return;
            }

            rb.MovePosition(Vector3.Lerp(startPosition, endPosition, GetPingPongFactor()));
        }

        private void Update()
        {
            if (isTweenDriven || (rb != null && rb.isKinematic))
            {
                return;
            }

            transform.position = Vector3.Lerp(startPosition, endPosition, GetPingPongFactor());
        }

        private float GetPingPongFactor() =>
            Mathf.PingPong(Time.time / Mathf.Max(0.01f, duration) + phaseOffset, 1f);

        private void OnDestroy() => moveTween?.Kill();
    }
}
