using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] InputAction thrust;
    [SerializeField] InputAction rotation;
    [SerializeField] float thrustStrength = 100f;
    [SerializeField] float rotationStrength = 100f;
    [SerializeField] AudioClip mainEngineSFX;
    [SerializeField] ParticleSystem mainEngineParticles;
    [SerializeField] ParticleSystem leftThrusterParticles;
    [SerializeField] ParticleSystem rightThrusterParticles;

    Rigidbody rb;
    AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        thrust.Enable();
        rotation.Enable();
    }

    private void FixedUpdate()
    {
        ProcessThrust();
        ProcessRotation();
    }

    private void ProcessThrust()
    {
        if (thrust.IsPressed())
        {
           StartThrusting();
        }
        else
        {
            StopThrusting();
        }

    }

    void StartThrusting(){
         rb.AddRelativeForce(Vector3.up * thrustStrength * Time.deltaTime);
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(mainEngineSFX, volumeScale: 2f); 
            }
            if(!mainEngineParticles.isPlaying)
            {
                mainEngineParticles.Play();
            }
    }

    void StopThrusting()
    {
        audioSource.Stop(); 
        mainEngineParticles.Stop();
    }
    void ProcessRotation()
    {
        float rotationInput = rotation.ReadValue<float>();

        if (rotationInput < 0)
        {
            RotateRight();
        }
        else if (rotationInput > 0)
        {
            RotateLeft();
        }
        else{
            StopRotation();
        }
        
    }

    void RotateRight()
    {
        ApplyRotation(rotationStrength);
        if(!rightThrusterParticles.isPlaying)
        {
            leftThrusterParticles.Stop();
            rightThrusterParticles.Play();
        }
    }

    void RotateLeft(){
        ApplyRotation(-rotationStrength);
        if(!leftThrusterParticles.isPlaying)
        {
            rightThrusterParticles.Stop();
            leftThrusterParticles.Play();
        }
    }

    void StopRotation()
    {
        rightThrusterParticles.Stop();
        leftThrusterParticles.Stop();
    }

    private void ApplyRotation(float rotationThisFrame)
    {
        rb.freezeRotation = true; // Freeze rotation to prevent physics from interfering

        //Dynamically rotate the ship
        transform.Rotate(Vector3.forward * rotationThisFrame * Time.deltaTime); 

        rb.freezeRotation = false; // Unfreeze rotation so physics can take over again
    }
}
