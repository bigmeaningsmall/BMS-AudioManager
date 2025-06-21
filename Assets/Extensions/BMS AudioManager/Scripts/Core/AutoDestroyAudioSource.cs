using UnityEngine;

public class AutoDestroyAudioSource : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isPausedByManager = false;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        // Only destroy if stopped AND not paused by manager
        if (audioSource != null && !audioSource.isPlaying && !isPausedByManager)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetPausedStatus(bool paused)
    {
        isPausedByManager = paused;
    }
}