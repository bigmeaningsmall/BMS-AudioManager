using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum AudioTrackType
{
    BGM,
    Ambient, 
    Dialogue
}

/// <summary>
/// The AudioManager class is responsible for managing background music and sound effects in the game.
/// It handles loading audio resources and routing events to appropriate track components.
/// This class uses the singleton pattern to ensure only one instance is active at any time.
/// The methods of this class are called via events defined in the AudioEventManager.
/// </summary>

public class AudioManager : MonoBehaviour
{
    [Header("VERSION")]
    [SerializeField] private string version = "v2.0.0";
    public static AudioManager Instance { get; private set; }

    // Track Components (these handle everything)
    [Header("Audio Tracks")]
    [SerializeField] private AudioTrack bgmTrack;
    [SerializeField] private AudioTrack ambientTrack;
    [SerializeField] private AudioTrack dialogueTrack;
    
    // Audio Resource Dictionaries (KEEP - centralized loading)
    [Header("Audio Resources")]
    private Dictionary<int, AudioClip> musicTracks = new Dictionary<int, AudioClip>();
    private Dictionary<int, AudioClip> ambientAudioTracks = new Dictionary<int, AudioClip>();
    private Dictionary<int, AudioClip> dialogueAudioTracks = new Dictionary<int, AudioClip>();
    private Dictionary<string, AudioClip> soundEffects = new Dictionary<string, AudioClip>();

    // Prefab References (KEEP - tracks will use these)
    [Header("Audio Prefabs")]
    [SerializeField] private GameObject audioTrackPrefab; // Generic prefab for audio tracks
    // [SerializeField] private GameObject musicPrefab;
    // [SerializeField] private GameObject ambientAudioPrefab;
    // [SerializeField] private GameObject dialogueAudioPrefab;
    [SerializeField] private GameObject soundEffectPrefab;
    
    [Header("SFX Settings")]
    [SerializeField] [Range(0f, 1f)] private float globalSFXAttenuation = 1f;
    public float GlobalSFXAttenuation 
    { 
        get => globalSFXAttenuation; 
        set => globalSFXAttenuation = Mathf.Clamp01(value); 
    }

    // Available Audio Lists (KEEP - for inspector visibility)
    #region Available Audio Tracks
    [Header("Available Music Tracks")]
    [SerializeField] private List<string> musicTrackNames = new List<string>();
    
    [Header("Available Ambient Audio Tracks")]
    [SerializeField] private List<string> ambientAudioTrackNames = new List<string>();

    [Header("Available Dialogue Audio Tracks")]
    [SerializeField] private List<string> dialogueTrackNames = new List<string>();
    
    [Header("Available Sound Effects")]
    [SerializeField] private List<string> soundEffectNames = new List<string>();
    #endregion


    // Parameter and Porperty References for tracks - these are for checking and reference
    // Parameters for audio - used for getting current state info
    private AudioTrackParamters bgmTrackParameters;
    private AudioTrackParamters ambientTrackParameters;
    private AudioTrackParamters dialogueTrackParameters;

    // public readonly getters -- optional, but useful for other scripts to access track parameters
    public AudioTrackParamters BGMParameters => bgmTrackParameters;
    public AudioTrackParamters AmbientParameters => ambientTrackParameters;
    public AudioTrackParamters DialogueParameters => dialogueTrackParameters;
    
    
    /// <summary>
    /// METHODS START HERE ------------------------------------------------------
    /// </summary>
    
    // Singleton Pattern
    #region Initialise Singleton & Audio Tracks
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        
            // Initialize track types BEFORE loading resources
            InitializeTrackTypes();
            LoadAudioResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Validate Audio Track Setup")]
    public void ValidateAudioTrackSetup()
    {
        Debug.Log("=== AudioManager Track Validation ===");
        
        if (bgmTrack == null)
            Debug.LogError("[AudioManager] BGM Track reference is NULL!");
        else
            Debug.Log($"[AudioManager] BGM Track: {bgmTrack.name} (Type: {bgmTrack.trackType})");
            
        if (ambientTrack == null)
            Debug.LogError("[AudioManager] Ambient Track reference is NULL!");
        else
            Debug.Log($"[AudioManager] Ambient Track: {ambientTrack.name} (Type: {ambientTrack.trackType})");
            
        if (dialogueTrack == null)
            Debug.LogError("[AudioManager] Dialogue Track reference is NULL!");
        else
            Debug.Log($"[AudioManager] Dialogue Track: {dialogueTrack.name} (Type: {dialogueTrack.trackType})");
            
        Debug.Log("=== Audio Resources ===");
        Debug.Log($"BGM tracks loaded: {musicTracks.Count}");
        Debug.Log($"Ambient tracks loaded: {ambientAudioTracks.Count}");
        Debug.Log($"Dialogue tracks loaded: {dialogueAudioTracks.Count}");
        Debug.Log($"SFX loaded: {soundEffects.Count}");
    }


    private void InitializeTrackTypes()
    {
        Debug.Log("[AudioManager] Initializing track types...");
        
        // Set the track types for each AudioTrack component
        if (bgmTrack != null)
        {
            bgmTrack.SetTrackType(AudioTrackType.BGM);
            Debug.Log("[AudioManager] BGM track type initialized");
        }
        else
        {
            Debug.LogError("[AudioManager] BGM track reference is null! Please assign an AudioTrack component to the bgmTrack field.");
        }
        
        if (ambientTrack != null)
        {
            ambientTrack.SetTrackType(AudioTrackType.Ambient);
            Debug.Log("[AudioManager] Ambient track type initialized");
        }
        else
        {
            Debug.LogError("[AudioManager] Ambient track reference is null! Please assign an AudioTrack component to the ambientTrack field.");
        }
        
        if (dialogueTrack != null)
        {
            dialogueTrack.SetTrackType(AudioTrackType.Dialogue);
            Debug.Log("[AudioManager] Dialogue track type initialized");
        }
        else
        {
            Debug.LogError("[AudioManager] Dialogue track reference is null! Please assign an AudioTrack component to the dialogueTrack field.");
        }
    }
    
    #endregion

    // Event Subscriptions
    #region Event Subscriptions
    private void OnEnable()
    {
        AudioEventManager.playTrack += PlayTrack;
        AudioEventManager.stopTrack += StopTrack;
        AudioEventManager.pauseTrack += PauseTrack;
        AudioEventManager.adjustTrack += AdjustTrack;
        
        AudioEventManager.PlaySFX += PlaySoundEffect;
    }

    private void OnDisable()
    {
        AudioEventManager.playTrack -= PlayTrack;
        AudioEventManager.stopTrack -= StopTrack;
        AudioEventManager.pauseTrack -= PauseTrack;
        AudioEventManager.adjustTrack -= AdjustTrack;
        
        AudioEventManager.PlaySFX -= PlaySoundEffect;
    }
    #endregion

    // Load Audio Resources (KEEP - centralized loading)
    #region Load Audio Resources
    private void LoadAudioResources()
    {
        AudioClip[] bgmClips = Resources.LoadAll<AudioClip>("Audio/BGM");
        for (int i = 0; i < bgmClips.Length; i++)
        {
            musicTracks[i] = bgmClips[i];
            musicTrackNames.Add(bgmClips[i].name);
        }
        
        AudioClip[] ambientClips = Resources.LoadAll<AudioClip>("Audio/Ambient");
        for (int i = 0; i < ambientClips.Length; i++)
        {
            ambientAudioTracks[i] = ambientClips[i];
            ambientAudioTrackNames.Add(ambientClips[i].name);
        }

        AudioClip[] dialogueClips = Resources.LoadAll<AudioClip>("Audio/Dialogue");
        for (int i = 0; i < dialogueClips.Length; i++)
        {
            dialogueAudioTracks[i] = dialogueClips[i];
            dialogueTrackNames.Add(dialogueClips[i].name);
        }
        
        AudioClip[] sfxClips = Resources.LoadAll<AudioClip>("Audio/SFX");
        foreach (var clip in sfxClips)
        {
            soundEffects[clip.name] = clip;
            soundEffectNames.Add(clip.name);
        }
        
    }
    #endregion

    #region Public Accessors for Audio Resources
    // Public accessors for tracks to get resources
    //-----------------------------------------------------------
    public AudioClip GetBGMClip(int index) => musicTracks.TryGetValue(index, out AudioClip clip) ? clip : null;

    public AudioClip GetBGMClip(string name)
    {
        foreach (var track in musicTracks)
        {
            if (track.Value.name == name) return track.Value;
        }
        return null;
    }
    public GameObject GetBGMPrefab() => audioTrackPrefab;
    //-----------------------------------------------------------
    public AudioClip GetAmbientClip(int index) => ambientAudioTracks.TryGetValue(index, out AudioClip clip) ? clip : null;
    public AudioClip GetAmbientClip(string name)
    {
        foreach (var track in ambientAudioTracks)
        {
            if (track.Value.name == name) return track.Value;
        }
        return null;
    }
    public GameObject GetAmbientPrefab() => audioTrackPrefab;
    //-----------------------------------------------------------

    public AudioClip GetDialogueClip(int index) => dialogueAudioTracks.TryGetValue(index, out AudioClip clip) ? clip : null;

    public AudioClip GetDialogueClip(string name)
    {
        foreach (var track in dialogueAudioTracks)
        {
            if (track.Value.name == name) return track.Value;
        }
        return null;
    }
    public GameObject GetDialoguePrefab() => audioTrackPrefab;
    //-----------------------------------------------------------
    #endregion

    
    //----------------------------------------------------------
    
    #region Public Event Methods - Audio Tracks
    
    // field to track delayed coroutines
    private Dictionary<AudioTrackType, Coroutine> delayedCoroutines = new Dictionary<AudioTrackType, Coroutine>();
    
    // PLAY TRACK METHODS---------------------------------------------------------------------------------------
    
    // Audio Event Methods (just passing properties and commands to audio tracks)
    public void PlayTrack(AudioTrackType trackType, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName)
    {
        // Cancel any existing delayed coroutine for this track type
        CancelDelayedTrack(trackType);
    
        if (delay <= 0f)
        {
            PlayTrackImmediate(trackType, attachTo, trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, eventName);
        }
        else
        {
            // Store the coroutine reference so we can cancel it later
            Coroutine delayedCoroutine = StartCoroutine(PlayTrackDelayed(delay, trackType, attachTo, trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, eventName));
            delayedCoroutines[trackType] = delayedCoroutine;
        }
    }
    // helper method to cancel delayed coroutines
    private void CancelDelayedTrack(AudioTrackType trackType)
    {
        if (delayedCoroutines.TryGetValue(trackType, out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
            {
                Debug.Log($"[AudioManager] CANCELLING delayed {trackType} event"); // Add this line
                StopCoroutine(existingCoroutine);
            }
            delayedCoroutines.Remove(trackType);
        }
        else
        {
            Debug.Log($"[AudioManager] No delayed {trackType} event to cancel"); // Add this line too
        }
    }
    private void PlayTrackImmediate(AudioTrackType trackType, Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        AudioTrack targetTrack = GetTrackByType(trackType);
        if (targetTrack == null)
        {
            Debug.LogError($"{trackType}Track reference is null!");
            return;
        }
        
        // CALL THE TRACK METHOD
        // This will handle the actual playing of the track
        targetTrack.Play(trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
        
        // Set parameters for the track -- parameters are updated in LateUpdate when fading 
        AudioTrackParamters newParams = new AudioTrackParamters(targetTrack.currentState, attachTo, trackNumber, trackName, volume, pitch, spatialBlend, loop, 0f, 0f, 0f, eventName);
        SetTrackParameters(trackType, newParams);
    }
    
    private IEnumerator PlayTrackDelayed(float delay, AudioTrackType trackType, Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        Debug.Log($"[AudioManager] Delaying {trackType} track for {delay}s");
        yield return new WaitForSeconds(delay);
    
        // Clean up the coroutine reference since it's completing
        delayedCoroutines.Remove(trackType);
    
        Debug.Log($"[AudioManager] Executing delayed {trackType} track");
        PlayTrackImmediate(trackType, attachTo, trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, eventName);
    }


    // STOP TRACK METHODS---------------------------------------------------------------------------------------
    
    public void StopTrack(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay = 0f, string eventName = "")
    {
        CancelDelayedTrack(trackType); // Cancel any pending events for this track
    
        if (delay <= 0f)
        {
            StopTrackImmediate(trackType, fadeDuration, fadeTarget);
        }
        else
        {
            Coroutine delayedCoroutine = StartCoroutine(StopTrackDelayed(delay, trackType, fadeDuration, fadeTarget));
            delayedCoroutines[trackType] = delayedCoroutine;
        }
    }

    private void StopTrackImmediate(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        AudioTrack targetTrack = GetTrackByType(trackType);
        if (targetTrack == null)
        {
            Debug.LogError($"{trackType}Track reference is null!");
            return;
        }
        targetTrack.Stop(fadeDuration, fadeTarget);
    }

    private IEnumerator StopTrackDelayed(float delay, AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        Debug.Log($"[AudioManager] Delaying {trackType} stop for {delay}s");
        yield return new WaitForSeconds(delay);
    
        // Clean up the coroutine reference since it's completing
        delayedCoroutines.Remove(trackType);
    
        Debug.Log($"[AudioManager] Executing delayed {trackType} stop");
        StopTrackImmediate(trackType, fadeDuration, fadeTarget);
    }

    // PAUSE TRACK METHODS---------------------------------------------------------------------------------------
    
    public void PauseTrack(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay = 0f, string eventName = "")
    {
        CancelDelayedTrack(trackType); // Cancel any pending events for this track
    
        if (delay <= 0f)
        {
            PauseTrackImmediate(trackType, fadeDuration, fadeTarget);
        }
        else
        {
            Coroutine delayedCoroutine = StartCoroutine(PauseTrackDelayed(delay, trackType, fadeDuration, fadeTarget));
            delayedCoroutines[trackType] = delayedCoroutine;
        }
    }

    private void PauseTrackImmediate(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        AudioTrack targetTrack = GetTrackByType(trackType);
        if (targetTrack == null)
        {
            Debug.LogError($"{trackType}Track reference is null!");
            return;
        }
        targetTrack.PauseToggle(fadeDuration, fadeTarget);
    }

    private IEnumerator PauseTrackDelayed(float delay, AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        Debug.Log($"[AudioManager] Delaying {trackType} pause for {delay}s");
        yield return new WaitForSeconds(delay);
    
        // Clean up the coroutine reference since it's completing
        delayedCoroutines.Remove(trackType);
    
        Debug.Log($"[AudioManager] Executing delayed {trackType} pause");
        PauseTrackImmediate(trackType, fadeDuration, fadeTarget);
    }
    
    // UPDATE TRACK METHODS---------------------------------------------------------------------------------------
    
    //method to update parameters of audio tracks
    public void AdjustTrack(AudioTrackType trackType, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName = "")
    {
        // Cancel ANY existing delayed event for this track type
        CancelDelayedTrack(trackType);
        
        if (delay <= 0f)
        {
            AdjustTrackImmediate(trackType, attachTo, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, eventName);
        }
        else
        {
            Coroutine delayedCoroutine = StartCoroutine(AdjustTrackDelayed(delay, trackType, attachTo, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, eventName));
            delayedCoroutines[trackType] = delayedCoroutine;
        }
    }

    private void AdjustTrackImmediate(AudioTrackType trackType, Transform attachTo, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        AudioTrack targetTrack = GetTrackByType(trackType);
        if (targetTrack == null)
        {
            Debug.LogError($"{trackType}Track reference is null!");
            return;
        }
        
        // CALL THE TRACK METHOD
        // This will handle the actual updating of the track parameters
        targetTrack.UpdateParameters(attachTo, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, eventName);
        
        // Get current parameters to preserve existing values
        AudioTrackParamters currentParams = GetTrackParameters(trackType);
        if (currentParams != null)
        {
            int tNum = currentParams.index; // Get the current index from the track
            string tName = currentParams.trackName;
            // if the eventname is not set, use the current event name
            if (string.IsNullOrEmpty(eventName)){
                eventName = currentParams.eventName;
            }
            
            AudioTrackParamters updatedParams = new AudioTrackParamters(targetTrack.currentState, attachTo, tNum, tName, volume, pitch, spatialBlend, loop, 0f, 0f, 0f, eventName);
            SetTrackParameters(trackType, updatedParams);
        }
    }

    private IEnumerator AdjustTrackDelayed(float delay, AudioTrackType trackType, Transform attachTo, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        Debug.Log($"[AudioManager] Delaying {trackType} update for {delay}s");
        yield return new WaitForSeconds(delay);
        
        // Clean up the coroutine reference since it's completing
        delayedCoroutines.Remove(trackType);
        
        Debug.Log($"[AudioManager] Executing delayed {trackType} update");
        AdjustTrackImmediate(trackType, attachTo, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, eventName);
    }

    //override UpdateTrack methods for different parameters // todo implement this in the future
    public void AdjustTrack(AudioTrackType trackType, Transform attachTo)
    {
        // Future implementation for simplified parameter updates
    }

    #endregion
    
    #region Helper Methods for Track Management

    private AudioTrack GetTrackByType(AudioTrackType trackType)
    {
        return trackType switch
        {
            AudioTrackType.BGM => bgmTrack,
            AudioTrackType.Ambient => ambientTrack,
            AudioTrackType.Dialogue => dialogueTrack,
            _ => null
        };
    }

    // Get the parameters for a specific track type - used internally but also called by AudioTrackParameterDisplay for accessing current track parameters
    public AudioTrackParamters GetTrackParameters(AudioTrackType trackType)
    {
        return trackType switch
        {
            AudioTrackType.BGM => bgmTrackParameters,
            AudioTrackType.Ambient => ambientTrackParameters,
            AudioTrackType.Dialogue => dialogueTrackParameters,
            _ => null
        };
    }

    private void SetTrackParameters(AudioTrackType trackType, AudioTrackParamters parameters)
    {
        switch (trackType)
        {
            case AudioTrackType.BGM:
                bgmTrackParameters = parameters;
                break;
            case AudioTrackType.Ambient:
                ambientTrackParameters = parameters;
                break;
            case AudioTrackType.Dialogue:
                dialogueTrackParameters = parameters;
                break;
        }
    }

    #endregion
    
    //---------------------------------------------------------- 
    
    private void LateUpdate()
    {
        // Update parameters for all track types during fading states
        UpdateTrackParameters(AudioTrackType.BGM);
        UpdateTrackParameters(AudioTrackType.Ambient);
        UpdateTrackParameters(AudioTrackType.Dialogue);
    }
    
    
    private void UpdateTrackParameters(AudioTrackType trackType)
    {
        AudioTrack track = GetTrackByType(trackType);
        AudioTrackParamters trackParams = GetTrackParameters(trackType);
    
        if (track == null || trackParams == null) return;
        
        trackParams.trackState = track.currentState;
        // trackParams.clipProgress = track.GetComponent<AudioSource>().time;
        // trackParams.clipLength = currentSource.clip != null ? currentSource.clip.length : 0f;
        // trackParams.clipPercent = trackParams.clipLength > 0f ? (trackParams.clipProgress / trackParams.clipLength) * 100f : 0f;
    
        AudioSource currentSource;
        
        // Handle fadeinout and crossfade separately - to decide between cue for crossfade or outgoing for fadein/out
        if (track.currentState == AudioTrackState.Crossfading)
        {
            currentSource = track.mainSource ? track.mainSource : track.cueSource;
        }
        else
        {
            currentSource = track.mainSource ? track.mainSource : track.outgoingSource;
        }
    
        if (currentSource == null)
        {
            // Only warn if the track is supposed to be playing
            if (track.currentState != AudioTrackState.Stopped)
            {
                Debug.LogWarning($"No active audio source found for {trackType} track.");
            }
            return;
        }
        
        trackParams.clipProgress = float.Parse(currentSource.time.ToString("F3"));
        trackParams.clipLength = currentSource.clip != null ? float.Parse(currentSource.clip.length.ToString("F3")) : 0f;
        trackParams.clipPercent = trackParams.clipLength > 0f ? float.Parse(((trackParams.clipProgress / trackParams.clipLength) * 100f).ToString("F1")) : 0f;

    
        // Update the track parameters based on the current audio source when fading or crossfading
        if (track.currentState == AudioTrackState.FadingIn || 
            track.currentState == AudioTrackState.FadingOut || 
            track.currentState == AudioTrackState.Crossfading ||
            track.currentState == AudioTrackState.AdjustingParameters ||
            track.currentState == AudioTrackState.FadeToPause ||
            track.currentState == AudioTrackState.FadeFromPause)
        {
            trackParams.trackState = track.currentState;
            trackParams.attachedTo = currentSource.transform.parent;
            trackParams.volume = currentSource.volume;
            trackParams.pitch = currentSource.pitch;
            trackParams.spatialBlend = currentSource.spatialBlend;
            trackParams.loop = currentSource.loop;
            trackParams.trackName = currentSource.clip != null ? currentSource.clip.name : "No Clip";
        
            // Remove "(Clone)" from the track name if it exists
            if (trackParams.trackName.Contains("(Clone)"))
            {
                trackParams.trackName = trackParams.trackName.Replace("(Clone)", "").Trim();
            }
        } 
    }
    
    
    // SFX Management (UNCHANGED)
    #region Play Sound Effects
    
    // delayed coroutines tracking for SFX (similar to tracks but specific to SFX)
    private List<Coroutine> delayedSFXCoroutines = new List<Coroutine>();
    
    // PlaySoundEffect method with all parameters
    // Note: soundNames is an array to allow random selection from multiple options
    public void PlaySoundEffect(string[] soundNames, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, bool loop, float delay, float percentChanceToPlay, Transform attachTo, Vector3 position, float minDist, float maxDist, string eventName)
    {
        // Check if the sound should play based on the percentage chance
        if (percentChanceToPlay < 100f)
        {
            int random = Random.Range(0, 100);
            if (random > percentChanceToPlay)
            {
                Debug.Log($"[AudioManager] SFX '{string.Join(", ", soundNames)}' skipped due to chance ({random} > {percentChanceToPlay})");
                return;
            }
        }
        
        // Select a random sound effect name from the array
        if (soundNames == null || soundNames.Length == 0)
        {
            Debug.LogError("[AudioManager] No sound names provided for SFX!");
            return;
        }
        
        string selectedSoundName = soundNames[Random.Range(0, soundNames.Length)];
        Debug.Log($"[AudioManager] Selected SFX: '{selectedSoundName}' from {soundNames.Length} options");
        
        if (delay <= 0f)
        {
            PlaySoundEffectImmediate(selectedSoundName, volume, pitch, randomizePitch, pitchRange, spatialBlend, loop, attachTo, position, minDist, maxDist, eventName);
        }
        else
        {
            Coroutine delayedCoroutine = StartCoroutine(PlaySoundEffectDelayed(delay, selectedSoundName, volume, pitch, randomizePitch, pitchRange, spatialBlend, loop, attachTo, position, minDist, maxDist, eventName));
            delayedSFXCoroutines.Add(delayedCoroutine);
        }
    }

    private void PlaySoundEffectImmediate(string soundName, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, bool loop, Transform attachTo, Vector3 position, float minDist, float maxDist, string eventName)
    {
        Debug.Log($"Playing sound effect '{soundName}' with volume {volume}, pitch {pitch}, spatial blend {spatialBlend}, loop {loop}");
        
        if (!soundEffects.TryGetValue(soundName, out AudioClip clip))
        {
            Debug.LogWarning($"Sound '{soundName}' not found in Resources/Audio/SFX!");
            return;
        }
        
        // Determine position and parent transform
        Vector3 spawnPosition;
        Transform parentTransform;

        if (attachTo != null)
        {
            // Use specified transform position and parent
            spawnPosition = attachTo.position;
            parentTransform = attachTo;
            Debug.Log($"[AudioManager] Attaching SFX to: {attachTo.name}");
        }
        else if (position != default(Vector3))
        {
            // Use provided Vector3 position, parent to AudioManager
            spawnPosition = position;
            parentTransform = transform;
            spatialBlend = Mathf.Max(spatialBlend, 0.1f); // Ensure some 3D when using world position
            Debug.Log($"[AudioManager] Using custom position: {position}");
        }
        else
        {
            // Default: Use AudioManager position and parent
            spawnPosition = transform.position;
            parentTransform = transform;
            Debug.Log($"[AudioManager] Using AudioManager default position with spatialBlend={spatialBlend}");
        }
        
        GameObject sfxObject = Instantiate(soundEffectPrefab, spawnPosition, Quaternion.identity, parentTransform);
        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();
        
        // Rename the GameObject to include the sound name and SFX tag
        sfxObject.name = $"{soundName} (SFX)";
        
        // More detailed naming
        sfxObject.name = $"{soundName} (SFX) - {(loop ? "Loop" : "OneShot")}";

        // Apply basic parameters
        sfxSource.clip = clip;
        sfxSource.volume = volume * globalSFXAttenuation;
        sfxSource.pitch = randomizePitch ? Random.Range(pitch - pitchRange, pitch + pitchRange) * pitch : pitch;
        sfxSource.loop = loop;
        
        // IMPORTANT: Apply spatial blend and 3D settings BEFORE other 3D properties
        sfxSource.spatialBlend = spatialBlend;
        
        // Apply 3D audio settings if spatial
        if (spatialBlend > 0f)
        {
            // Set 3D audio properties
            sfxSource.rolloffMode = AudioRolloffMode.Logarithmic;
            sfxSource.minDistance = minDist;
            sfxSource.maxDistance = maxDist;
            
            // Ensure other 3D settings are properly configured
            sfxSource.spread = 0f; // Directional sound
            sfxSource.dopplerLevel = 1f; // Enable doppler effect
            
            Debug.Log($"[AudioManager] Applied 3D settings: minDist={minDist}, maxDist={maxDist}, rolloff={sfxSource.rolloffMode}");
        }
        else
        {
            // For 2D audio, explicitly set these to ensure no 3D processing
            sfxSource.rolloffMode = AudioRolloffMode.Logarithmic; // This still works for 2D
            sfxSource.minDistance = 1f;
            sfxSource.maxDistance = 500f;
            Debug.Log("[AudioManager] 2D audio - spatial blend = 0");
        }
        
        // Start playing
        sfxSource.Play();

        // Only auto-destroy if not looping
        if (!loop)
        {
            Destroy(sfxObject, clip.length / Mathf.Abs(sfxSource.pitch));
        }
        
        Debug.Log($"[AudioManager] SFX '{soundName}' playing at {spawnPosition} - spatialBlend={sfxSource.spatialBlend}, minDist={sfxSource.minDistance}, maxDist={sfxSource.maxDistance}");
    }

    private IEnumerator PlaySoundEffectDelayed(float delay, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, bool loop, Transform attachTo, Vector3 position, float minDist, float maxDist, string eventName)
    {
        Debug.Log($"[AudioManager] Delaying SFX '{soundName}' for {delay}s");
        yield return new WaitForSeconds(delay);
        
        // Remove this coroutine from tracking list
        delayedSFXCoroutines.RemoveAll(c => c == null);
        
        Debug.Log($"[AudioManager] Executing delayed SFX '{soundName}'");
        PlaySoundEffectImmediate(soundName, volume, pitch, randomizePitch, pitchRange, spatialBlend, loop, attachTo, position, minDist, maxDist, eventName);
    }
    
    // OPTIONAL: Method to cancel all delayed SFX - this is useful for cleanup or resetting
    public void CancelAllDelayedSFX()
    {
        foreach (var coroutine in delayedSFXCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        delayedSFXCoroutines.Clear();
        Debug.Log("[AudioManager] Cancelled all delayed SFX");
    }

    // OPTIONAL: Method to stop all looped SFX (for cleanup)
    public void StopAllLoopedSFX()
    {
        // Find all SFX GameObjects (they'll be children of various transforms) -  WE NEED TO CHECK IF THE NAME CONTAINS "(SFX)" TO AVOID UNINTENDED DESTRUCTION
        
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
    
        foreach (var source in allSources)
        {
            // Check if it's an SFX source (has our prefab structure) and is looping
            if (source.loop && source.gameObject.name.Contains("SFX")) // Adjust name check as needed
            {
                Destroy(source.gameObject);
            }
        }
        Debug.Log("[AudioManager] Stopped all looped SFX");
    }

    // CONVENIENCE OVERLOADS for backwards compatibility and ease of use: TODO: FIND A BETTER WAY TO DO THIS

    // Simple single sound overload
    public void PlaySoundEffect(string soundName, float volume = 1f, float pitch = 1f, bool randomizePitch = false, float pitchRange = 0.1f, float spatialBlend = 0f, bool loop = false, float delay = 0f, float percentChanceToPlay = 100f, Transform attachTo = null, Vector3 position = default, float minDist = 1f, float maxDist = 500f, string eventName = "")
    {
        PlaySoundEffect(new string[] { soundName }, volume, pitch, randomizePitch, pitchRange, spatialBlend, loop, delay, percentChanceToPlay, attachTo, position, minDist, maxDist, eventName);
    }

    // Transform-only positioning (most common use case)
    public void PlaySoundEffectAt(Transform attachTo, string soundName, float volume = 1f, float spatialBlend = 1f, float minDist = 1f, float maxDist = 20f)
    {
        PlaySoundEffect(new string[] { soundName }, volume, 1f, false, 0f, spatialBlend, false, 0f, 100f, attachTo, default, minDist, maxDist, "");
    }

    // Vector3 positioning
    public void PlaySoundEffectAt(Vector3 position, string soundName, float volume = 1f, float spatialBlend = 1f, float minDist = 1f, float maxDist = 20f)
    {
        PlaySoundEffect(new string[] { soundName }, volume, 1f, false, 0f, spatialBlend, false, 0f, 100f, null, position, minDist, maxDist, "");
    }
    #endregion
}