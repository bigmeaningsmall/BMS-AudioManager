using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    [SerializeField] private AudioTrack ambientTrack;
    //[SerializeField] private BGMAudioTrack bgmTrack;
    //[SerializeField] private DialogueAudioTrack dialogueTrack;
    
    // Audio Resource Dictionaries (KEEP - centralized loading)
    [Header("Audio Resources")]
    private Dictionary<int, AudioClip> musicTracks = new Dictionary<int, AudioClip>();
    private Dictionary<int, AudioClip> ambientAudioTracks = new Dictionary<int, AudioClip>();
    private Dictionary<int, AudioClip> dialogueAudioTracks = new Dictionary<int, AudioClip>();
    private Dictionary<string, AudioClip> soundEffects = new Dictionary<string, AudioClip>();

    // Prefab References (KEEP - tracks will use these)
    [Header("Audio Prefabs")]
    [SerializeField] private GameObject musicPrefab;
    [SerializeField] private GameObject ambientAudioPrefab;
    [SerializeField] private GameObject dialogueAudioPrefab;
    [SerializeField] private GameObject soundEffectPrefab;

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

    [Space(10)]
    // Parameter and Porperty References for tracks - these are for checking and reference
    // Parameters for ambient audio - used for getting current state info
    //[HideInInspector]
    public AudioTrackParamters ambientTrackParamters;
    // private int index = -1; // Track index for identification
    // private string trackName = "Ambient Track"; // Track name for identification
    // private string eventName = "AmbientAudioTrackEvent"; // Event name for identification
    
    /// <summary>
    /// METHODS START HERE ------------------------------------------------------
    /// </summary>
    
    // Singleton Pattern
    #region Initialise Singleton Pattern
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioResources();
        }
        else
        {
            Destroy(gameObject);
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
        AudioEventManager.updateTrack += UpdateTrack;
        
        AudioEventManager.PlaySFX += PlaySoundEffect;
        
        // TODO: Uncomment when BGM/Dialogue tracks are implemented
        // AudioEventManager.playBGMTrack += PlayMusic;
        // AudioEventManager.stopBGMTrack += StopMusic;
        // AudioEventManager.pauseBGMTrack += PauseMusic;
        // AudioEventManager.playDialogueTrack += PlayDialogue;
        // AudioEventManager.stopDialogueTrack += StopDialogueAudio;
        // AudioEventManager.pauseDialogueTrack += PauseDialogue;
    }

    private void OnDisable()
    {
        AudioEventManager.playTrack -= PlayTrack;
        AudioEventManager.stopTrack -= StopTrack;
        AudioEventManager.pauseTrack -= PauseTrack;
        AudioEventManager.updateTrack -= UpdateTrack;
        
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
    public GameObject GetBGMPrefab() => musicPrefab;
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
    public GameObject GetAmbientPrefab() => ambientAudioPrefab;
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
    public GameObject GetDialoguePrefab() => dialogueAudioPrefab;
    //-----------------------------------------------------------
    #endregion
    
    //----------------------------------------------------------
    // Ambient Audio Event Methods (just pass to track)
    public void PlayTrack(AudioTrackType trackType, Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        
        // CALL THE TRACK METHOD
        // This will handle the actual playing of the ambient track
        ambientTrack.Play(trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
        
        // set parameters for the ambient track -- parameters are updated in LateUpdate when fading 
        ambientTrackParamters = new AudioTrackParamters(attachTo, trackNumber, trackName, volume, pitch, spatialBlend, loop, eventName);
        
    }

    public void StopTrack(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        ambientTrack.Stop(fadeDuration, fadeTarget); 
    }

    public void PauseTrack(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        ambientTrack.PauseToggle(fadeDuration, fadeTarget);
    }
    
    //method to update parameters of ambient audio
    public void UpdateTrack(AudioTrackType trackType, Transform attachTo, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        
        // CALL THE TRACK METHOD
        // This will handle the actual updating of the ambient track parameters
        ambientTrack.UpdateParameters(attachTo, volume, pitch, spatialBlend, fadeDuration, fadeTarget, loop, eventName);
        
        // set parameters for the ambient track -- parameters are updated in LateUpdate when fading 
        
        int tNum = ambientTrackParamters.index; // Get the current index from the track
        string tName = ambientTrackParamters.trackName;
        string eName = ambientTrackParamters.eventName;
        ambientTrackParamters = new AudioTrackParamters(attachTo, tNum, tName, volume, pitch, spatialBlend, loop, eName);
    }
    //override UpdateAmbient methods for different parameters
    public void UpdateTrack(AudioTrackType trackType, Transform attachTo){
        
    }

    
    
    //---------------------------------------------------------- ambientTrack.currentState == AmbientState.Playing || 
    
    private void LateUpdate(){
        
        // Update ambient parameters if the track is fading 
        UpdateAmbientParameters();
    }
    
    
    private void UpdateAmbientParameters(){
        
        AudioSource currentSource;
            
        // handle fadeinout and crossfade separately - to decide between cue for crossfade or outgoing for fadein/out
        if (ambientTrack.currentState == AudioTrackState.Crossfading){
            currentSource = ambientTrack.mainSource ? ambientTrack.mainSource : ambientTrack.cueSource;
        }
        else{
            currentSource = ambientTrack.mainSource ? ambientTrack.mainSource : ambientTrack.outgoingSource;
        }
        
        if (currentSource == null)
        {
            Debug.LogWarning("No active audio source found for ambient track.");
            return;
        }
        
        // Update the ambient track parameters based on the current audio source when fading or crossfading
        if (ambientTrack.currentState == AudioTrackState.FadingIn || ambientTrack.currentState == AudioTrackState.FadingOut || ambientTrack.currentState == AudioTrackState.Crossfading){
            ambientTrackParamters.attachedTo = currentSource.transform.parent;
            
            ambientTrackParamters.volume = currentSource.volume;
            ambientTrackParamters.pitch = currentSource.pitch;
            ambientTrackParamters.spatialBlend = currentSource.spatialBlend;
            ambientTrackParamters.loop = currentSource.loop;
            
            ambientTrackParamters.trackName = currentSource.clip != null ? currentSource.clip.name : "No Clip";
            
            //remove "(Clone)" from the track name if it exists
            if (ambientTrackParamters.trackName.Contains("(Clone)"))
            {
                ambientTrackParamters.trackName = ambientTrackParamters.trackName.Replace("(Clone)", "").Trim();
            }
        } 
        
    }
    
    
    // SFX Management (UNCHANGED)
    #region Play Sound Effects
    public void PlaySoundEffect(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, string eventName)
    {
        Debug.Log($"Playing sound effect '{soundName}' with volume {volume}, pitch {pitch}, spatial blend {spatialBlend}");
        
        if (!soundEffects.TryGetValue(soundName, out AudioClip clip))
        {
            Debug.Log($"Sound '{soundName}' not found in Resources/Audio/SFX!");
            return;
        }
        
        if(attachTo == null)
        {
            attachTo = transform;
            spatialBlend = 0;
        }
        
        GameObject sfxObject = Instantiate(soundEffectPrefab, attachTo.position, Quaternion.identity, attachTo);
        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();

        sfxSource.clip = clip;
        sfxSource.volume = volume;
        sfxSource.pitch = randomizePitch ? Random.Range(pitch - pitchRange, pitch + pitchRange) * pitch : pitch;
        sfxSource.spatialBlend = spatialBlend;
        sfxSource.Play();

        Destroy(sfxObject, clip.length / sfxSource.pitch);
    }
    #endregion
}