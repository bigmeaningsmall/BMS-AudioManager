using System;
using UnityEngine;

//define enums for fade types
public enum FadeType
{
    FadeInOut,
    Crossfade
}
public enum CollisionType
{
    Null,
    Collision,
    Trigger
}
public enum FadeTarget
{
    Ignore,         // Don't fade anything (instant change)
    FadeVolume,     // Fade volume only, pitch changes instantly
    FadePitch,      // Fade pitch only, volume changes instantly  
    FadeBoth        // Fade both volume and pitch
}

// A simple class to define delegates for audio-related events
public static class AudioEventManager
{
    
    //define a delegate for audio events - BGM
    public delegate void AudioEvent_PlayBGM_Track(int index, string trackName, float volume, FadeType fadeType, float fadeDuration, bool loopBGM, string eventName);
    public delegate void AudioEvent_StopBGM_Track(float fadeDuration);
    public delegate void AudioEvent_PauseBGM_Track(float fadeDuration);
    
    //define a delegate for audio events - Ambient Music
    public delegate void AudioEvent_PlayAmbientAudio_Track(Transform attachTo, int index, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loopAmbient, string eventName);
    public delegate void AudioEvent_StopAmbientAudio_Track(float fadeDuration, FadeTarget fadeTarget);
    public delegate void AudioEvent_PauseAmbientAudio_Track(float fadeDuration, FadeTarget fadeTarget);
    public delegate void AudioEvent_UpdateAmbientAudio_Track(Transform attachTo, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loopAmbient, string eventName);
    
    
    // Define a delegate for audio events - Dialogue
    public delegate void AudioEvent_PlayDialogue_Track(Transform attachTo, int index, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, string eventName);
    public delegate void AudioEvent_StopDialogue_Track(float fadeDuration, FadeTarget fadeTarget);
    public delegate void AudioEvent_PauseDialogue_Track(float fadeDuration, FadeTarget fadeTarget);
    
    
    // Define a delegate for audio events - SFX // todo add delay
    public delegate void AudioEvent_PlaySFX(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange,  float spatialBlend, string eventName);
    
    
    // --- Events --- BGM - Single Track
    // playing background music
    public static AudioEvent_PlayBGM_Track playBGMTrack;
    // stopping background music
    public static AudioEvent_StopBGM_Track stopBGMTrack;
    // pausing background music
    public static AudioEvent_PauseBGM_Track pauseBGMTrack;
    
    
    // --- Events --- Ambient Music - Single Track
    // playing ambient music
    public static AudioEvent_PlayAmbientAudio_Track playAmbientTrack;
    // stopping ambient music
    public static AudioEvent_StopAmbientAudio_Track stopAmbientTrack;
    // pausing ambient music
    public static AudioEvent_PauseAmbientAudio_Track pauseAmbientTrack;
    // updating ambient music
    public static AudioEvent_UpdateAmbientAudio_Track updateAmbientTrack;
    
    // --- Events --- Dialogue - Single Track
    // playing dialogue
    public static AudioEvent_PlayDialogue_Track playDialogueTrack;
    // stopping dialogue
    public static AudioEvent_StopDialogue_Track stopDialogueTrack;
    // pausing dialogue
    public static AudioEvent_PauseDialogue_Track pauseDialogueTrack;
    
    // --- Events --- SFX - OneShots
    // Multi-delegate for playing sound effects
    public static AudioEvent_PlaySFX PlaySFX;
    
    
}