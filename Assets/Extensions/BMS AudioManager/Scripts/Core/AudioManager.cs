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
    [Header("VERSION")] // Version of the AudioManager - TODO : always update this when making changes
    [SerializeField] private string version = "v1.2.1";
    public static AudioManager Instance { get; private set; }

    // --------------------------------------------------------------------------------------------
    [Header("Background Music Track")]
    [SerializeField] private GameObject musicPrefab;
    private float musicFadeDuration = 1.5f;
    private FadeType musicFadeType = FadeType.Crossfade;
    [HideInInspector] public bool isFadingMusic = false; // Flag to prevent multiple fades at once
    private bool isPausedMusic = false; // Tracks if the music is paused
    private float bgmTargetVolume = 1.0f;
    private float bgmTargetPitch = 1.0f; // todo add pitch control for background music

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
    private float ambientTargetPitch = 1.0f; //todo add pitch control for ambient audio 
    
    private Dictionary<int, AudioClip> ambientAudioTracks = new Dictionary<int, AudioClip>();
    private AudioSource currentAmbientAudioSource;
    private AudioSource nextAmbientAudioSource;
    
    // --------------------------------------------------------------------------------------------
    
    //TODO (Adding note here so i remember) - Need to autodestroy dialogue audio sources after they finish playing.
    
    [Header("Dialogue Audio Track")] // Currently works the same as Ambient Audio
    [SerializeField] private GameObject dialogueAudioPrefab;
    
    private FadeType dialogueFadeType = FadeType.Crossfade;
    [HideInInspector] public bool isFadingDialogueAudio = false; // Flag to prevent multiple fades at once
    private bool isPausedDialogueAudio = false; // Tracks if the dialogue audio is paused
    private float dialogueTargetVolume = 1.0f;
    private float dialogueTargetPitch = 1.0f; 
    private float dialogueFadeDuration = 0.5f;
    private FadeTarget dialogueFadeTarget = FadeTarget.FadeBoth;
    
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
        
        AudioEventManager.playAmbientTrack += PlayAmbient;
        AudioEventManager.stopAmbientTrack += StopAmbient;
        AudioEventManager.pauseAmbientTrack += PauseAmbient;
        
        AudioEventManager.playDialogueTrack += PlayDialogue;
        AudioEventManager.stopDialogueTrack += StopDialogueAudio;
        AudioEventManager.pauseDialogueTrack += PauseDialogue;
        
        AudioEventManager.PlaySFX += PlaySoundEffect;
    }

    private void OnDisable()
    {
        AudioEventManager.playBGMTrack -= PlayMusic;
        AudioEventManager.stopBGMTrack -= StopMusic;
        AudioEventManager.pauseBGMTrack -= PauseMusic;
        
        AudioEventManager.playAmbientTrack -= PlayAmbient;
        AudioEventManager.stopAmbientTrack -= StopAmbient;
        AudioEventManager.pauseAmbientTrack -= PauseAmbient;
        
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
    #region Play Music ------------------------------------
    
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
    #region Stop Music ------------------------------------
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
    #region Pause Music ------------------------------------
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
    #region Play Ambient ------------------------------------
    
    // Event Method - Play ambient by track number or name with optional volume and loop settings - calls appropriate overload based on parameters
    public void PlayAmbient(Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, bool loop, string eventName)
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
            PlayAmbient(attachTo, trackNumber, volume, pitch, spatialBlend, loop);
        }
        else if (!string.IsNullOrEmpty(trackName))
        {
            PlayAmbient(attachTo, trackName, volume, pitch, spatialBlend, loop);
        }
    }

    public void PlayAmbient(Transform attachTo, int trackNumber, float volume, float pitch, float spatialBlend, bool loop = true)
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
            StartCoroutine(CrossfadeAmbient(attachTo, newTrack, volume, pitch, spatialBlend, loop));
        }
        else
        {
            StartCoroutine(FadeOutAndInAmbient(attachTo, newTrack, volume, pitch, spatialBlend, loop));
        }
    }

    public void PlayAmbient(Transform attachTo, string trackName, float volume, float pitch, float spatialBlend, bool loop = true)
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
                    StartCoroutine(CrossfadeAmbient(attachTo, track.Value, volume, pitch, spatialBlend, loop));
                }
                else
                {
                    StartCoroutine(FadeOutAndInAmbient(attachTo, track.Value, volume, pitch, spatialBlend, loop));
                }
                return;
            }
        }
        Debug.Log($"Ambient audio track '{trackName}' not found in Resources/Audio/Ambient!");
    }

    private IEnumerator CrossfadeAmbient(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend, bool loop)
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
                //nextAmbientAudioSource.pitch = Mathf.Lerp(startPitch, targetPitch, t / crossfadeDuration); // todo - Planned update to have pitch and volume controled seperately - disabled for now
                nextAmbientAudioSource.spatialBlend = Mathf.Lerp(startSpatialBlend, targetSpatialBlend, t / crossfadeDuration); //todo - dont need this to be lerped but leaving until i have time to check and delete
                yield return null;
            }
            Destroy(currentAmbientAudioSource.gameObject); // Clean up old AudioSource after crossfade
        }

        nextAmbientAudioSource.volume = targetVolume;
        currentAmbientAudioSource = nextAmbientAudioSource;
        isFadingAmbientAudio = false; // Reset flag after crossfade completes
    }

    private IEnumerator FadeOutAndInAmbient(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend, bool loop)
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
    #region Stop Ambient ------------------------------------
    public void StopAmbient(float fadeDuration)
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
            StartCoroutine(FadeOutCurrentAmbient());
        }
    }

    private IEnumerator FadeOutCurrentAmbient()
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
    #region Pause Ambient ------------------------------------
    public void PauseAmbient(float fadeDuration)
    {
        // Check if a fade is already in progress to avoid interruptions
        if (isFadingAmbientAudio) return;

        ambientFadeDuration = fadeDuration; // Set the fade duration for pausing
    
        // Toggle pause state
        if (isPausedAmbientAudio)
        {
            // Resume the ambient audio with fade-in if currently paused
            StartCoroutine(FadeInAmbient());
        }
        else
        {
            // Fade out and pause if currently playing
            StartCoroutine(FadeOutAndPauseAmbient());
        }

        isPausedAmbientAudio = !isPausedAmbientAudio; // Toggle the pause state
    }

    private IEnumerator FadeOutAndPauseAmbient()
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

    private IEnumerator FadeInAmbient()
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
    #region Play Dialogue ------------------------------------
    
    // Event Method - Play dialogue by track number or name with optional volume settings - calls appropriate overload based on parameters
    public void PlayDialogue(Transform attachTo, int trackNumber, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, string eventName)
    {
        // Handle override behavior
        if (isFadingDialogueAudio) 
        {
            Debug.Log("Interrupting current dialogue fade for new dialogue");
            StopAllCoroutines(); // Kill current fade
            isFadingDialogueAudio = false;
            // Continue to play new dialogue...
        }

        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot play dialogue audio while paused. Unpause first, then play new track.");
            return;
        }

        dialogueTargetVolume = volume;
        dialogueTargetPitch = pitch;
        dialogueFadeType = fadeType;
        dialogueFadeDuration = fadeDuration;
        dialogueFadeTarget = fadeTarget;

        if (string.IsNullOrEmpty(trackName) && trackNumber >= 0)
        {
            PlayDialogue(attachTo, trackNumber, volume, pitch, spatialBlend);
        }
        else if (!string.IsNullOrEmpty(trackName))
        {
            PlayDialogue(attachTo, trackName, volume, pitch, spatialBlend);
        }
    }

    public void PlayDialogue(Transform attachTo, int trackNumber, float volume, float pitch, float spatialBlend)
    {
        // Handle override behavior
        if (isFadingDialogueAudio) 
        {
            StopAllCoroutines();
            isFadingDialogueAudio = false;
        }
    
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
            StartCoroutine(CrossfadeDialogue(attachTo, newTrack, volume, pitch, spatialBlend));
        }
        else
        {
            StartCoroutine(FadeOutAndInDialogue(attachTo, newTrack, volume, pitch, spatialBlend));
        }
    }

    public void PlayDialogue(Transform attachTo, string trackName, float volume, float pitch, float spatialBlend)
    {
        // Handle override behavior
        if (isFadingDialogueAudio) 
        {
            StopAllCoroutines();
            isFadingDialogueAudio = false;
        }
    
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
                    StartCoroutine(CrossfadeDialogue(attachTo, track.Value, volume, pitch, spatialBlend));
                }
                else
                {
                    StartCoroutine(FadeOutAndInDialogue(attachTo, track.Value, volume, pitch, spatialBlend));
                }
                return;
            }
        }
        Debug.Log($"Dialogue audio track '{trackName}' not found in Resources/Audio/Dialogue!");
    }
    
    private IEnumerator CrossfadeDialogue(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend)
    {
        if (attachTo == null)
        {
            attachTo = transform;
        }

        GameObject dialogueObject = Instantiate(dialogueAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
        nextDialogueAudioSource = dialogueObject.GetComponent<AudioSource>();
        nextDialogueAudioSource.clip = newTrack;
        nextDialogueAudioSource.volume = 0;
        nextDialogueAudioSource.pitch = 0;
        nextDialogueAudioSource.spatialBlend = targetSpatialBlend;
        nextDialogueAudioSource.Play();

        if (currentDialogueAudioSource != null && currentDialogueAudioSource.isPlaying)
        {
            float startVolume = currentDialogueAudioSource.volume;
            float startPitch = currentDialogueAudioSource.pitch;
            
            // Handle instant changes for Ignore
            if (dialogueFadeTarget == FadeTarget.Ignore)
            {
                currentDialogueAudioSource.volume = 0;
                currentDialogueAudioSource.pitch = startPitch;
                nextDialogueAudioSource.volume = targetVolume;
                nextDialogueAudioSource.pitch = targetPitch;
                Destroy(currentDialogueAudioSource.gameObject);
            }
            else
            {
                // Fade over time
                for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
                {
                    float progress = t / dialogueFadeDuration;
                    
                    switch (dialogueFadeTarget)
                    {
                        case FadeTarget.FadeVolume:
                            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                            nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                            currentDialogueAudioSource.pitch = startPitch;
                            nextDialogueAudioSource.pitch = targetPitch;
                            break;
                            
                        case FadeTarget.FadePitch:
                            currentDialogueAudioSource.volume = 0;
                            nextDialogueAudioSource.volume = targetVolume;
                            currentDialogueAudioSource.pitch = startPitch;
                            nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                            break;
                            
                        case FadeTarget.FadeBoth:
                            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                            nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                            currentDialogueAudioSource.pitch = startPitch;
                            nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                            break;
                    }
                    
                    yield return null;
                }
                Destroy(currentDialogueAudioSource.gameObject);
            }
        }
        else
        {
            // No current source, just fade in the new one
            if (dialogueFadeTarget == FadeTarget.Ignore)
            {
                nextDialogueAudioSource.volume = targetVolume;
                nextDialogueAudioSource.pitch = targetPitch;
            }
            else
            {
                for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
                {
                    float progress = t / dialogueFadeDuration;
                    
                    switch (dialogueFadeTarget)
                    {
                        case FadeTarget.FadeVolume:
                            nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                            nextDialogueAudioSource.pitch = targetPitch;
                            break;
                            
                        case FadeTarget.FadePitch:
                            nextDialogueAudioSource.volume = targetVolume;
                            nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                            break;
                            
                        case FadeTarget.FadeBoth:
                            nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                            nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                            break;
                    }
                    
                    yield return null;
                }
            }
        }

        // Ensure final values are set
        nextDialogueAudioSource.volume = targetVolume;
        nextDialogueAudioSource.pitch = targetPitch;
        currentDialogueAudioSource = nextDialogueAudioSource;
        isFadingDialogueAudio = false;
    }
        
    private IEnumerator FadeOutAndInDialogue(Transform attachTo, AudioClip newTrack, float targetVolume, float targetPitch, float targetSpatialBlend)
    {
        if (attachTo == null)
        {
            attachTo = transform;
        }

        // === FADE OUT PHASE ===
        if (currentDialogueAudioSource != null && currentDialogueAudioSource.isPlaying)
        {
            float startVolume = currentDialogueAudioSource.volume;
            float startPitch = currentDialogueAudioSource.pitch;
            
            if (dialogueFadeTarget == FadeTarget.Ignore)
            {
                currentDialogueAudioSource.volume = 0;
                currentDialogueAudioSource.pitch = 0;
            }
            else
            {
                for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
                {
                    float progress = t / dialogueFadeDuration;
                    
                    switch (dialogueFadeTarget)
                    {
                        case FadeTarget.FadeVolume:
                            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                            currentDialogueAudioSource.pitch = startPitch;
                            break;
                            
                        case FadeTarget.FadePitch:
                            currentDialogueAudioSource.volume = startVolume;
                            currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                            break;
                            
                        case FadeTarget.FadeBoth:
                            currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                            currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                            break;
                    }
                    
                    yield return null;
                }
            }
            currentDialogueAudioSource.Stop();
            Destroy(currentDialogueAudioSource.gameObject);
        }

        // === FADE IN PHASE ===
        GameObject dialogueObject = Instantiate(dialogueAudioPrefab, attachTo.position, Quaternion.identity, attachTo);
        nextDialogueAudioSource = dialogueObject.GetComponent<AudioSource>();
        nextDialogueAudioSource.clip = newTrack;
        nextDialogueAudioSource.volume = 0;
        nextDialogueAudioSource.pitch = 0;
        nextDialogueAudioSource.spatialBlend = targetSpatialBlend;
        nextDialogueAudioSource.Play();

        if (dialogueFadeTarget == FadeTarget.Ignore)
        {
            nextDialogueAudioSource.volume = targetVolume;
            nextDialogueAudioSource.pitch = targetPitch;
        }
        else
        {
            for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
            {
                float progress = t / dialogueFadeDuration;
                
                switch (dialogueFadeTarget)
                {
                    case FadeTarget.FadeVolume:
                        nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                        nextDialogueAudioSource.pitch = targetPitch;
                        break;
                        
                    case FadeTarget.FadePitch:
                        nextDialogueAudioSource.volume = targetVolume;
                        nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                        break;
                        
                    case FadeTarget.FadeBoth:
                        nextDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                        nextDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                        break;
                }
                
                yield return null;
            }
        }

        // Ensure final values are set
        nextDialogueAudioSource.volume = targetVolume;
        nextDialogueAudioSource.pitch = targetPitch;
        currentDialogueAudioSource = nextDialogueAudioSource;
        isFadingDialogueAudio = false;
    } 
    
    #endregion
    // --------------------------------------------------------------------------------------------
    
  // --------------------------------------------------------------------------------------------
    #region Stop Dialogue ------------------------------------
    public void StopDialogueAudio(float fadeDuration, FadeTarget fadeTarget)
    {
        if (isPausedDialogueAudio)
        {
            Debug.Log("Cannot stop dialogue audio while paused. Unpause first, then stop.");
            return;
        }

        dialogueFadeDuration = fadeDuration;
        dialogueFadeTarget = fadeTarget;

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

        if (dialogueFadeTarget == FadeTarget.Ignore)
        {
            currentDialogueAudioSource.volume = 0;
            currentDialogueAudioSource.pitch = 0;
        }
        else
        {
            for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
            {
                float progress = t / dialogueFadeDuration;
            
                switch (dialogueFadeTarget)
                {
                    case FadeTarget.FadeVolume:
                        currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                        currentDialogueAudioSource.pitch = startPitch;
                        break;
                    
                    case FadeTarget.FadePitch:
                        currentDialogueAudioSource.volume = startVolume;
                        currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                        break;
                    
                    case FadeTarget.FadeBoth:
                        currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                        currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                        break;
                }
            
                yield return null;
            }
        }
        currentDialogueAudioSource.GetComponent<AutoDestroyAudioSource>()?.SetPausedStatus(false);
        currentDialogueAudioSource.Stop();
        Destroy(currentDialogueAudioSource.gameObject);
        currentDialogueAudioSource = null;
        isFadingDialogueAudio = false;
    }
    
    #endregion
    // --------------------------------------------------------------------------------------------
    
    // --------------------------------------------------------------------------------------------
    #region Pause Dialogue ------------------------------------
    public void PauseDialogue(float fadeDuration, FadeTarget fadeTarget)
    {
        //if (isFadingDialogueAudio) return;

        // Safety check - if audio finished and destroyed itself, reset and exit
        if (currentDialogueAudioSource == null)
        {
            Debug.Log("No dialogue audio to pause - audio may have finished");
            isPausedDialogueAudio = false; // Reset pause state
            return;
        }
        
        // If currently fading, just stop all coroutines and handle the pause immediately
        if (isFadingDialogueAudio)
        {
            StopAllCoroutines(); // Nuclear option - stops all fades
            isFadingDialogueAudio = false;
        }
        
        dialogueFadeDuration = fadeDuration;
        dialogueFadeTarget = fadeTarget;

        if (isPausedDialogueAudio)
        {
            StartCoroutine(FadeInDialogue());
        }
        else
        {
            StartCoroutine(FadeOutAndPauseDialogue());
        }

        isPausedDialogueAudio = !isPausedDialogueAudio;
    }

    private IEnumerator FadeOutAndPauseDialogue()
    {
        // Safety check at start
        if (currentDialogueAudioSource == null)
        {
            isFadingDialogueAudio = false;
            isPausedDialogueAudio = false;
            yield break;
        }

        isFadingDialogueAudio = true;
        float startVolume = currentDialogueAudioSource.volume;
        float startPitch = currentDialogueAudioSource.pitch;

        if (dialogueFadeTarget == FadeTarget.Ignore)
        {
            currentDialogueAudioSource.volume = 0;
            currentDialogueAudioSource.pitch = 0;
        }
        else
        {
            for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
            {
                // ADD NULL CHECK HERE - audio can be destroyed mid-fade
                if (currentDialogueAudioSource == null)
                {
                    isFadingDialogueAudio = false;
                    isPausedDialogueAudio = false;
                    yield break;
                }
                
                float progress = t / dialogueFadeDuration;
            
                switch (dialogueFadeTarget)
                {
                    case FadeTarget.FadeVolume:
                        currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                        currentDialogueAudioSource.pitch = startPitch;
                        break;
                    
                    case FadeTarget.FadePitch:
                        currentDialogueAudioSource.volume = startVolume;
                        currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                        break;
                    
                    case FadeTarget.FadeBoth:
                        currentDialogueAudioSource.volume = Mathf.Lerp(startVolume, 0, progress);
                        currentDialogueAudioSource.pitch = Mathf.Lerp(startPitch, 0, progress);
                        break;
                }
            
                yield return null;
            }
        }

        // Final null check before pause
        if (currentDialogueAudioSource != null)
        {
            currentDialogueAudioSource.Pause();
            currentDialogueAudioSource.GetComponent<AutoDestroyAudioSource>()?.SetPausedStatus(true);
        }
        else
        {
            isPausedDialogueAudio = false; // Reset pause state if audio was destroyed
        }
        
        isFadingDialogueAudio = false;
    }

    private IEnumerator FadeInDialogue()
    {
        // Safety check at start
        if (currentDialogueAudioSource == null)
        {
            isFadingDialogueAudio = false;
            isPausedDialogueAudio = false;
            yield break;
        }

        isFadingDialogueAudio = true;
        currentDialogueAudioSource.UnPause();
        currentDialogueAudioSource.GetComponent<AutoDestroyAudioSource>()?.SetPausedStatus(false);

        float targetVolume = dialogueTargetVolume;
        float targetPitch = dialogueTargetPitch;

        if (dialogueFadeTarget == FadeTarget.Ignore)
        {
            currentDialogueAudioSource.volume = targetVolume;
            currentDialogueAudioSource.pitch = targetPitch;
        }
        else
        {
            for (float t = 0; t < dialogueFadeDuration; t += Time.deltaTime)
            {
                // ADD NULL CHECK HERE - audio can be destroyed mid-fade
                if (currentDialogueAudioSource == null)
                {
                    isFadingDialogueAudio = false;
                    isPausedDialogueAudio = false;
                    yield break;
                }
                
                float progress = t / dialogueFadeDuration;
            
                switch (dialogueFadeTarget)
                {
                    case FadeTarget.FadeVolume:
                        currentDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                        currentDialogueAudioSource.pitch = targetPitch;
                        break;
                    
                    case FadeTarget.FadePitch:
                        currentDialogueAudioSource.volume = targetVolume;
                        currentDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                        break;
                    
                    case FadeTarget.FadeBoth:
                        currentDialogueAudioSource.volume = Mathf.Lerp(0, targetVolume, progress);
                        currentDialogueAudioSource.pitch = Mathf.Lerp(0, targetPitch, progress);
                        break;
                }
            
                yield return null;
            }
        }

        // Final null check before setting final values
        if (currentDialogueAudioSource != null)
        {
            currentDialogueAudioSource.volume = targetVolume;
            currentDialogueAudioSource.pitch = targetPitch;
        }
        
        isFadingDialogueAudio = false;
    }

    #endregion
    // --------------------------------------------------------------------------------------------
    
    // SFX Management ********************
    // --------------------------------------------------------------------------------------------
    #region Play Sound Effects ------------------------------------
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
