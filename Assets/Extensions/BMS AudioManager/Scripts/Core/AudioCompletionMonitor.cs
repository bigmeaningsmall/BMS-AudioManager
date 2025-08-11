
using UnityEngine;

public class AudioCompletionMonitor : MonoBehaviour
{
    private AudioTrack parentTrack;
    private AudioSource targetSource;
    private bool hasNotifiedCompletion = false;
    
    public void Initialize(AudioTrack track, AudioSource source)
    {
        parentTrack = track;
        targetSource = source;
        Debug.Log($"[AudioMonitor] Monitoring source: {source.name}");
    }
    
    private void Update()
    {
        // Skip if already notified or missing references
        if (hasNotifiedCompletion || targetSource == null || parentTrack == null)
        {
            return;
        }
        
        // Skip if no clip or is looped
        if (targetSource.clip == null || targetSource.loop)
        {
            return;
        }
        
        // Check if audio has reached the end naturally
        if (targetSource.isPlaying && targetSource.time >= targetSource.clip.length - 0.1f)
        {
            Debug.Log($"[AudioMonitor] Audio reached end: {targetSource.clip.name}");
            hasNotifiedCompletion = true;
            parentTrack.OnAudioCompleted(targetSource);
            Destroy(this);
        }
        
        // Also check if stopped playing (but only after it was playing)
        if (!targetSource.isPlaying && targetSource.time > 0.1f)
        {
            Debug.Log($"[AudioMonitor] Audio stopped after playing: {targetSource.clip.name}");
            hasNotifiedCompletion = true;
            parentTrack.OnAudioCompleted(targetSource);
            Destroy(this);
        }
    }
}