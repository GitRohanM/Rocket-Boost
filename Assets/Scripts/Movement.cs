using UnityEngine;
using UnityEngine.InputSystem;

namespace RocketBoost
{
    /// <summary>
    /// Reads thrust and rotation input and applies it to the rocket's Rigidbody.
    /// Rotation is driven directly on the transform because the Rigidbody has frozen
    /// rotation and no angular velocity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public class Movement : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Hold action bound to the thrust control. Serialized inline as a singleton action.")]
        [SerializeField] private InputAction thrust;

        [Tooltip("1D axis action bound to the rotation controls. Serialized inline as a singleton action.")]
        [SerializeField] private InputAction rotation;

        [Header("Movement Settings")]
        [SerializeField] private float thrustStrength = 100f;
        [SerializeField] private float rotationStrength = 100f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip mainEngineSFX;

        [Header("Visual Particles")]
        [SerializeField] private ParticleSystem mainEngineParticles;
        [SerializeField] private ParticleSystem leftThrusterParticles;
        [SerializeField] private ParticleSystem rightThrusterParticles;

        private Rigidbody rb;
        private AudioSource audioSource;

        /// <summary>True while the thrust action is held.</summary>
        public bool IsThrusting => thrust != null && thrust.enabled && thrust.IsPressed();

        /// <summary>Current speed in metres per second.</summary>
        public float VelocityMagnitude => rb != null ? rb.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();

            if (rb != null)
            {
                rb.freezeRotation = true;
            }

            if (audioSource != null && mainEngineSFX != null)
            {
                audioSource.clip = mainEngineSFX;
                audioSource.loop = true;
            }

            WarnIfInputMissing();
        }

        private void OnEnable()
        {
            thrust?.Enable();
            rotation?.Enable();
        }

        private void OnDisable()
        {
            thrust?.Disable();
            rotation?.Disable();
            StopThrusting();
            StopRotation();
        }

        private void FixedUpdate()
        {
            ProcessThrust();
            ProcessRotation();
        }

        private void WarnIfInputMissing()
        {
            if (thrust == null)
            {
                Debug.LogError(
                    "[Movement] No thrust InputAction assigned. Bind an action to the rocket in the Inspector.",
                    this);
            }

            if (rotation == null)
            {
                Debug.LogError(
                    "[Movement] No rotation InputAction assigned. Bind an action to the rocket in the Inspector.",
                    this);
            }
        }

        private void ProcessThrust()
        {
            if (IsThrusting)
            {
                StartThrusting();
                return;
            }

            StopThrusting();
        }

        private void StartThrusting()
        {
            if (rb != null)
            {
                // Keep the existing force curve: thrustStrength is tuned in the prefab and
                // is applied per fixed step, as in the original arcade feel.
                rb.AddRelativeForce(Vector3.up * thrustStrength * Time.fixedDeltaTime);
            }

            if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }

            if (mainEngineParticles != null && !mainEngineParticles.isPlaying)
            {
                mainEngineParticles.Play();
            }
        }

        private void StopThrusting()
        {
            // Only stop the shared source when the engine loop is what is playing, so
            // crash/success one-shots are not cut off.
            if (audioSource != null && audioSource.isPlaying && audioSource.clip == mainEngineSFX)
            {
                audioSource.Stop();
            }

            if (mainEngineParticles != null && mainEngineParticles.isPlaying)
            {
                mainEngineParticles.Stop();
            }
        }

        private void ProcessRotation()
        {
            if (rotation == null)
            {
                return;
            }

            float rotationInput = rotation.ReadValue<float>();

            if (rotationInput < 0f)
            {
                RotateRight();
            }
            else if (rotationInput > 0f)
            {
                RotateLeft();
            }
            else
            {
                StopRotation();
            }
        }

        private void RotateRight()
        {
            ApplyRotation(rotationStrength);
            PlayThrusterParticles(leftThrusterParticles, rightThrusterParticles);
        }

        private void RotateLeft()
        {
            ApplyRotation(-rotationStrength);
            PlayThrusterParticles(rightThrusterParticles, leftThrusterParticles);
        }

        private void PlayThrusterParticles(ParticleSystem active, ParticleSystem inactive)
        {
            if (active == null || active.isPlaying)
            {
                return;
            }

            if (inactive != null)
            {
                inactive.Stop();
            }

            active.Play();
        }

        private void StopRotation()
        {
            if (rightThrusterParticles != null && rightThrusterParticles.isPlaying)
            {
                rightThrusterParticles.Stop();
            }

            if (leftThrusterParticles != null && leftThrusterParticles.isPlaying)
            {
                leftThrusterParticles.Stop();
            }
        }

        private void ApplyRotation(float rotationThisFrame)
        {
            transform.Rotate(Vector3.forward * rotationThisFrame * Time.fixedDeltaTime);
        }
    }
}
