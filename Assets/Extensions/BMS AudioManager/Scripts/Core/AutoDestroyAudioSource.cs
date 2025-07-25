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
            Destroy(gameObject); // todo might add annother fade here so its not cut off instantly
        }
    }
    
    public void SetPausedStatus(bool paused)
    {
        isPausedByManager = paused;
    }
}