using UnityEngine;

public class AutoDestroyAudioSource : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        // Two simple conditions:
        // 1. Audio is not playing
        // 2. SFX are not paused globally
        if (audioSource != null && 
            !audioSource.isPlaying && 
            AudioManager.Instance != null && 
            !AudioManager.Instance.AllSFXPaused)
        {
            Destroy(gameObject);
        }
    }
}