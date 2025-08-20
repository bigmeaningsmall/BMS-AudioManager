using UnityEngine;

/// <summary>
/// Static helper class that provides easy-to-use overloads for AudioManager events
/// Place this in a separate script file: AudioManagerHelper.cs
/// </summary>
public static class AudioEvent
{
    // ==================== PLAY TRACK OVERLOADS ====================
    
    /// <summary>
    /// Play track with just type and name - simplest usage
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, string trackName)
    {
        AudioEventManager.PlayTrack(trackType, -1, trackName, 1f, 1f, 0f, FadeType.FadeInOut, 0.5f, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Play track with type, name, and volume
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, string trackName, float volume)
    {
        AudioEventManager.PlayTrack(trackType, -1, trackName, volume, 1f, 0f, FadeType.FadeInOut, 0.5f, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Play track with type, name, volume, and fade duration
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, string trackName, float volume, float fadeDuration)
    {
        AudioEventManager.PlayTrack(trackType, -1, trackName, volume, 1f, 0f, FadeType.FadeInOut, fadeDuration, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Play track with type, name, volume, fade duration, and transform
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, string trackName, float volume, float fadeDuration, Transform attachTo)
    {
        float spatialBlend = attachTo != null ? 1f : 0f; // Auto-set spatial blend if transform provided
        AudioEventManager.PlayTrack(trackType, -1, trackName, volume, 1f, spatialBlend, FadeType.FadeInOut, fadeDuration, FadeTarget.FadeBoth, true, 0f, attachTo, "");
    }
    
    /// <summary>
    /// Play track with type, index (instead of name)
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, int trackIndex)
    {
        AudioEventManager.PlayTrack(trackType, trackIndex, "", 1f, 1f, 0f, FadeType.FadeInOut, 0.5f, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Play track with common parameters
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, string trackName, float volume, float pitch, float fadeDuration, FadeType fadeType)
    {
        AudioEventManager.PlayTrack(trackType, -1, trackName, volume, pitch, 0f, fadeType, fadeDuration, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Play track - full control version (all common parameters)
    /// </summary>
    public static void PlayTrackFull(AudioTrackType trackType, string trackName, float volume, float pitch, float spatialBlend, 
        FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName)
    {
        AudioEventManager.PlayTrack(trackType, -1, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, fadeTarget, loop, delay, attachTo, eventName);
    }
    
    // ==================== STOP TRACK OVERLOADS ====================
    
    /// <summary>
    /// Stop track immediately
    /// </summary>
    public static void StopTrack(AudioTrackType trackType)
    {
        AudioEventManager.StopTrack(trackType, 0f, FadeTarget.FadeVolume, 0f, "");
    }
    
    /// <summary>
    /// Stop track with fade
    /// </summary>
    public static void StopTrack(AudioTrackType trackType, float fadeDuration)
    {
        AudioEventManager.StopTrack(trackType, fadeDuration, FadeTarget.FadeVolume, 0f, "");
    }
    
    /// <summary>
    /// Stop track with fade and target
    /// </summary>
    public static void StopTrack(AudioTrackType trackType, float fadeDuration, FadeTarget fadeTarget)
    {
        AudioEventManager.StopTrack(trackType, fadeDuration, fadeTarget, 0f, "");
    }
    
    // ==================== PAUSE TRACK OVERLOADS ====================
    
    /// <summary>
    /// Pause/unpause track immediately
    /// </summary>
    public static void PauseTrack(AudioTrackType trackType)
    {
        AudioEventManager.PauseTrack(trackType, 0f, FadeTarget.FadeVolume, 0f, "");
    }
    
    /// <summary>
    /// Pause/unpause track with fade
    /// </summary>
    public static void PauseTrack(AudioTrackType trackType, float fadeDuration)
    {
        AudioEventManager.PauseTrack(trackType, fadeDuration, FadeTarget.FadeVolume, 0f, "");
    }
    
    // ==================== ADJUST TRACK OVERLOADS ====================
    
    /// <summary>
    /// Adjust track volume only
    /// </summary>
    public static void AdjustTrackVolume(AudioTrackType trackType, float volume)
    {
        AudioEventManager.AdjustTrack(trackType, volume, 1f, 0f, 0f, FadeTarget.FadeVolume, true, 0f, null, "");
    }
    
    /// <summary>
    /// Adjust track volume with fade
    /// </summary>
    public static void AdjustTrackVolume(AudioTrackType trackType, float volume, float fadeDuration)
    {
        AudioEventManager.AdjustTrack(trackType, volume, 1f, 0f, fadeDuration, FadeTarget.FadeVolume, true, 0f, null, "");
    }
    
    /// <summary>
    /// Adjust track volume and pitch
    /// </summary>
    public static void AdjustTrack(AudioTrackType trackType, float volume, float pitch)
    {
        AudioEventManager.AdjustTrack(trackType, volume, pitch, 0f, 0f, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    /// <summary>
    /// Adjust track with fade
    /// </summary>
    public static void AdjustTrack(AudioTrackType trackType, float volume, float pitch, float fadeDuration)
    {
        AudioEventManager.AdjustTrack(trackType, volume, pitch, 0f, fadeDuration, FadeTarget.FadeBoth, true, 0f, null, "");
    }
    
    // ==================== SFX OVERLOADS ====================
    
    /// <summary>
    /// Play SFX - simplest version
    /// </summary>
    public static void PlaySFX(string soundName)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, 1f, 1f, false, 0.1f, 0f, false, 0f, 100f, null, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX with volume
    /// </summary>
    public static void PlaySFX(string soundName, float volume)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, false, 0.1f, 0f, false, 0f, 100f, null, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX with multiple sound options (random selection)
    /// </summary>
    public static void PlaySFX(string[] soundNames)
    {
        AudioEventManager.PlaySFX(soundNames, 1f, 1f, false, 0.1f, 0f, false, 0f, 100f, null, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX with volume and random pitch
    /// </summary>
    public static void PlaySFX(string soundName, float volume, bool randomizePitch)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, randomizePitch, 0.1f, 0f, false, 0f, 100f, null, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX attached to transform (3D audio)
    /// </summary>
    public static void PlaySFX(string soundName, float volume, Transform attachTo)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, false, 0.1f, 1f, false, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX at world position (3D audio)
    /// </summary>
    public static void PlaySFX(string soundName, float volume, Vector3 position)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, false, 0.1f, 1f, false, 0f, 100f, null, position, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX with common 3D parameters
    /// </summary>
    public static void PlaySFX3D(string soundName, float volume, Transform attachTo, float minDist, float maxDist)
    {
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, false, 0.1f, 1f, false, 0f, 100f, attachTo, Vector3.zero, minDist, maxDist, "");
    }
    
    /// <summary>
    /// Play looped SFX
    /// </summary>
    public static void PlayLoopedSFX(string soundName, float volume, Transform attachTo = null)
    {
        float spatialBlend = attachTo != null ? 1f : 0f;
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, false, 0.1f, spatialBlend, true, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "");
    }
    
    /// <summary>
    /// Play SFX with chance and delay
    /// </summary>
    public static void PlayRandomSFX(string[] soundNames, float volume, float percentChance, float delay = 0f)
    {
        AudioEventManager.PlaySFX(soundNames, volume, 1f, true, 0.2f, 0f, false, delay, percentChance, null, Vector3.zero, 1f, 500f, "");
    }
}