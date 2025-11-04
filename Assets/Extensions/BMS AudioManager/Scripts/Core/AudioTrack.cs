using System;
using System.Collections;
using UnityEngine;

// public enum AudioTrackType
// {
//     BGM,
//     Ambient, 
//     Dialogue
// }

public enum AudioTrackState
{
    Stopped,        // No audio playing
    Playing,        // Main source playing normally
    Paused,         // Main source paused
    FadingIn,       // Main or cue source fading in
    FadingOut,      // Outgoing source fading out  
    Crossfading,    // Cue fading in, outgoing fading out
    AdjustingParameters,       // Updating parameters (volume/pitch) during Playing or FadingIn
    FadeToPause,    // Fading out to pause
    FadeFromPause   // Fading in from pause
}

public class AudioTrack : MonoBehaviour
{
    // Add this property to AudioTrack class for debugging:
    public AudioTrackType TrackType => trackType;


    
    [Header("Track Configuration")]
    [SerializeField] public AudioTrackType trackType;
    
    [Header("Audio Sources (3-Source System)")]
    [HideInInspector] public AudioSource mainSource;      // Currently playing at target volume
    [HideInInspector] public AudioSource cueSource;       // Incoming audio (fading in)
    [HideInInspector] public AudioSource outgoingSource;  // Audio being faded out
    
    private Coroutine mainCoroutine;  // Add this field at the top
    

    #region Editor Stuff
    // properties for editor debugging
    #if UNITY_EDITOR
        public AudioSource MainSource => mainSource != null && mainSource ? mainSource : null;
        public AudioSource CueSource => cueSource != null && cueSource ? cueSource : null;
        public AudioSource OutgoingSource => outgoingSource != null && outgoingSource ? outgoingSource : null;
    #endif
    #endregion
    
    [Header("State")]
    [HideInInspector] public AudioTrackState currentState = AudioTrackState.Stopped;
    
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
        // Try to get AudioManager from the same GameObject first
        audioManager = GetComponent<AudioManager>();
    
        // If not found, try to get from parent
        if (audioManager == null)
        {
            audioManager = GetComponentInParent<AudioManager>();
        }
    
        // If still not found, try to find it in the scene
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }
    
        if (audioManager == null)
        {
            AudioDebug.LogError($"[AudioTrack] Cannot find AudioManager! Make sure AudioTrack is on the same GameObject as AudioManager or is a child of it.");
        }
        else
        {
            AudioDebug.Log($"[AudioTrack] Found AudioManager: {audioManager.name}");
        }
    }
    // set by AudioManager during its Awake
    public void SetTrackType(AudioTrackType type)
    {
        trackType = type;
        AudioDebug.Log($"[AudioTrack] Track type set to: {trackType}");
    }


    // ==================== PUBLIC CALL METHODS ====================
    #region PUBLIC METHODS

     /// <summary>
    /// Play method with 3-source safety system - STATE BASED -
    /// </summary>
    public void Play(int trackNumber, string trackName, float volume, float pitch, float spatialBlend, 
                     FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        AudioDebug.Log($"[Track] Play called - Current State: {currentState}, FadeType: {fadeType}");
        
        // Store target settings
        targetVolume = volume;
        targetPitch = pitch;
        currentSpatialBlend = spatialBlend;
        isLooping = loop;
        
        // Get the audio clip
        AudioClip clip = ResolveAudioClip(trackNumber, trackName);
        if (clip == null)
        {
            AudioDebug.LogError($"AudioTrack: Could not find clip for track {trackNumber}/{trackName}");
            return;
        }
        
        // Handle based on current state
        switch (currentState)
        {
            case AudioTrackState.Stopped:
                // Simple case - just start playing
                StartFromStopped(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
                break;
                
            case AudioTrackState.Playing:
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
                
            case AudioTrackState.Crossfading:
                // Already crossfading - use 3-source safety
                HandlePlayDuringCrossfade(clip, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
                break;
                
            case AudioTrackState.FadingIn:
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
                
            case AudioTrackState.Paused:
                AudioDebug.LogWarning("Cannot play while paused. Resume or stop first.");
                break;
                
            case AudioTrackState.FadingOut:
                // Let fade out complete, then start new
                AudioDebug.Log("Currently fading out. New play request will start after fade completes.");
                break;
        }
    }

    /// <summary>
    /// Toggle pause/unpause. Can be safely spammed - will toggle between pause/resume states
    /// </summary>
    public void PauseToggle(float fadeDuration = 0f, FadeTarget fadeTarget = FadeTarget.FadeVolume)
    {
        if (currentState == AudioTrackState.Paused)
        {
            Resume(fadeDuration, fadeTarget);
        }
        else if (currentState == AudioTrackState.FadeToPause && mainCoroutine != null)
        {
            // Interrupt fade-to-pause and reverse to fade-from-pause
            StopCoroutine(mainCoroutine);
            mainCoroutine = null;
        
            if (mainSource != null)
            {
                mainSource.UnPause(); // Make sure it's unpaused
                currentState = AudioTrackState.FadeFromPause;
                mainCoroutine = StartCoroutine(FadeFromPause(fadeDuration, fadeTarget));
            }
        }
        else if (currentState == AudioTrackState.FadeFromPause && mainCoroutine != null)
        {
            // Interrupt fade-from-pause and reverse to fade-to-pause
            StopCoroutine(mainCoroutine);
            mainCoroutine = null;
        
            currentState = AudioTrackState.FadeToPause;
            mainCoroutine = StartCoroutine(FadeToPause(fadeDuration, fadeTarget));
        }
        else
        {
            Pause(fadeDuration, fadeTarget);
        }
    }

    /// <summary>
    /// Pause the current audio with optional fade - Called by TogglePause
    /// </summary>
    private void Pause(float fadeDuration = 0f, FadeTarget fadeTarget = FadeTarget.FadeVolume)
    {
        AudioDebug.Log($"[Track] Pause called - Current State: {currentState}");

        // Can only pause if we're in a playing state
        if (currentState == AudioTrackState.Stopped || currentState == AudioTrackState.Paused)
        {
            AudioDebug.LogWarning($"[Track] Cannot pause from state: {currentState}");
            return;
        }

        // Stop any active main coroutine (fade interruption)
        if (mainCoroutine != null)
        {
            StopCoroutine(mainCoroutine);
            mainCoroutine = null;
        }

        // Stop any active fade coroutines but keep sources
        if (cueCoroutine != null)
        {
            StopCoroutine(cueCoroutine);
            cueCoroutine = null;
        }

        if (outgoingCoroutine != null)
        {
            StopCoroutine(outgoingCoroutine);
            outgoingCoroutine = null;
        }

        // Clean up outgoing source since it was fading out anyway
        if (outgoingSource != null)
        {
            outgoingSource.Stop();
            Destroy(outgoingSource.gameObject);
            outgoingSource = null;
        }

        // If we have a cue that was fading in, promote it to main first
        if (cueSource != null)
        {
            if (mainSource != null)
            {
                mainSource.Stop();
                Destroy(mainSource.gameObject);
            }
            mainSource = cueSource;
            cueSource = null;
        }

        // Now pause the main source
        if (mainSource != null)
        {
            if (fadeDuration <= 0f)
            {
                // Instant pause
                mainSource.Pause();
                currentState = AudioTrackState.Paused;
            }
            else
            {
                // Fade to pause - starts from CURRENT values
                currentState = AudioTrackState.FadeToPause;
                mainCoroutine = StartCoroutine(FadeToPause(fadeDuration, fadeTarget));
            }
        }
    }

    /// <summary>
    /// Resume from pause with optional fade - Called by TogglePause
    /// </summary>
    private void Resume(float fadeDuration = 0f, FadeTarget fadeTarget = FadeTarget.FadeVolume)
    {
        AudioDebug.Log($"[Track] Resume called - Current State: {currentState}");

        if (currentState != AudioTrackState.Paused)
        {
            AudioDebug.LogWarning($"[Track] Cannot resume from state: {currentState}");
            return;
        }

        if (mainSource == null)
        {
            AudioDebug.LogWarning("[Track] No paused source to resume");
            currentState = AudioTrackState.Stopped;
            return;
        }

        if (fadeDuration <= 0f)
        {
            // Instant resume
            mainSource.UnPause();
            mainSource.volume = targetVolume;
            mainSource.pitch = targetPitch;
            currentState = AudioTrackState.Playing;
        }
        else
        {
            // Fade from pause - starts from CURRENT values
            mainSource.UnPause();
            currentState = AudioTrackState.FadeFromPause;
            mainCoroutine = StartCoroutine(FadeFromPause(fadeDuration, fadeTarget));
        }
    }
     
    /// <summary>
    /// Stop all audio immediately or with fade. Overrides everything and cleans up all sources.
    /// </summary>
    private bool stopRequested = false;

    public void Stop(float fadeDuration = 0f, FadeTarget fadeTarget = FadeTarget.FadeVolume)
    {
        AudioDebug.Log($"[Track] Stop called - Current State: {currentState}");
        
        // Set the stop flag FIRST - this prevents any fade coroutines from continuing
        stopRequested = true;
        
        // Stop all active coroutines
        if (mainCoroutine != null)
        {
            StopCoroutine(mainCoroutine);
            mainCoroutine = null;
        }
        
        if (cueCoroutine != null)
        {
            StopCoroutine(cueCoroutine);
            cueCoroutine = null;
        }
        
        if (outgoingCoroutine != null)
        {
            StopCoroutine(outgoingCoroutine);
            outgoingCoroutine = null;
        }
        
        if (fadeDuration <= 0f)
        {
            InstantStop();
        }
        else
        {
            currentState = AudioTrackState.FadingOut;
            mainCoroutine = StartCoroutine(FadeAllToStop(fadeDuration, fadeTarget));
        }
    }
    
    /// <summary>
    /// Instantly stop and destroy all sources
    /// </summary>
    private void InstantStop()
    {
        // Clean up main source
        if (mainSource != null)
        {
            mainSource.Stop();
            Destroy(mainSource.gameObject);
            mainSource = null;
        }

        // Clean up cue source
        if (cueSource != null)
        {
            cueSource.Stop();
            Destroy(cueSource.gameObject);
            cueSource = null;
        }

        // Clean up outgoing source
        if (outgoingSource != null)
        {
            outgoingSource.Stop();
            Destroy(outgoingSource.gameObject);
            outgoingSource = null;
        }

        currentState = AudioTrackState.Stopped;
        AudioDebug.Log("[Track] Instant stop complete");
    }
    

    /// <summary>
    /// Update parameters of the currently playing main source with optional fading
    /// Works during Playing and FadingIn states - can interrupt existing fades with new targets
    /// </summary>
    public void UpdateParameters(Transform newAttachTo, float newVolume, float newPitch, 
        float newSpatialBlend, float fadeDuration, FadeTarget fadeTarget, bool newLoop, string eventName)
    {
        AudioDebug.Log($"[Track] UpdateParameters called - Current State: {currentState}");
        
        // Safety check - allow updates during Playing and FadingIn states
        if (currentState != AudioTrackState.Playing && 
            currentState != AudioTrackState.FadingIn && 
            currentState != AudioTrackState.AdjustingParameters)
        {
            AudioDebug.LogWarning($"[Track] Cannot update parameters during state: {currentState}. Only allowed during Playing, FadingIn, or Updating states.");
            return;
        }
        
        // Must have a main source to update
        if (mainSource == null)
        {
            AudioDebug.LogWarning("[Track] No main source to update parameters for.");
            return;
        }
        
        AudioDebug.Log($"[Track] Updating parameters - Volume: {newVolume}, Pitch: {newPitch}, SpatialBlend: {newSpatialBlend}, FadeTarget: {fadeTarget}");
        
        // Store new target settings
        targetVolume = newVolume;
        targetPitch = newPitch;
        currentSpatialBlend = newSpatialBlend;
        isLooping = newLoop;
        
        // Instant updates (no fading needed)
        // Transform change - only if it's actually different and not null
        if (newAttachTo != null && newAttachTo != mainSource.transform.parent)
        {
            mainSource.transform.SetParent(newAttachTo);
            mainSource.transform.position = newAttachTo.position;
            AudioDebug.Log($"[Track] Moved audio source to new parent: {newAttachTo.name}");
        }
        else if (newAttachTo != null)
        {
            AudioDebug.Log("[Track] Transform not changed - already attached to specified parent");
        }
        
        // Loop setting - instant
        mainSource.loop = newLoop;
        
        // Spatial blend - instant (could be faded in Phase 2)
        mainSource.spatialBlend = newSpatialBlend;
        
        // Volume and Pitch updates based on FadeTarget
        if (fadeTarget == FadeTarget.Ignore)
        {
            // Ignore means don't update volume/pitch at all (like in Play method)
            AudioDebug.Log("[Track] FadeTarget.Ignore - Volume and Pitch not updated");
        }
        else if (fadeDuration <= 0f)
        {
            // Instant parameter changes when no fade duration
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = newVolume;
                
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = newPitch;
                
            AudioDebug.Log("[Track] Parameters updated instantly (no fade duration)");
        }
        else
        {
            // Faded parameter changes - can interrupt existing fades
            // Stop any existing main coroutine first
            if (mainCoroutine != null)
            {
                StopCoroutine(mainCoroutine);
                mainCoroutine = null;
                AudioDebug.Log("[Track] Interrupted existing fade for new parameter targets");
            }
            
            // Start parameter fade
            currentState = AudioTrackState.AdjustingParameters; // Reusing existing state for parameter fading
            mainCoroutine = StartCoroutine(FadeParametersToTarget(fadeDuration, fadeTarget, newVolume, newPitch));
            AudioDebug.Log($"[Track] Started parameter fade - duration: {fadeDuration}s, target: {fadeTarget}");
        }
    }
    
    #endregion

    
    // ==================== 3-SOURCE SAFETY METHODS ====================
    
    private void HandlePlayDuringCrossfade(AudioClip clip, float volume, float pitch, float spatialBlend,
        FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        AudioDebug.Log("[Track] Handling Play during crossfade - 3-source safety engaged");

        // Store current outgoing source fade values for inheritance
        float inheritVolume = 0f;
        float inheritPitch = 0f;
        float inheritFadeDuration = fadeDuration; // Default to new fade duration
        bool hasInheritance = false;

        if (outgoingSource != null)
        {
            // Capture current fade values from the outgoing source
            inheritVolume = outgoingSource.volume;
            inheritPitch = outgoingSource.pitch;
            hasInheritance = true;

            // Try to inherit remaining fade time from the current outgoing coroutine
            if (outgoingCoroutine != null)
            {
                // Calculate remaining fade time based on current volume/pitch progress
                // Assuming fade is linear and we're fading to 0
                float fadeProgress = 1f - (outgoingSource.volume / targetVolume);
                if (fadeProgress > 0f && fadeProgress < 1f)
                {
                    // Estimate remaining time based on progress
                    inheritFadeDuration = fadeDuration * (1f - fadeProgress);
                    AudioDebug.Log($"[Track] Inheriting fade duration: {inheritFadeDuration:F2}s (progress: {fadeProgress:F2})");
                }
            }

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

            // Start fade out on the demoted cue using inherited fade duration
            outgoingCoroutine = StartCoroutine(FadeOutAndDestroy(outgoingSource, inheritFadeDuration, fadeTarget));
        }
        // If no cue but there's a main, move main to outgoing
        else if (mainSource != null)
        {
            outgoingSource = mainSource;
            mainSource = null;
            outgoingCoroutine = StartCoroutine(FadeOutAndDestroy(outgoingSource, fadeDuration, fadeTarget));
        }

        // Create new cue source for the new track
        cueSource = CreateAudioSource(attachTo, clip);
        if (cueSource == null) return;

        cueSource.clip = clip;
        cueSource.loop = loop;
        cueSource.spatialBlend = spatialBlend;
        cueSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
        cueSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
        cueSource.Play();

        // Start fade in on new cue
        currentState = AudioTrackState.Crossfading;
        cueCoroutine = StartCoroutine(FadeInCue(fadeDuration, fadeTarget, volume, pitch));
    }
    
    // ==================== STATE TRANSITION METHODS ====================
    
    private void StartFromStopped(AudioClip clip, float volume, float pitch, float spatialBlend,
        float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        AudioDebug.Log($"[DEBUG] StartFromStopped called - fadeTarget: {fadeTarget}, fadeDuration: {fadeDuration}");
    
        mainSource = CreateAudioSource(attachTo, clip);
        if (mainSource == null) return;
    
        mainSource.clip = clip;
        mainSource.loop = loop;
        mainSource.spatialBlend = spatialBlend;
    
        if (fadeTarget == FadeTarget.Ignore || fadeDuration <= 0)
        {
            AudioDebug.Log("[DEBUG] Taking INSTANT play path");
            // Instant play
            mainSource.volume = volume;
            mainSource.pitch = pitch;
            mainSource.Play();
            currentState = AudioTrackState.Playing;
        }
        else
        {
            AudioDebug.Log($"[DEBUG] Taking FADE play path - setting initial volume to: {(fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth ? 0f : volume)}");
        
            // Fade in
            mainSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
            mainSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
            mainSource.Play();
        
            AudioDebug.Log($"[DEBUG] Audio source playing: {mainSource.isPlaying}, volume: {mainSource.volume}");
        
            currentState = AudioTrackState.FadingIn;
            mainCoroutine = StartCoroutine(FadeInMain(fadeDuration, fadeTarget, volume, pitch));
        
            AudioDebug.Log("[DEBUG] Started FadeInMain coroutine");
        }
    }
    
    private void StartCrossfade(AudioClip clip, float volume, float pitch, float spatialBlend,
        float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        AudioDebug.Log("[Track] Starting crossfade - enforcing 3-source limit");

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
        cueSource = CreateAudioSource(attachTo, clip);
        if (cueSource == null) return;

        cueSource.clip = clip;
        cueSource.loop = loop;
        cueSource.spatialBlend = spatialBlend;

        // Set initial fade values
        cueSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
        cueSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
        cueSource.Play();

        currentState = AudioTrackState.Crossfading;

        // Start fade in on new cue
        cueCoroutine = StartCoroutine(FadeInCue(fadeDuration, fadeTarget, volume, pitch));
    }
    
    private void StartFadeOutIn(AudioClip clip, float volume, float pitch, float spatialBlend,
        float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        AudioDebug.Log("[Track] Starting Fade Out/In transition");

        // IMMEDIATE cleanup of any existing sources (safety)
        if (outgoingCoroutine != null)
        {
            StopCoroutine(outgoingCoroutine);
            outgoingCoroutine = null;
        }
    
        if (outgoingSource != null)
        {
            outgoingSource.Stop();
            Destroy(outgoingSource.gameObject);
            outgoingSource = null;
        }

        if (cueCoroutine != null)
        {
            StopCoroutine(cueCoroutine);
            cueCoroutine = null;
        }
    
        if (cueSource != null)
        {
            cueSource.Stop();
            Destroy(cueSource.gameObject);
            cueSource = null;
        }

        // Move current main to outgoing for fade out
        if (mainSource != null)
        {
            outgoingSource = mainSource;
            mainSource = null;

            // Start fade out, then fade in when complete
            currentState = AudioTrackState.FadingOut;
            outgoingCoroutine = StartCoroutine(FadeOutThenFadeIn(outgoingSource, clip, volume, pitch,
                spatialBlend, fadeDuration, fadeTarget, loop, attachTo));
        }
        else
        {
            // No main source, just start from stopped
            StartFromStopped(clip, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, attachTo);
        }
    }
    // ==================== FADE COROUTINES ====================
    
    private IEnumerator FadeInMain(float duration, FadeTarget fadeTarget, float targetVol, float targetPit)
    {
        float elapsed = 0f;
        float startVol = mainSource.volume;
        float startPit = mainSource.pitch;

        while (elapsed < duration && mainSource != null)
        {
            // Check for stop interruption only - mainCoroutine assignment happens after StartCoroutine
            if (stopRequested) 
            {
                AudioDebug.Log("[Track] FadeInMain interrupted by stop - exiting cleanly");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = Mathf.Lerp(startVol, targetVol, t);

            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = Mathf.Lerp(startPit, targetPit, t);

            yield return null;
        }

        // Only finish if not interrupted - check AGAIN before setting final values
        if (!stopRequested && mainSource != null && mainCoroutine != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = targetVol;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = targetPit;

            currentState = AudioTrackState.Playing;
            AudioDebug.Log("[Track] FadeInMain completed successfully");
        }
        else
        {
            AudioDebug.Log("[Track] FadeInMain was interrupted - skipping final state change");
        }
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
            currentState = AudioTrackState.Playing;
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
    
    private IEnumerator FadeOutThenFadeIn(AudioSource fadeOutSource, AudioClip newClip, float volume,
        float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, Transform attachTo)
    {
        // Pre-create the incoming source as cue (but don't play yet)
        cueSource = CreateAudioSource(attachTo, newClip);
        if (cueSource == null) yield break;

        cueSource.clip = newClip;
        cueSource.loop = loop;
        cueSource.spatialBlend = spatialBlend;
        cueSource.volume = (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth) ? 0f : volume;
        cueSource.pitch = (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth) ? 0f : pitch;
        // Don't play yet - it's waiting as cue

        // Phase 1: Fade out the current source
        if (fadeOutSource != null)
        {
            float elapsed = 0f;
            float startVol = fadeOutSource.volume;
            float startPit = fadeOutSource.pitch;

            while (elapsed < fadeDuration && fadeOutSource != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                    fadeOutSource.volume = Mathf.Lerp(startVol, 0f, t);

                if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                    fadeOutSource.pitch = Mathf.Lerp(startPit, 0f, t);

                yield return null;
            }

            // Clean up the faded out source
            if (fadeOutSource != null)
            {
                fadeOutSource.Stop();
                Destroy(fadeOutSource.gameObject);
                outgoingSource = null;
            }
        }

        // Phase 2: Promote cue to main IMMEDIATELY when it starts playing
        if (cueSource != null)
        {
            cueSource.Play();
            
            // PROMOTE CUE TO MAIN RIGHT AWAY - prevents bypass issues
            mainSource = cueSource;
            cueSource = null;
            
            currentState = AudioTrackState.FadingIn;

            // Fade in the now-main source
            float elapsed2 = 0f;
            float startVol2 = mainSource.volume;
            float startPit2 = mainSource.pitch;

            while (elapsed2 < fadeDuration && mainSource != null)
            {
                elapsed2 += Time.deltaTime;
                float t = elapsed2 / fadeDuration;

                if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                    mainSource.volume = Mathf.Lerp(startVol2, volume, t);

                if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                    mainSource.pitch = Mathf.Lerp(startPit2, pitch, t);

                yield return null;
            }

            // Ensure final values
            if (mainSource != null)
            {
                if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                    mainSource.volume = volume;
                if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                    mainSource.pitch = pitch;
            }
        }

        currentState = AudioTrackState.Playing;
        outgoingCoroutine = null;
    }
    
    //Pause
    private IEnumerator FadeToPause(float duration, FadeTarget fadeTarget)
    {
        if (mainSource == null) yield break;

        float elapsed = 0f;
        float startVol = mainSource.volume;
        float startPit = mainSource.pitch;

        while (elapsed < duration && mainSource != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = Mathf.Lerp(startVol, 0f, t);

            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = Mathf.Lerp(startPit, 0f, t);

            yield return null;
        }

        // Pause at the end of fade
        if (mainSource != null)
        {
            mainSource.Pause();
            currentState = AudioTrackState.Paused;
        }

        mainCoroutine = null;
    }

    private IEnumerator FadeFromPause(float duration, FadeTarget fadeTarget)
    {
        if (mainSource == null) yield break;

        float elapsed = 0f;
        float startVol = mainSource.volume;
        float startPit = mainSource.pitch;

        while (elapsed < duration && mainSource != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = Mathf.Lerp(startVol, targetVolume, t);

            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = Mathf.Lerp(startPit, targetPitch, t);

            yield return null;
        }

        // Ensure final values
        if (mainSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = targetVolume;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = targetPitch;

            currentState = AudioTrackState.Playing;
        }

        mainCoroutine = null;
    }
    
        
     private IEnumerator FadeAllToStop(float duration, FadeTarget fadeTarget)
    {
        stopRequested = false; // Reset flag for this fade operation
        
        // Collect all active sources and capture their CURRENT VALUES
        var activeSources = new System.Collections.Generic.List<(AudioSource source, float startVol, float startPit)>();

        if (mainSource != null)
            activeSources.Add((mainSource, mainSource.volume, mainSource.pitch));

        if (cueSource != null)
            activeSources.Add((cueSource, cueSource.volume, cueSource.pitch));

        if (outgoingSource != null)
            activeSources.Add((outgoingSource, outgoingSource.volume, outgoingSource.pitch));

        if (activeSources.Count == 0)
        {
            currentState = AudioTrackState.Stopped;
            mainCoroutine = null;
            yield break;
        }

        AudioDebug.Log($"[Track] Fading {activeSources.Count} sources to stop over {duration}s");

        // Fade from the captured values - FORCE OVERRIDE every frame
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var (source, startVol, startPit) = activeSources[i];

                if (source == null)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }

                // FORCE these values every frame - overrides any other coroutines
                if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                {
                    float targetVol = Mathf.Lerp(startVol, 0f, t);
                    source.volume = targetVol;
                    // Force it again to ensure it sticks
                    source.volume = targetVol;
                }

                if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                {
                    float targetPit = Mathf.Lerp(startPit, 0f, t);
                    source.pitch = targetPit;
                    // Force it again to ensure it sticks
                    source.pitch = targetPit;
                }
            }

            if (activeSources.Count == 0)
                break;

            yield return null;
        }

        InstantStop();
        AudioDebug.Log("[Track] Fade stop complete");
        mainCoroutine = null;
    }
    
    /// <summary>
    /// Fade current main source parameters to new target values
    /// Similar to FadeInMain but starts from current values instead of 0
    /// </summary>
    private IEnumerator FadeParametersToTarget(float duration, FadeTarget fadeTarget, float targetVol, float targetPit)
    {
        if (mainSource == null) yield break;
    
        float elapsed = 0f;
        float startVol = mainSource.volume;
        float startPit = mainSource.pitch;
    
        AudioDebug.Log($"[Track] Parameter fade starting - From Vol:{startVol:F2}/Pitch:{startPit:F2} To Vol:{targetVol:F2}/Pitch:{targetPit:F2}");

        while (elapsed < duration && mainSource != null)
        {
            // Check for stop interruption only (removed mainCoroutine check)
            if (stopRequested) 
            {
                AudioDebug.Log("[Track] Parameter fade interrupted by stop - exiting cleanly");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = Mathf.Lerp(startVol, targetVol, t);

            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = Mathf.Lerp(startPit, targetPit, t);

            yield return null;
        }

        // Only finish if not interrupted
        if (!stopRequested && mainSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                mainSource.volume = targetVol;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                mainSource.pitch = targetPit;

            currentState = AudioTrackState.Playing; // Return to stable playing state
            AudioDebug.Log("[Track] Parameter fade completed successfully");
        }
        else
        {
            AudioDebug.Log("[Track] Parameter fade was interrupted - skipping final state change");
        }
    
        mainCoroutine = null;
    }
     
    // ==================== HELPER METHODS ====================
    #region HELPER METHODS

    // Resolve audio clip by track number or name
    private AudioClip ResolveAudioClip(int trackNumber, string trackName)
    {
        if (audioManager == null)
        {
            AudioDebug.LogError($"[AudioTrack] AudioManager is null!");
            return null;
        }
    
        // Try by name first if provided AND NOT EMPTY
        if (!string.IsNullOrEmpty(trackName))
        {
            AudioClip clip = trackType switch
            {
                AudioTrackType.BGM => audioManager.GetBGMClip(trackName),
                AudioTrackType.Ambient => audioManager.GetAmbientClip(trackName),
                AudioTrackType.Dialogue => audioManager.GetDialogueClip(trackName),
                _ => null
            };
        
            if (clip != null) return clip;
        }
    
        // ONLY use track number if trackNumber >= 0 (your original logic)
        if (trackNumber >= 0)
        {
            return trackType switch
            {
                AudioTrackType.BGM => audioManager.GetBGMClip(trackNumber),
                AudioTrackType.Ambient => audioManager.GetAmbientClip(trackNumber),
                AudioTrackType.Dialogue => audioManager.GetDialogueClip(trackNumber),
                _ => null
            };
        }
    
        return null;
    }
    
    private AudioSource CreateAudioSource(Transform attachTo = null, AudioClip clip = null)
    {
        GameObject prefab = trackType switch
        {
            AudioTrackType.BGM => audioManager.GetBGMPrefab(),
            AudioTrackType.Ambient => audioManager.GetAmbientPrefab(),
            AudioTrackType.Dialogue => audioManager.GetDialoguePrefab(),
            _ => null
        };

        if (prefab == null)
        {
            AudioDebug.LogError("No audio prefab set in AudioManager!");
            return null;
        }

        Transform parent = attachTo ?? audioManager.transform;
        GameObject audioObj = Instantiate(prefab, parent.position, Quaternion.identity, parent);
    
        // Set the AUDIO TYPE
        AudioSourceType audioSourceType = audioObj.GetComponent<AudioSourceType>();
        if (audioSourceType != null)
        {
            // Get track type name
            AudioType audioType = trackType switch
            {
                AudioTrackType.BGM => AudioType.BGM,
                AudioTrackType.Ambient => AudioType.Ambient,
                AudioTrackType.Dialogue => AudioType.Dialogue,
                _ => AudioType.Null
            };
        
            audioSourceType.AudioType = audioType;
            AudioDebug.Log($"[AudioTrack] Set AudioType to {audioType} for {trackType} track");
        }
        else
        {
            AudioDebug.LogWarning($"[AudioTrack] No AudioSourceType component found on {trackType} track prefab");
        }
        
        // Get track type name
        string trackTypeName = trackType switch
        {
            AudioTrackType.BGM => "BGM",
            AudioTrackType.Ambient => "Ambient", 
            AudioTrackType.Dialogue => "Dialogue",
            _ => "Audio"
        };
    
        // Get clip name (clean it up)
        string clipName = "Unknown";
        if (clip != null)
        {
            clipName = clip.name;
            // Remove "(Clone)" if it exists
            if (clipName.Contains("(Clone)"))
            {
                clipName = clipName.Replace("(Clone)", "").Trim();
            }
        }
    
        // Simple naming: ClipName (Type)
        audioObj.name = $"{clipName} ({trackTypeName})"; // todo consider adding the state to the name as well
    
        return audioObj.GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Only check when we have a main source that's supposed to be playing
        if (mainSource != null && currentState == AudioTrackState.Playing && 
            mainSource.clip != null && !mainSource.loop)
        {
            // Check if non-looped audio has finished
            if (!mainSource.isPlaying || mainSource.time >= mainSource.clip.length - 0.01f)
            {
                AudioDebug.Log($"[AudioTrack] Non-looped audio finished: {mainSource.clip.name}");
                InstantStop();
            }
        }
    }

    #endregion
    
    
    // ==================== PUBLIC PROPERTIES ====================
    #region PUBLIC PROPERTIES

    public AudioTrackState CurrentState => currentState;
    public bool IsPlaying => mainSource != null && mainSource.isPlaying;
    public bool IsCrossfading => currentState == AudioTrackState.Crossfading;

    #endregion
    

    
    // Debug helpers---------------------------------------------------------
    public string DebugInfo => $"Type: {trackType}, State: {currentState}, Main: {(mainSource != null)}, Cue: {(cueSource != null)}, Outgoing: {(outgoingSource != null)}";
    
    
}