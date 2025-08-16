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
    
    //define a delegate for audio events - type of track is defined ion the parameters
    public delegate void AudioEvent_PlayAudio_Track(AudioTrackType trackType, Transform attachTo, int trackIndex, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, string eventName);
    public delegate void AudioEvent_StopAudio_Track(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay, string eventName);
    public delegate void AudioEvent_PauseAudio_Track(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay, string eventName);
    public delegate void AudioEvent_UpdateAudio_Track(AudioTrackType trackType, Transform attachTo, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, string eventName);

    
    // Define a delegate for audio events - SFX // todo add delay
    public delegate void AudioEvent_PlaySFX(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange,  float spatialBlend, string eventName);
    
    
    // ---------------------------------------------------------------------------------
    
    
    // --- Events --- Generic Audio Tracks 
    // playing ambient music
    public static AudioEvent_PlayAudio_Track playTrack;
    // stopping ambient music
    public static AudioEvent_StopAudio_Track stopTrack;
    // pausing ambient music
    public static AudioEvent_PauseAudio_Track pauseTrack;
    // updating ambient music
    public static AudioEvent_UpdateAudio_Track updateTrack;
    
    
    // --- Events --- SFX - OneShots
    // Multi-delegate for playing sound effects
    public static AudioEvent_PlaySFX PlaySFX;
    
    
}