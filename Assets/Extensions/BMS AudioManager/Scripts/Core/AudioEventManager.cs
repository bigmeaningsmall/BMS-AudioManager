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
    public delegate void AudioEvent_PlayAudio_Track(AudioTrackType trackType, int trackIndex, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName);
    public delegate void AudioEvent_StopAudio_Track(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay, string eventName);
    public delegate void AudioEvent_PauseAudio_Track(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget, float delay, string eventName);
    public delegate void AudioEvent_AdjustAudio_Track(AudioTrackType trackType, float volume, float pitch, float spatialBlend, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName);

    
    // Define a delegate for audio events - SFX // todo add delay
    //public delegate void AudioEvent_PlaySFX(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange,  float spatialBlend, string eventName);
    
    public delegate void AudioEvent_PlaySFX(string[] soundName, float volume, float pitch, bool randomisePitch, float pitchRange, float spatialBlend, bool loop, float delay, float percentChanceToPlay, Transform attachTo, Vector3 position, float minDist, float maxDist, string eventName);
    
    
    // ---------------------------------------------------------------------------------
    
    
    // --- Events --- Generic Audio Tracks 
    // playing ambient music
    public static AudioEvent_PlayAudio_Track playTrack;
    // stopping ambient music
    public static AudioEvent_StopAudio_Track stopTrack;
    // pausing ambient music
    public static AudioEvent_PauseAudio_Track pauseTrack;
    // updating ambient music
    public static AudioEvent_AdjustAudio_Track adjustTrack;
    
    
    // --- Events --- SFX - OneShots
    // Multi-delegate for playing sound effects
    public static AudioEvent_PlaySFX PlaySFX;
    
    
}