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
    [SerializeField] private AmbientAudioTrack ambientTrack;
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
        AudioEventManager.playAmbientTrack += PlayAmbient;
        AudioEventManager.stopAmbientTrack += StopAmbient;
        AudioEventManager.pauseAmbientTrack += PauseAmbient;
        
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
        AudioEventManager.playAmbientTrack -= PlayAmbient;
        AudioEventManager.stopAmbientTrack -= StopAmbient;
        AudioEventManager.pauseAmbientTrack -= PauseAmbient;
        
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

        AudioClip[] sfxClips = Resources.LoadAll<AudioClip>("Audio/SFX");
        foreach (var clip in sfxClips)
        {
            soundEffects[clip.name] = clip;
            soundEffectNames.Add(clip.name);
        }
        
        AudioClip[] dialogueClips = Resources.LoadAll<AudioClip>("Audio/Dialogue");
        for (int i = 0; i < dialogueClips.Length; i++)
        {
            dialogueAudioTracks[i] = dialogueClips[i];
            dialogueTrackNames.Add(dialogueClips[i].name);
        }
    }
    #endregion

    // Public accessors for tracks to get resources
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

    // Ambient Audio Event Methods (just pass to track)
    public void PlayAmbient(Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, string eventName)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        ambientTrack.Play(trackNumber, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, attachTo);
    }

    public void StopAmbient(float fadeDuration, FadeTarget fadeTarget)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        ambientTrack.Stop(fadeDuration, fadeTarget); //todo: Uncomment when Stop method is implemented
    }

    public void PauseAmbient(float fadeDuration, FadeTarget fadeTarget)
    {
        if (ambientTrack == null)
        {
            Debug.LogError("AmbientTrack reference is null!");
            return;
        }
        //ambientTrack.Pause(fadeDuration, fadeTarget); //todo: Uncomment when Pause method is implemented
        ambientTrack.PauseToggle(fadeDuration, fadeTarget);
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