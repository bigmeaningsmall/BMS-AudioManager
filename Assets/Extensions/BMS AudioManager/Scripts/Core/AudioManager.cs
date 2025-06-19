using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// The AudioManager class is responsible for managing background music and sound effects in the game.
/// It handles loading audio resources, playing, stopping, and pausing background music with fade effects,
/// and playing sound effects with various parameters such as volume, pitch, and spatial blend.
/// This class uses the singleton pattern to ensure only one instance is active at any time.
/// The methods of this class are called via events defined in the AudioEventManager.
/// </summary>

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // --------------------------------------------------------------------------------------------
    [Header("Background Music Track")]
    [SerializeField] private GameObject musicPrefab;
    private float musicFadeDuration = 1.5f;
    private FadeType musicFadeType = FadeType.Crossfade;
    [HideInInspector] public bool isFadingMusic = false; // Flag to prevent multiple fades at once
    private bool isPausedMusic = false; // Tracks if the music is paused
    private float bgmTargetVolume = 1.0f;
    //private float bgmTargetPitch = 1.0f; // todo add pitch control for background music

    private Dictionary<int, AudioClip> musicTracks = new Dictionary<int, AudioClip>();
    private AudioSource currentMusicSource;
    private AudioSource nextMusicSource;

    // --------------------------------------------------------------------------------------------
    
    [Header("Ambient Audio Track")]
    [SerializeField] private GameObject ambientAudioPrefab;
    private float ambientFadeDuration = 1.5f;
    private FadeType ambientFadeType = FadeType.Crossfade;
    [HideInInspector] public bool isFadingAmbientAudio = false; // Flag to prevent multiple fades at once
    private bool isPausedAmbientAudio = false; // Tracks if the ambient audio is paused
    private float ambientTargetVolume = 1.0f;
    //private float ambientTargetPitch = 1.0f; //todo add pitch control for ambient audio 
    
    private Dictionary<int, AudioClip> ambientAudioTracks = new Dictionary<int, AudioClip>();
    private AudioSource currentAmbientAudioSource;
    private AudioSource nextAmbientAudioSource;
    
    // --------------------------------------------------------------------------------------------
    
    [Header("Dialogue Audio Track")] // Currently works the same as Ambient Audio
    [SerializeField] private GameObject dialogueAudioPrefab;
    private float dialogueFadeDuration = 0.5f;
    private FadeType dialogueFadeType = FadeType.Crossfade;
    [HideInInspector] public bool isFadingDialogueAudio = false; // Flag to prevent multiple fades at once
    private bool isPausedDialogueAudio = false; // Tracks if the dialogue audio is paused
    private float dialogueTargetVolume = 1.0f;
    private float dialogueTargetPitch = 1.0f; // todo make this adjustable in the inspector
    
    private Dictionary<int, AudioClip> dialogueAudioTracks = new Dictionary<int, AudioClip>();
    private AudioSource currentDialogueAudioSource;
    private AudioSource nextDialogueAudioSource;

    // --------------------------------------------------------------------------------------------
    
    [Header("Sound Effects Settings")]
    [SerializeField] private GameObject soundEffectPrefab;
    private Dictionary<string, AudioClip> soundEffects = new Dictionary<string, AudioClip>();

    // --------------------------------------------------------------------------------------------
    #region Available Audio Tracks ------------------------------------
    [Header("Available Music Tracks")]
    [SerializeField] private List<string> musicTrackNames = new List<string>();
    
    [Header("Available Ambient Audio Tracks")]
    [SerializeField] private List<string> ambientAudioTrackNames = new List<string>();

    [Header("Available Dialogue Audio Tracks")]
    [SerializeField] private List<string> dialogueTrackNames = new List<string>();
    
    [Header("Available Sound Effects")]
    [SerializeField] private List<string> soundEffectNames = new List<string>();
    #endregion
    // --------------------------------------------------------------------------------------------

    // Initialise and Subscriptions *********************
    // --------------------------------------------------------------------------------------------
    #region Initialise Singleton Pattern ------------------------------------
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
    // --------------------------------------------------------------------------------------------

    // --------------------------------------------------------------------------------------------
    #region Event Subscriptions ------------------------------------
    private void OnEnable()
    {
        AudioEventManager.playBGMTrack += PlayMusic;
        AudioEventManager.stopBGMTrack += StopMusic;
        AudioEventManager.pauseBGMTrack += PauseMusic;
        
        AudioEventManager.playAmbientTrack += PlayAmbientAudio;
        AudioEventManager.stopAmbientTrack += StopAmbientAudio;
        AudioEventManager.pauseAmbientTrack += PauseAmbientAudio;
        
        AudioEventManager.playDialogueTrack += PlayDialogueAudio;
        AudioEventManager.stopDialogueTrack += StopDialogueAudio;
        AudioEventManager.pauseDialogueTrack += PauseDialogueAudio;
        
        AudioEventManager.PlaySFX += PlaySoundEffect;
    }

    private void OnDisable()
    {
        AudioEventManager.playBGMTrack -= PlayMusic;
        AudioEventManager.stopBGMTrack -= StopMusic;
        AudioEventManager.pauseBGMTrack -= PauseMusic;
        
        AudioEventManager.playAmbientTrack -= PlayAmbientAudio;
        AudioEventManager.stopAmbientTrack -= StopAmbientAudio;
        AudioEventManager.pauseAmbientTrack -= PauseAmbientAudio;
        
        AudioEventManager.PlaySFX -= PlaySoundEffect;
    }
    #endregion 
    // --------------------------------------------------------------------------------------------
    
    // --------------------------------------------------------------------------------------------
    #region Load Audio Resources ------------------------------------
    private void LoadAudioResources()
    {
        AudioClip[] bgmClips = Resources.LoadAll<AudioClip>("Audio/BGM");
        for (int i = 0; i < bgmClips.Length; i++)
        {
            musicTracks[i] = bgmClips[i];
            musicTrackNames.Add(bgmClips[i].name);
        }
        
        AudioClip[] ambientClips = Resources.LoadAll<AudioClip>("Audio/Ambient"); // ambient audio - music, drones etc..
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
    // --------------------------------------------------------------------------------------------

    // Background Music (BGM) Management ********************
    // --------------------------------------------------------------------------------------------
    #region Play Background Music ------------------------------------
    
    // Event Method - Play background music by track number or name with optional volume and loop settings - calls appropriate overload based on parameters
    public void PlayMusic(int trackNumber, string trackName, float volume, FadeType fadeType, float fadeDuration, bool loop, string eventName)
    {
        if (isFadingMusic) return; // Block if a fade/crossfade is already in progress
        
        // Block [action] if music is currently paused
        if (isPausedMusic)
        {
            Debug.Log("Cannot [action] background music while paused. Unpause first, then [action].");
            return;
        }
        
        bgmTargetVolume = volume;  // The volume parameter passed to the method

        musicFadeType = fadeType;
        musicFadeDuration = fadeDuration;
        
        if (string.IsNullOrEmpty(trackName) && trackNumber >= 0)
        {
            PlayMusic(trackNumber, volume, loop);
        }
        else if (!string.IsNullOrEmpty(trackName))
        {
            PlayMusic(trackName, volume, loop);
        }
    }
    // Overload - Play background music by track number with optional volume and loop settings
    public void PlayMusic(int trackNumber, float volume, bool loop = true)
    {
        if (isFadingMusic) return; // Block if a fade/crossfade is already in progress
        
        // Block [action] if music is currently paused
        if (isPausedMusic)
        {
            Debug.Log("Cannot [action] background music while paused. Unpause first, then [action].");
            return;
        }

        if (!musicTracks.TryGetValue(trackNumber, out AudioClip newTrack)) return;
        isFadingMusic = true;
        if (musicFadeType == FadeType.Crossfade)
        {
            StartCoroutine(CrossfadeMusic(newTrack, volume, loop));
        }
        else
        {
            StartCoroutine(FadeOutAndInMusic(newTrack, volume, loop));
        }
    }

    public void PlayMusic(string trackName, float volume, bool loop = true)
    {
        if (isFadingMusic) return; // Block if a fade/crossfade is already in progress
        
        // Block [action] if music is currently paused
        if (isPausedMusic)
        {
            Debug.Log("Cannot [action] background music while paused. Unpause first, then [action].");
            return;
        }

        foreach (var track in musicTracks)
        {
            if (track.Value.name == trackName)
            {
                isFadingMusic = true;
                if (musicFadeType == FadeType.Crossfade)
                {
                    StartCoroutine(CrossfadeMusic(track.Value, volume, loop));
                }
                else
                {
                    StartCoroutine(FadeOutAndInMusic(track.Value, volume, loop));
                }
                return;
            }
        }
        Debug.Log($"Music track '{trackName}' not found in Resources/Audio/BGM!");
    }

    private IEnumerator CrossfadeMusic(AudioClip newTrack, float targetVolume, bool loop)
    {
        float crossfadeDuration = musicFadeDuration;

        GameObject musicObject = Instantiate(musicPrefab, transform);
        nextMusicSource = musicObject.GetComponent<AudioSource>();
        nextMusicSource.clip = newTrack;
        nextMusicSource.volume = 0;  // Start volume at 0 for crossfade
        nextMusicSource.loop = loop;
        nextMusicSource.Play();

        if (currentMusicSource != null && currentMusicSource.isPlaying)
        {
            float startVolume = currentMusicSource.volume;
            for (float t = 0; t < crossfadeDuration; t += Time.deltaTime)
            {
                currentMusicSource.volume = Mathf.Lerp(startVolume, 0, t / crossfadeDuration);
                nextMusicSource.volume = Mathf.Lerp(0, targetVolume, t / crossfadeDuration);
                yield return null;
            }
            Destroy(currentMusicSource.gameObject); // Clean up old AudioSource after crossfade
        }

        nextMusicSource.volume = targetVolume;
        currentMusicSource = nextMusicSource;
        isFadingMusic = false; // Reset flag after crossfade completes
    }

    private IEnumerator FadeOutAndInMusic(AudioClip newTrack, float targetVolume, bool loop)
    {
        if (currentMusicSource != null && currentMusicSource.isPlaying)
        {
            float startVolume = currentMusicSource.volume;
            for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
            {
                currentMusicSource.volume = Mathf.Lerp(startVolume, 0, t / musicFadeDuration);
                yield return null;
            }
            currentMusicSource.Stop();
            Destroy(currentMusicSource.gameObject); // Clean up old AudioSource after fade out
        }

        GameObject musicObject = Instantiate(musicPrefab, transform);
        nextMusicSource = musicObject.GetComponent<AudioSource>();
        nextMusicSource.clip = newTrack;
        nextMusicSource.volume = 0;
        nextMusicSource.loop = loop;
        nextMusicSource.Play();

        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            nextMusicSource.volume = Mathf.Lerp(0, targetVolume, t / musicFadeDuration);
            yield return null;
        }

        nextMusicSource.volume = targetVolume;
        currentMusicSource = nextMusicSource;
        isFadingMusic = false; // Reset flag after fade completes
    }
    #endregion
    // --------------------------------------------------------------------------------------------
    
 // --------------------------------------------------------------------------------------------
    #region StopBackgroundMusic ------------------------------------
    public void StopMusic(float fadeDuration)
    {
        // Block stop if music is currently paused
        if (isPausedMusic)
        {
            Debug.Log("Cannot stop background music while paused. Unpause first, then stop.");
            return;
        }

        musicFadeDuration = fadeDuration;
        
        // Check if there's music playing and that it's not already fading
        if (currentMusicSource != null && currentMusicSource.isPlaying && !isFadingMusic)
        {
            StartCoroutine(FadeOutCurrentMusic());
        }
    }

    private IEnumerator FadeOutCurrentMusic()
    {
        isFadingMusic = true;
        float startVolume = currentMusicSource.volume;

        // Fade out over musicFadeDuration
        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            currentMusicSource.volume = Mathf.Lerp(startVolume, 0, t / musicFadeDuration);
            yield return null;
        }

        // Stop and clean up the music source after fade-out
        currentMusicSource.Stop();
        Destroy(currentMusicSource.gameObject);
        currentMusicSource = null;  // Reset the currentMusicSource reference
        isFadingMusic = false; // Allow other fades to proceed
    }
    #endregion
    // --------------------------------------------------------------------------------------------

    // --------------------------------------------------------------------------------------------
    #region PauseBackgroundMusic ------------------------------------
    public void PauseMusic(float fadeDuration)
    {
        // Check if a fade is already in progress to avoid interruptions
        if (isFadingMusic) return;

        musicFadeDuration = fadeDuration; // Set the fade duration for pausing
        
        // Toggle pause state
        if (isPausedMusic)
        {
            // Resume the music with fade-in if currently paused
            StartCoroutine(FadeInMusic());
        }
        else
        {
            // Fade out and pause if currently playing
            StartCoroutine(FadeOutAndPauseMusic());
        }

        isPausedMusic = !isPausedMusic; // Toggle the pause state
    }
    private IEnumerator FadeOutAndPauseMusic()
    {
        isFadingMusic = true;
        float startVolume = currentMusicSource.volume;

        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            currentMusicSource.volume = Mathf.Lerp(startVolume, 0, t / musicFadeDuration);
            yield return null;
        }

        currentMusicSource.Pause(); // Pause the music once fade-out completes
        isFadingMusic = false;
    }
    private IEnumerator FadeInMusic()
    {
        isFadingMusic = true;
        nextMusicSource.UnPause(); // Resume the music before fade-in
    
        // Use the stored target volume instead of hardcoded 1.0f
        float targetVolume = bgmTargetVolume; // This should be stored when BGM is first played
    
        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            nextMusicSource.volume = Mathf.Lerp(0, targetVolume, t / musicFadeDuration);
            yield return null;
        }

        nextMusicSource.volume = targetVolume; // Ensure final volume is set
        isFadingMusic = false;
    }


    #endregion
    // --------------------------------------------------------------------------------------------
    
    // Ambient Audio Management ********************
    // --------------------------------------------------------------------------------------------
    #region PlayAmbientAudio ------------------------------------
    
    // Event Method - Play ambient by track number or name with optional volume and loop settings - calls appropriate overload based on parameters
    public void PlayAmbientAudio(Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, bool loop, string eventName)
    {
        if (isFadingAmbientAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedAmbientAudio)
        {
            Debug.Log("Cannot play ambient audio while paused. Unpause first, then play new track.");
            return;
        }
        
        ambientTargetVolume = volume;  // The volume parameter passed to the method

        ambientFadeType = fadeType;
        ambientFadeDuration = fadeDuration;

        if (string.IsNullOrEmpty(trackName) && trackNumber >= 0)
        {
            PlayAmbientAudio(attachTo, trackNumber, volume, pitch, spatialBlend, loop);
        }
        else if (!string.IsNullOrEmpty(trackName))
        {
            PlayAmbientAudio(attachTo, trackName, volume, pitch, spatialBlend, loop);
        }
    }

    public void PlayAmbientAudio(Transform attachTo, int trackNumber, float volume, float pitch, float spatialBlend, bool loop = true)
    {
        if (isFadingAmbientAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedAmbientAudio)
        {
            Debug.Log("Cannot play ambient audio while paused. Unpause first, then play new track.");
            return;
        }

        if (!ambientAudioTracks.TryGetValue(trackNumber, out AudioClip newTrack)) return;
        isFadingAmbientAudio = true;
        if (ambientFadeType == FadeType.Crossfade)
        {
            StartCoroutine(CrossfadeAmbientAudio(attachTo, newTrack, volume, pitch, spatialBlend, loop));
        }
        else
        {
            StartCoroutine(FadeOutAndInAmbientAudio(attachTo, newTrack, volume, pitch, spatialBlend, loop));
        }
    }

    public void PlayAmbientAudio(Transform attachTo, string trackName, float volume, float pitch, float spatialBlend, bool loop = true)
    {
        if (isFadingAmbientAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedAmbientAudio)
        {
            Debug.Log("Cannot play ambient audio while paused. Unpause first, then play new track.");
            return;
        }

        foreach (var track in ambientAudioTracks)
        {
            if (track.Value.name == trackName)
            {
                isFadingAmbientAudio = true;
                if (ambientFadeType == FadeType.Crossfade)
                {
                    StartCoroutine(CrossfadeAmbientAudio(attachTo, track.Value, volume, pitch, spatialBlend, loop));
                }
                else
                {
                    StartCoroutine(FadeOutAndInAmbientAudio(attachTo, track.Value, volume, pitch, spatialBlend, loop));
                }
                return;
            }
        }
        Debug.Log($"Ambient audio track '{trackName}' not found in Resources/Audio/Ambient!");
    }

private IEnumerator CrossfadeAmbientAudio(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend, bool loop)
{
    float crossfadeDuration = ambientFadeDuration;

    if (attachTo == null)
    {
        attachTo = transform; // Default to AudioManager's transform if attachTo is null
    }

    GameObject ambientObject = Instantiate(ambientAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
    nextAmbientAudioSource = ambientObject.GetComponent<AudioSource>();
    nextAmbientAudioSource.clip = newTrack;
    nextAmbientAudioSource.volume = 0;  // Start volume at 0 for crossfade
    nextAmbientAudioSource.pitch = targetPitch;
    nextAmbientAudioSource.spatialBlend = targetSpatialBlend;
    nextAmbientAudioSource.loop = loop;
    nextAmbientAudioSource.Play();

    if (currentAmbientAudioSource != null && currentAmbientAudioSource.isPlaying)
    {
        float startVolume = currentAmbientAudioSource.volume;
        float startPitch = currentAmbientAudioSource.pitch;
        float startSpatialBlend = currentAmbientAudioSource.spatialBlend;
        for (float t = 0; t < crossfadeDuration; t += Time.deltaTime)
        {
            currentAmbientAudioSource.volume = Mathf.Lerp(startVolume, 0, t / crossfadeDuration);
            nextAmbientAudioSource.volume = Mathf.Lerp(0, targetVolume, t / crossfadeDuration);
            nextAmbientAudioSource.pitch = Mathf.Lerp(startPitch, targetPitch, t / crossfadeDuration);
            nextAmbientAudioSource.spatialBlend = Mathf.Lerp(startSpatialBlend, targetSpatialBlend, t / crossfadeDuration);
            yield return null;
        }
        Destroy(currentAmbientAudioSource.gameObject); // Clean up old AudioSource after crossfade
    }

    nextAmbientAudioSource.volume = targetVolume;
    currentAmbientAudioSource = nextAmbientAudioSource;
    isFadingAmbientAudio = false; // Reset flag after crossfade completes
}

private IEnumerator FadeOutAndInAmbientAudio(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend, bool loop)
{
    if (attachTo == null)
    {
        attachTo = transform; // Default to AudioManager's transform if attachTo is null
    }

    if (currentAmbientAudioSource != null && currentAmbientAudioSource.isPlaying)
    {
        float startVolume = currentAmbientAudioSource.volume;
        
        for (float t = 0; t < ambientFadeDuration; t += Time.deltaTime)
        {
            currentAmbientAudioSource.volume = Mathf.Lerp(startVolume, 0, t / ambientFadeDuration);
            yield return null;
        }
        currentAmbientAudioSource.Stop();
        Destroy(currentAmbientAudioSource.gameObject); // Clean up old AudioSource after fade out
    }

    GameObject ambientObject = Instantiate(ambientAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
    nextAmbientAudioSource = ambientObject.GetComponent<AudioSource>();
    nextAmbientAudioSource.clip = newTrack;
    nextAmbientAudioSource.volume = 0;
    nextAmbientAudioSource.pitch = targetPitch;
    nextAmbientAudioSource.spatialBlend = targetSpatialBlend;
    nextAmbientAudioSource.loop = loop;
    nextAmbientAudioSource.Play();

    for (float t = 0; t < ambientFadeDuration; t += Time.deltaTime)
    {
        nextAmbientAudioSource.volume = Mathf.Lerp(0, targetVolume, t / ambientFadeDuration);
        nextAmbientAudioSource.pitch = Mathf.Lerp(0, targetPitch, t / ambientFadeDuration);
        nextAmbientAudioSource.spatialBlend = Mathf.Lerp(0, targetSpatialBlend, t / ambientFadeDuration);
        yield return null;
    }

    nextAmbientAudioSource.volume = targetVolume;
    currentAmbientAudioSource = nextAmbientAudioSource;
    isFadingAmbientAudio = false; // Reset flag after fade completes
}
    #endregion
    // --------------------------------------------------------------------------------------------
    
  // --------------------------------------------------------------------------------------------
    #region StopAmbientAudio ------------------------------------
    public void StopAmbientAudio(float fadeDuration)
    {
        // Block stop if audio is currently paused
        if (isPausedAmbientAudio)
        {
            Debug.Log("Cannot stop ambient audio while paused. Unpause first, then stop.");
            return;
        }

        ambientFadeDuration = fadeDuration;

        // Check if there's ambient audio playing and that it's not already fading
        if (currentAmbientAudioSource != null && currentAmbientAudioSource.isPlaying && !isFadingAmbientAudio)
        {
            StartCoroutine(FadeOutCurrentAmbientAudio());
        }
    }

    private IEnumerator FadeOutCurrentAmbientAudio()
    {
        isFadingAmbientAudio = true;
        float startVolume = currentAmbientAudioSource.volume;

        // Fade out over ambientFadeDuration
        for (float t = 0; t < ambientFadeDuration; t += Time.deltaTime)
        {
            currentAmbientAudioSource.volume = Mathf.Lerp(startVolume, 0, t / ambientFadeDuration);
            yield return null;
        }

        // Stop and clean up the ambient audio source after fade-out
        currentAmbientAudioSource.Stop();
        Destroy(currentAmbientAudioSource.gameObject);
        currentAmbientAudioSource = null;  // Reset the currentAmbientAudioSource reference
        isFadingAmbientAudio = false; // Allow other fades to proceed
    }
    #endregion
    // --------------------------------------------------------------------------------------------
    
    // --------------------------------------------------------------------------------------------
    #region PauseAmbientAudio ------------------------------------
    public void PauseAmbientAudio(float fadeDuration)
    {
        // Check if a fade is already in progress to avoid interruptions
        if (isFadingAmbientAudio) return;

        ambientFadeDuration = fadeDuration; // Set the fade duration for pausing
    
        // Toggle pause state
        if (isPausedAmbientAudio)
        {
            // Resume the ambient audio with fade-in if currently paused
            StartCoroutine(FadeInAmbientAudio());
        }
        else
        {
            // Fade out and pause if currently playing
            StartCoroutine(FadeOutAndPauseAmbientAudio());
        }

        isPausedAmbientAudio = !isPausedAmbientAudio; // Toggle the pause state
    }

    private IEnumerator FadeOutAndPauseAmbientAudio()
    {
        isFadingAmbientAudio = true;
        float startVolume = currentAmbientAudioSource.volume;

        for (float t = 0; t < ambientFadeDuration; t += Time.deltaTime)
        {
            currentAmbientAudioSource.volume = Mathf.Lerp(startVolume, 0, t / ambientFadeDuration);
            yield return null;
        }

        currentAmbientAudioSource.Pause(); // Pause the ambient audio once fade-out completes
        isFadingAmbientAudio = false;
    }

    private IEnumerator FadeInAmbientAudio()
    {
        isFadingAmbientAudio = true;
        currentAmbientAudioSource.UnPause(); // Resume the ambient audio before fade-in
    
        // Use the stored target volume instead of hardcoded 1.0f
        float targetVolume = ambientTargetVolume; // This should be stored when ambient is first played
    
        for (float t = 0; t < ambientFadeDuration; t += Time.deltaTime)
        {
            currentAmbientAudioSource.volume = Mathf.Lerp(0, targetVolume, t / ambientFadeDuration);
            yield return null;
        }

        currentAmbientAudioSource.volume = targetVolume; // Ensure final volume is set
        isFadingAmbientAudio = false;
    }

    #endregion
    // --------------------------------------------------------------------------------------------
    
    // Dialog Management *******************
    // --------------------------------------------------------------------------------------------
    #region PlayDialogueAudio ------------------------------------
    
    // Event Method - Play dialogue by track number or name with optional volume settings - calls appropriate overload based on parameters
    public void PlayDialogueAudio(Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, string eventName)
    {
        if (isFadingDialogueAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot play dialogue audio while paused. Unpause first, then play new track.");
            return;
        }
        
        dialogueTargetVolume = volume;  // The volume parameter passed to the method
        dialogueTargetPitch = pitch;    // The pitch parameter passed to the method


        dialogueFadeType = fadeType;
        dialogueFadeDuration = fadeDuration;

        if (string.IsNullOrEmpty(trackName) && trackNumber >= 0)
        {
            PlayDialogueAudio(attachTo, trackNumber, volume, pitch, spatialBlend);
        }
        else if (!string.IsNullOrEmpty(trackName))
        {
            PlayDialogueAudio(attachTo, trackName, volume, pitch, spatialBlend);
        }
    }

    public void PlayDialogueAudio(Transform attachTo, int trackNumber, float volume, float pitch, float spatialBlend)
    {
        if (isFadingDialogueAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot play dialogue audio while paused. Unpause first, then play new track.");
            return;
        }

        if (!dialogueAudioTracks.TryGetValue(trackNumber, out AudioClip newTrack)) return;
        isFadingDialogueAudio = true;
        if (dialogueFadeType == FadeType.Crossfade)
        {
            StartCoroutine(CrossfadeDialogueAudio(attachTo, newTrack, volume, pitch, spatialBlend));
        }
        else
        {
            StartCoroutine(FadeOutAndInDialogueAudio(attachTo, newTrack, volume, pitch, spatialBlend));
        }
    }

    public void PlayDialogueAudio(Transform attachTo, string trackName, float volume, float pitch, float spatialBlend)
    {
        if (isFadingDialogueAudio) return; // Block if a fade/crossfade is already in progress
        
        // Block play if audio is currently paused
        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot play dialogue audio while paused. Unpause first, then play new track.");
            return;
        }

        foreach (var track in dialogueAudioTracks)
        {
            if (track.Value.name == trackName)
            {
                isFadingDialogueAudio = true;
                if (dialogueFadeType == FadeType.Crossfade)
                {
                    StartCoroutine(CrossfadeDialogueAudio(attachTo, track.Value, volume, pitch, spatialBlend));
                }
                else
                {
                    StartCoroutine(FadeOutAndInDialogueAudio(attachTo, track.Value, volume, pitch, spatialBlend));
                }
                return;
            }
        }
        Debug.Log($"Dialogue audio track '{trackName}' not found in Resources/Audio/Dialogue!");
    }

private IEnumerator CrossfadeDialogueAudio(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend)
{
    float crossfadeDuration = dialogueFadeDuration;

    if (attachTo == null)
    {
        attachTo = transform; // Default to AudioManager's transform if attachTo is null
    }

    GameObject dialogueObject = Instantiate(dialogueAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
    nextDialogueAudioSource = dialogueObject.GetComponent<AudioSource>();
    nextDialogueAudioSource.clip = newTrack;
    nextDialogueAudioSource.volume = 0;  // Start volume at 0 for crossfade
    nextDialogueAudioSource.pitch = targetPitch;
    nextDialogueAudioSource.spatialBlend = targetSpatialBlend;
    nextDialogueAudioSource.Play();

    if (currentDialogueAudioSource != null && currentDialogueAudioSource.isPlaying)
    {
        float startVolume = currentDialogueAudioSource.volume;
        float startPitch = currentDialogueAudioSource.pitch;
        float startSpatialBlend = currentDialogueAudioSource.spatialBlend;
        for (float t = 0; t < crossfadeDuration; t += Time.deltaTime)
        {
            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, t / crossfadeDuration);
            nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, t / crossfadeDuration);
            nextDialogueAudioSource.pitch = Mathf.Lerp(startPitch, targetPitch, t / crossfadeDuration);
            nextDialogueAudioSource.spatialBlend = Mathf.Lerp(startSpatialBlend, targetSpatialBlend, t / crossfadeDuration);
            yield return null;
        }
        Destroy(currentDialogueAudioSource.gameObject); // Clean up old AudioSource after crossfade
    }

    nextDialogueAudioSource.volume = targetVolume;
    currentDialogueAudioSource = nextDialogueAudioSource;
    isFadingDialogueAudio = false; // Reset flag after crossfade completes
}

private IEnumerator FadeOutAndInDialogueAudio(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend)
{
    if (attachTo == null)
    {
        attachTo = transform; // Default to AudioManager's transform if attachTo is null
    }

    if (currentDialogueAudioSource != null && currentDialogueAudioSource.isPlaying)
    {
        float startVolume = currentDialogueAudioSource.volume;
        float startPitch = currentDialogueAudioSource.pitch;
        
        for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
        {
            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, t / dialogueFadeDuration);
            currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, t / dialogueFadeDuration);
            yield return null;
        }
        currentDialogueAudioSource.Stop();
        Destroy(currentDialogueAudioSource.gameObject); // Clean up old AudioSource after fade out
    }

    GameObject dialogueObject = Instantiate(dialogueAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
    nextDialogueAudioSource = dialogueObject.GetComponent<AudioSource>();
    nextDialogueAudioSource.clip = newTrack;
    nextDialogueAudioSource.volume = 0;
    nextDialogueAudioSource.pitch = targetPitch;
    nextDialogueAudioSource.spatialBlend = targetSpatialBlend;
    nextDialogueAudioSource.Play();

    for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
    {
        nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, t / dialogueFadeDuration);
        nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, t / dialogueFadeDuration);
        nextDialogueAudioSource.spatialBlend = Mathf.Lerp(0, targetSpatialBlend, t / dialogueFadeDuration);
        yield return null;
    }

    nextDialogueAudioSource.volume = targetVolume;
    currentDialogueAudioSource = nextDialogueAudioSource;
    isFadingDialogueAudio = false; // Reset flag after fade completes
}
    #endregion
    // --------------------------------------------------------------------------------------------
    
  // --------------------------------------------------------------------------------------------
    #region StopDialogueAudio ------------------------------------
    public void StopDialogueAudio(float fadeDuration)
    {
        // Block stop if audio is currently paused
        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot stop dialogue audio while paused. Unpause first, then stop.");
            return;
        }

        dialogueFadeDuration = fadeDuration;

        // Check if there's dialogue audio playing and that it's not already fading
        if (currentDialogueAudioSource != null && currentDialogueAudioSource.isPlaying && !isFadingDialogueAudio)
        {
            StartCoroutine(FadeOutCurrentDialogueAudio());
        }
    }

    private IEnumerator FadeOutCurrentDialogueAudio()
    {
        isFadingDialogueAudio = true;
        float startVolume = currentDialogueAudioSource.volume;
        float startPitch = currentDialogueAudioSource.pitch;

        // Fade out over dialogueFadeDuration
        for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
        {
            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, t / dialogueFadeDuration);
            currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, t / dialogueFadeDuration);
            yield return null;
        }

        // Stop and clean up the dialogue audio source after fade-out
        currentDialogueAudioSource.Stop();
        Destroy(currentDialogueAudioSource.gameObject);
        currentDialogueAudioSource = null;  // Reset the currentDialogueAudioSource reference
        isFadingDialogueAudio = false; // Allow other fades to proceed
    }
    #endregion
    // --------------------------------------------------------------------------------------------
    
    // --------------------------------------------------------------------------------------------
    #region PauseDialogueAudio ------------------------------------
    public void PauseDialogueAudio(float fadeDuration)
    {
        // Check if a fade is already in progress to avoid interruptions
        if (isFadingDialogueAudio) return;

        dialogueFadeDuration = fadeDuration; // Set the fade duration for pausing
    
        // Toggle pause state
        if (isPausedDialogueAudio)
        {
            // Resume the dialogue audio with fade-in if currently paused
            StartCoroutine(FadeInDialogueAudio());
        }
        else
        {
            // Fade out and pause if currently playing
            StartCoroutine(FadeOutAndPauseDialogueAudio());
        }

        isPausedDialogueAudio = !isPausedDialogueAudio; // Toggle the pause state
    }

    private IEnumerator FadeOutAndPauseDialogueAudio()
    {
        isFadingDialogueAudio = true;
        float startVolume = currentDialogueAudioSource.volume;
        float startPitch = currentDialogueAudioSource.pitch;

        for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
        {
            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, t / dialogueFadeDuration);
            currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, t / dialogueFadeDuration);
            yield return null;
        }

        currentDialogueAudioSource.Pause(); // Pause the dialogue audio once fade-out completes
        isFadingDialogueAudio = false;
    }

    private IEnumerator FadeInDialogueAudio()
    {
        isFadingDialogueAudio = true;
        currentDialogueAudioSource.UnPause(); // Resume the dialogue audio before fade-in
    
        // Use the stored target values instead of hardcoded 1.0f
        float targetVolume = dialogueTargetVolume; // This should be stored when dialogue is first played
        float targetPitch = dialogueTargetPitch;   // This should be stored when dialogue is first played

        for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
        {
            currentDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, t / dialogueFadeDuration);
            currentDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, t / dialogueFadeDuration);
            yield return null;
        }

        currentDialogueAudioSource.volume = targetVolume; // Ensure final volume is set
        currentDialogueAudioSource.pitch = targetPitch;   // Ensure final pitch is set
        isFadingDialogueAudio = false;
    }

    #endregion
    // --------------------------------------------------------------------------------------------
    
    // SFX Management ********************
    // --------------------------------------------------------------------------------------------
    #region PlaySoundEffects ------------------------------------
    public void PlaySoundEffect(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, string eventName)
    {
        
        Debug.Log($"Playing sound effect '{soundName}' with volume {volume}, pitch {pitch}, spatial blend {spatialBlend}");
        
        // Check if the sound effect exists in the dictionary
        if (!soundEffects.TryGetValue(soundName, out AudioClip clip))
        {
            Debug.Log($"Sound '{soundName}' not found in Resources/Audio/SFX!");
            return;
        }
        
        // If no transform is provided, play the sound at the AudioManager's position with no spatial blend
        if(attachTo == null)
        {
            attachTo = transform;
            spatialBlend = 0;
        }
        
        // Create a new GameObject to play the sound effect 
        GameObject sfxObject = Instantiate(soundEffectPrefab, attachTo.position, Quaternion.identity, attachTo);
        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();

        // Set the AudioSource properties and play the sound effect
        sfxSource.clip = clip;
        sfxSource.volume = volume;
        sfxSource.pitch = randomizePitch ? Random.Range(pitch - pitchRange, pitch + pitchRange) * pitch : pitch;
        sfxSource.spatialBlend = spatialBlend;
        sfxSource.Play();

        // Destroy the GameObject after the sound effect has finished playing
        Destroy(sfxObject, clip.length / sfxSource.pitch);
    }
    #endregion
    // --------------------------------------------------------------------------------------------
}
