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
    
    // ==================== PLAY TRACK (SoundDefinition) OVERLOADS ====================
    // Asset-safe alternative to the string overloads above. The definition supplies the clip,
    // the channel (def.TrackType) and all default params; override args replace individual values.

    /// <summary>
    /// Play a SoundDefinition on its own channel using all of its default parameters.
    /// </summary>
    public static void PlayTrack(SoundDefinition def)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrack called with null SoundDefinition."); return; }
        AudioEventManager.PlayTrack(def.TrackType, -1, "", def.volume, def.pitch, def.spatialBlend,
            def.fadeType, def.fadeDuration, def.fadeTarget, def.loop, 0f, null, "", def.GetClip());
    }

    /// <summary>
    /// Play a SoundDefinition with a volume override (other params from the definition).
    /// </summary>
    public static void PlayTrack(SoundDefinition def, float volume)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrack called with null SoundDefinition."); return; }
        AudioEventManager.PlayTrack(def.TrackType, -1, "", volume, def.pitch, def.spatialBlend,
            def.fadeType, def.fadeDuration, def.fadeTarget, def.loop, 0f, null, "", def.GetClip());
    }

    /// <summary>
    /// Play a SoundDefinition with volume and fade-duration overrides.
    /// </summary>
    public static void PlayTrack(SoundDefinition def, float volume, float fadeDuration)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrack called with null SoundDefinition."); return; }
        AudioEventManager.PlayTrack(def.TrackType, -1, "", volume, def.pitch, def.spatialBlend,
            def.fadeType, fadeDuration, def.fadeTarget, def.loop, 0f, null, "", def.GetClip());
    }

    /// <summary>
    /// Play a SoundDefinition attached to a transform (auto-spatialised when attachTo is set).
    /// </summary>
    public static void PlayTrack(SoundDefinition def, Transform attachTo)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrack called with null SoundDefinition."); return; }
        float spatialBlend = attachTo != null ? 1f : def.spatialBlend;
        AudioEventManager.PlayTrack(def.TrackType, -1, "", def.volume, def.pitch, spatialBlend,
            def.fadeType, def.fadeDuration, def.fadeTarget, def.loop, 0f, attachTo, "", def.GetClip());
    }

    /// <summary>
    /// Play a SoundDefinition on an explicit channel (override the definition's own type).
    /// </summary>
    public static void PlayTrack(AudioTrackType trackType, SoundDefinition def)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrack called with null SoundDefinition."); return; }
        AudioEventManager.PlayTrack(trackType, -1, "", def.volume, def.pitch, def.spatialBlend,
            def.fadeType, def.fadeDuration, def.fadeTarget, def.loop, 0f, null, "", def.GetClip());
    }

    /// <summary>
    /// Play a SoundDefinition - full control passthrough (every param explicit).
    /// </summary>
    public static void PlayTrackFull(SoundDefinition def, float volume, float pitch, float spatialBlend,
        FadeType fadeType, float fadeDuration, FadeTarget fadeTarget, bool loop, float delay, Transform attachTo, string eventName)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayTrackFull called with null SoundDefinition."); return; }
        AudioEventManager.PlayTrack(def.TrackType, -1, "", volume, pitch, spatialBlend,
            fadeType, fadeDuration, fadeTarget, loop, delay, attachTo, eventName, def.GetClip());
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

    /// <summary>
    /// Stop the channel a SoundDefinition plays on, using the definition's own fade duration and
    /// fade target - so stop is symmetric with PlayTrack(def). Stop is still channel-based: this
    /// stops whatever is currently on def.TrackType, not a specific clip.
    /// </summary>
    public static void StopTrack(SoundDefinition def)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] StopTrack called with null SoundDefinition."); return; }
        AudioEventManager.StopTrack(def.TrackType, def.fadeDuration, def.fadeTarget, 0f, "");
    }

    /// <summary>
    /// Stop the channel a SoundDefinition plays on with a duration override (fade target from the definition).
    /// </summary>
    public static void StopTrack(SoundDefinition def, float fadeDuration)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] StopTrack called with null SoundDefinition."); return; }
        AudioEventManager.StopTrack(def.TrackType, fadeDuration, def.fadeTarget, 0f, "");
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

    /// <summary> i think this works // todo test in a project as its going to be the common call...
    /// Play SFX with explicit pitch range (x = min, y = max) attached to transform (3D audio).
    /// e.g. new Vector2(0.9f, 1.2f) randomises pitch between 0.9 and 1.2 on each play.
    /// </summary>
    public static void PlaySFX(string soundName, float volume, Vector2 pitchRange, Transform attachTo)
    {
        float pitchCentre = (pitchRange.x + pitchRange.y) / 2f;
        float pitchVariance = (pitchRange.y - pitchRange.x) / 2f;
        float spatialBlend = attachTo != null ? 1f : 0f;
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, pitchCentre, true, pitchVariance, spatialBlend, false, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "");
    }

    /// <summary>
    /// Play SFX with pitch randomisation toggle attached to transform (3D audio).
    /// Uses default pitch variance of +-0.1.
    /// </summary>
    public static void PlaySFX(string soundName, float volume, bool randomizePitch, Transform attachTo)
    {
        float spatialBlend = attachTo != null ? 1f : 0f;
        AudioEventManager.PlaySFX(new string[] { soundName }, volume, 1f, randomizePitch, 0.1f, spatialBlend, false, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "");
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

    // ==================== SFX (SoundDefinition) OVERLOADS ====================
    // The definition's clip + variations form the random pool (def.GetClipPool());
    // defaults come from the definition, override args replace individual values.

    /// <summary>
    /// Play a SoundDefinition as a one-shot SFX using its default parameters.
    /// </summary>
    public static void PlaySFX(SoundDefinition def)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlaySFX called with null SoundDefinition."); return; }
        AudioEventManager.PlaySFX(null, def.volume, def.pitch, false, 0.1f, def.spatialBlend, def.loop, 0f, 100f, null, Vector3.zero, 1f, 500f, "", def.GetClipPool());
    }

    /// <summary>
    /// Play a SoundDefinition SFX with a volume override.
    /// </summary>
    public static void PlaySFX(SoundDefinition def, float volume)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlaySFX called with null SoundDefinition."); return; }
        AudioEventManager.PlaySFX(null, volume, def.pitch, false, 0.1f, def.spatialBlend, def.loop, 0f, 100f, null, Vector3.zero, 1f, 500f, "", def.GetClipPool());
    }

    /// <summary>
    /// Play a SoundDefinition SFX attached to a transform (3D, auto-spatialised).
    /// </summary>
    public static void PlaySFX(SoundDefinition def, Transform attachTo)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlaySFX called with null SoundDefinition."); return; }
        float spatialBlend = attachTo != null ? 1f : def.spatialBlend;
        AudioEventManager.PlaySFX(null, def.volume, def.pitch, false, 0.1f, spatialBlend, def.loop, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "", def.GetClipPool());
    }

    /// <summary>
    /// Play a SoundDefinition SFX at a world position (3D).
    /// </summary>
    public static void PlaySFX(SoundDefinition def, Vector3 position)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlaySFX called with null SoundDefinition."); return; }
        AudioEventManager.PlaySFX(null, def.volume, def.pitch, false, 0.1f, 1f, def.loop, 0f, 100f, null, position, 1f, 500f, "", def.GetClipPool());
    }

    /// <summary>
    /// Play a SoundDefinition SFX with explicit 3D distance settings.
    /// </summary>
    public static void PlaySFX3D(SoundDefinition def, Transform attachTo, float minDist, float maxDist)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlaySFX3D called with null SoundDefinition."); return; }
        AudioEventManager.PlaySFX(null, def.volume, def.pitch, false, 0.1f, 1f, def.loop, 0f, 100f, attachTo, Vector3.zero, minDist, maxDist, "", def.GetClipPool());
    }

    /// <summary>
    /// Play a looped SoundDefinition SFX (e.g. an engine or fire loop).
    /// </summary>
    public static void PlayLoopedSFX(SoundDefinition def, Transform attachTo = null)
    {
        if (def == null) { AudioDebug.LogWarning("[AudioEvent] PlayLoopedSFX called with null SoundDefinition."); return; }
        float spatialBlend = attachTo != null ? 1f : def.spatialBlend;
        AudioEventManager.PlaySFX(null, def.volume, def.pitch, false, 0.1f, spatialBlend, true, 0f, 100f, attachTo, Vector3.zero, 1f, 500f, "", def.GetClipPool());
    }
}