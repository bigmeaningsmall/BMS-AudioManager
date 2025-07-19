using System.Collections;
using UnityEngine;

public enum AmbientState
{
    Stopped,        // No audio playing
    Playing,        // Main source playing normally
    Paused,         // Main source paused
    FadingIn,       // Main or cue source fading in
    FadingOut,      // Outgoing source fading out  
    Crossfading,    // Cue fading in, outgoing fading out
}

public class AmbientAudioTrack : MonoBehaviour
{
    [Header("Audio Sources (3-Source System)")]
    [SerializeField] private AudioSource mainSource;      // Currently playing at target volume
    [SerializeField] private AudioSource cueSource;       // Incoming audio (fading in)
    [SerializeField] private AudioSource outgoingSource;  // Audio being faded out
    
    private Coroutine mainCoroutine;  // Add this field at the top
    
    // Add these properties for editor debugging
#if UNITY_EDITOR
    public AudioSource MainSource => mainSource;
    public AudioSource CueSource => cueSource;
    public AudioSource OutgoingSource => outgoingSource;
#endif
    
    [Header("State")]
    [SerializeField] private AmbientState currentState = AmbientState.Stopped;
    
    // Track settings (preserved for pause/resume)
    private float targetVolume = 1f;
    private float targetPitch = 1f;
    private float currentSpatialBlend = 0f;
    private bool isLooping = true;
    
    // Active coroutines
    private Coroutine cueCoroutine;      // Handles cue fade in
    private Coroutine outgoingCoroutine; // Handles outgoing fade out
    
    // Reference to AudioManager for resources
    private AudioManager audioManager;
    
    private void Awake()
    {
        audioManager = GetComponentInParent<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("AmbientAudioTrack must be a child of AudioManager!");
        }
    }
    
    /// <summary>
    /// Play method with 3-source safety system
    /// </summary>
    public void Play(int trackNumber, string trackName, float volume, float pitch, float spatialBlend, 
                     FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        Debug.Log($"[AmbientTrack] Play called - Current State: {currentState}, FadeType: {fadeType}");
        
        // Store target settings
        targetVolume = volume;
        targetPitch = pitch;
        currentSpatialBlend = spatialBlend;
        isLooping = loop;
        
        // Get the audio clip
        AudioClip clip = ResolveAudioClip(trackNumber, trackName);
        if (clip == null)
        {
            Debug.LogError($"AmbientAudioTrack: Could not find clip for track {trackNumber}/{trackName}");
            return;
        }
        
        // Handle based on current state
        switch (currentState)
        {
            case AmbientState.Stopped:
                // Simple case - just start playing
                StartFromStopped(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
                break;
                
            case AmbientState.Playing:
                // Need to transition - check fade type
                if (fadeType == FadeType.Crossfade)
                {
                    StartCrossfade(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
                }
                else
                {
                    StartFadeOutIn(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
                }
                break;
                
            case AmbientState.Crossfading:
                // Already crossfading - use 3-source safety
                HandlePlayDuringCrossfade(clip, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
                break;
                
            case AmbientState.FadingIn:
                // ALWAYS use 3-source safety when transitioning during any fade
                if (fadeType == FadeType.Crossfade)
                {
                    HandlePlayDuringCrossfade(clip, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
                }
                else
                {
                    StartFadeOutIn(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
                }
                break;
                
            case AmbientState.Paused:
                Debug.LogWarning("Cannot play while paused. Resume or stop first.");
                break;
                
            case AmbientState.FadingOut:
                // Let fade out complete, then start new
                Debug.Log("Currently fading out. New play request will start after fade completes.");
                break;
        }
    }
    
    // ==================== 3-SOURCE SAFETY METHODS ====================
    
private void HandlePlayDuringCrossfade(AudioClip clip, float volume, float pitch, float spatialBlend,
    FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
{
    Debug.Log("[AmbientTrack] Handling Play during crossfade - 3-source safety engaged");

    // Store current outgoing source fade values for inheritance
    float inheritVolume = 0f;
    float inheritPitch = 0f;
    bool hasInheritance = false;

    if (outgoingSource != null)
    {
        // Capture current fade values from the outgoing source
        inheritVolume = outgoingSource.volume;
        inheritPitch = outgoingSource.pitch;
        hasInheritance = true;

        // Clean up existing outgoing source
        if (outgoingCoroutine != null)
        {
            StopCoroutine(outgoingCoroutine);
            outgoingCoroutine = null;
        }
        outgoingSource.Stop();
        Destroy(outgoingSource.gameObject);
        outgoingSource = null;
    }

    // Stop the current cue fade
    if (cueCoroutine != null)
    {
        StopCoroutine(cueCoroutine);
        cueCoroutine = null;
    }

    // Move current cue to outgoing (it will fade out)
    if (cueSource != null)
    {
        outgoingSource = cueSource;
        cueSource = null;

        // Apply inherited values if available
        if (hasInheritance)
        {
            outgoingSource.volume = inheritVolume;
            outgoingSource.pitch = inheritPitch;
        }

        // Start fade out on the demoted cue (from its current/inherited values)
        outgoingCoroutine = StartCoroutine(FadeOutAndDestroy(outgoingSource, fadeDuration, fadeTarget));
    }
    // If no cue but there's a main, move main to outgoing
    else if (mainSource != null)
    {
        outgoingSource = mainSource;
        mainSource = null;
        outgoingCoroutine = StartCoroutine(FadeOutAndDestroy(outgoingSource, fadeDuration, fadeTarget));
    }

    // Create new cue source for the new track
    cueSource = CreateAudioSource(attachTo);
    if (cueSource == null) return;

    cueSource.clip = clip;
    cueSource.loop = loop;
    cueSource.spatialBlend = spatialBlend;
    cueSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
    cueSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
    cueSource.Play();

    // Start fade in on new cue
    currentState = AmbientState.Crossfading;
    cueCoroutine = StartCoroutine(FadeInCue(fadeDuration, fadeTarget, volume, pitch));
}
    
    // ==================== STATE TRANSITION METHODS ====================
    
    private void StartFromStopped(AudioClip clip, float volume, float pitch, float spatialBlend,
        float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        mainSource = CreateAudioSource(attachTo);
        if (mainSource == null) return;
        
        mainSource.clip = clip;
        mainSource.loop = loop;
        mainSource.spatialBlend = spatialBlend;
        
        if (fadeTarget == FadeTarget.Ignore || fadeDuration <= 0)
        {
            // Instant play
            mainSource.volume = volume;
            mainSource.pitch = pitch;
            mainSource.Play();
            currentState = AmbientState.Playing;
            mainCoroutine = StartCoroutine(FadeInMain(fadeDuration, fadeTarget, volume, pitch)); // BUG: This shouldn't run!
        }
        else
        {
            // Fade in
            mainSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
            mainSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
            mainSource.Play();
            
            currentState = AmbientState.FadingIn;
            StartCoroutine(FadeInMain(fadeDuration, fadeTarget, volume, pitch));
        }
    }
    
    private void StartCrossfade(AudioClip clip, float volume, float pitch, float spatialBlend,
        float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        Debug.Log("[AmbientTrack] Starting crossfade - enforcing 3-source limit");

        // CRITICAL: Clean up any existing outgoing source first
        if (outgoingSource != null)
        {
            if (outgoingCoroutine != null)
            {
                StopCoroutine(outgoingCoroutine);
                outgoingCoroutine = null;
            }
            outgoingSource.Stop();
            Destroy(outgoingSource.gameObject);
            outgoingSource = null;
        }

        // CRITICAL: Stop and clean up any existing cue source
        if (cueSource != null)
        {
            if (cueCoroutine != null)
            {
                StopCoroutine(cueCoroutine);
                cueCoroutine = null;
            }
            cueSource.Stop();
            Destroy(cueSource.gameObject);
            cueSource = null;
        }

        // Move main to outgoing (if exists)
        if (mainSource != null)
        {
            outgoingSource = mainSource;
            mainSource = null;
            // Start fade out on the outgoing source
            outgoingCoroutine = StartCoroutine(FadeOutAndDestroy(outgoingSource, fadeDuration, fadeTarget));
        }

        // Create new cue source
        cueSource = CreateAudioSource(attachTo);
        if (cueSource == null) return;

        cueSource.clip = clip;
        cueSource.loop = loop;
        cueSource.spatialBlend = spatialBlend;

        // Set initial fade values
        cueSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
        cueSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
        cueSource.Play();

        currentState = AmbientState.Crossfading;

        // Start fade in on new cue
        cueCoroutine = StartCoroutine(FadeInCue(fadeDuration, fadeTarget, volume, pitch));
    }
    
    private void StartFadeOutIn(AudioClip clip, float volume, float pitch, float spatialBlend,
                                float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        // For fade out/in, we'll handle it differently than crossfade
        // This is a TODO for now - focusing on crossfade safety first
        Debug.Log("[AmbientTrack] Fade Out/In not yet implemented with 3-source system");
    }
    
    // ==================== FADE COROUTINES ====================
    
    private IEnumerator FadeInMain(float duration, FadeTarget fadeTarget, float targetVol, float targetPit)
    {
        float elapsed = 0f;
        float startVol = mainSource.volume;
        float startPit = mainSource.pitch;
        
        while (elapsed < duration && mainSource != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = Mathf.Lerp(startVol, targetVol, t);
                
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = Mathf.Lerp(startPit, targetPit, t);
                
            yield return null;
        }
        
        // Ensure final values
        if (mainSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = targetVol;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = targetPit;
        }
        
        currentState = AmbientState.Playing;
    }
    
    private IEnumerator FadeInCue(float duration, FadeTarget fadeTarget, float targetVol, float targetPit)
    {
        float elapsed = 0f;
        float startVol = cueSource.volume;
        float startPit = cueSource.pitch;
        
        while (elapsed < duration && cueSource != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                cueSource.volume = Mathf.Lerp(startVol, targetVol, t);
                
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                cueSource.pitch = Mathf.Lerp(startPit, targetPit, t);
                
            yield return null;
        }
        
        // Ensure final values and promote cue to main
        if (cueSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                cueSource.volume = targetVol;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                cueSource.pitch = targetPit;
                
            // Promote cue to main
            mainSource = cueSource;
            cueSource = null;
            currentState = AmbientState.Playing;
        }
        
        cueCoroutine = null;
    }
    
    private IEnumerator FadeOutAndDestroy(AudioSource source, float duration, FadeTarget fadeTarget)
    {
        if (source == null) yield break;
        
        float elapsed = 0f;
        float startVol = source.volume;
        float startPit = source.pitch;
        
        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                source.volume = Mathf.Lerp(startVol, 0f, t);
                
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                source.pitch = Mathf.Lerp(startPit, 0f, t);
                
            yield return null;
        }
        
        // Destroy the source
        if (source != null)
        {
            source.Stop();
            Destroy(source.gameObject);
            
            // Clear reference if it's our outgoing source
            if (source == outgoingSource)
            {
                outgoingSource = null;
                outgoingCoroutine = null;
            }
        }
    }
    
    // ==================== HELPER METHODS ====================
    
    private AudioClip ResolveAudioClip(int trackNumber, string trackName)
    {
        if (!string.IsNullOrEmpty(trackName))
        {
            AudioClip clip = audioManager.GetAmbientClip(trackName);
            if (clip != null) return clip;
        }
        
        if (trackNumber >= 0)
        {
            return audioManager.GetAmbientClip(trackNumber);
        }
        
        return null;
    }
    
    private AudioSource CreateAudioSource(Transform attachTo = null)
    {
        GameObject prefab = audioManager.GetAmbientPrefab();
        if (prefab == null)
        {
            Debug.LogError("No ambient audio prefab set in AudioManager!");
            return null;
        }
        
        Transform parent = attachTo ?? transform;
        GameObject audioObj = Instantiate(prefab, parent.position, Quaternion.identity, parent);
        return audioObj.GetComponent<AudioSource>();
    }
    
    // ==================== PUBLIC PROPERTIES ====================
    
    public AmbientState CurrentState => currentState;
    public bool IsPlaying => mainSource != null && mainSource.isPlaying;
    public bool IsCrossfading => currentState == AmbientState.Crossfading;
    
    // Debug helpers---------------------------------------------------------
    public string DebugInfo => $"State: {currentState}, Main: {(mainSource != null)}, Cue: {(cueSource != null)}, Outgoing: {(outgoingSource != null)}";
    
    
}