using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] float delay = 2f;
    [SerializeField] AudioClip successSFX;
    [SerializeField] AudioClip crashSFX;
    [SerializeField] ParticleSystem successParticles;
    [SerializeField] ParticleSystem crashParticles;

    AudioSource audioSource;

    bool isControllable = true;
    bool isCollidable = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        RespondToDebugKeys();
    }

    void RespondToDebugKeys(){
        if(Keyboard.current.lKey.wasPressedThisFrame){
            NextLevel();
        }
        else if(Keyboard.current.cKey.wasPressedThisFrame){
            isCollidable = !isCollidable;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isControllable || !isCollidable) return;

        switch (collision.gameObject.tag)
        {
            case "Friendly":
                Debug.Log("Everything is looking good!");
                break;
            case "Finish":
                StartNextLevelSequence();
                break;
            default:
                StartCrashSequence();
                break;
        }
    }

    void StartNextLevelSequence()
    {
        isControllable = false;
        successParticles.Play();
        audioSource.Stop(); // Stop any current audio
        audioSource.PlayOneShot(successSFX);
        GetComponent<Movement>().enabled = false;
        Invoke("NextLevel", delay);
    }

    void StartCrashSequence()
    {
        isControllable = false;
        crashParticles.Play();
        audioSource.Stop(); // Stop any current audio
        audioSource.PlayOneShot(crashSFX);
        GetComponent<Movement>().enabled = false;
        Invoke("ReloadLevel", delay);
    }

    void NextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex == SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0;
        }
        
        SceneManager.LoadScene(nextSceneIndex);
    }

    void ReloadLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}
