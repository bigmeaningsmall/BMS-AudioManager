using UnityEngine;

/// <summary>
/// Static helper class that provides easy-to-use overloads for AudioManager events
/// Place this in a separate script file: AudioManagerHelper.cs
/// </summary>
public static class AudioEvent
{
    // Clip playback is SoundDefinition / SoundId only. The legacy string- and index-based PlayTrack
    // overloads were removed when the Resources lookup was retired (bank-only workflow).

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
    
    // SFX playback is SoundDefinition / SoundId only. The legacy string-based PlaySFX / PlaySFX3D /
    // PlayLoopedSFX / PlayRandomSFX overloads were removed with the Resources lookup (bank-only workflow).

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

    // ==================== SoundId (typed, string-free) OVERLOADS ====================
    // These resolve a SoundId to its SoundDefinition via the runtime registry, then reuse the
    // SoundDefinition overloads above. A SoundId only resolves if its bank is currently loaded.

    /// <summary>
    /// Resolves a SoundId to its loaded SoundDefinition. Returns null (with a warning) if the
    /// id is None, the AudioManager isn't ready, or the sound's bank isn't currently loaded.
    /// </summary>
    private static SoundDefinition Resolve(SoundId id, string caller)
    {
        if (id == SoundId.None)
        {
            AudioDebug.LogWarning($"[AudioEvent] {caller} called with SoundId.None.");
            return null;
        }
        if (AudioManager.Instance == null)
        {
            AudioDebug.LogWarning($"[AudioEvent] {caller}({id}) - no AudioManager in scene yet.");
            return null;
        }

        SoundDefinition def = AudioManager.Instance.Registry.Get((int)id);
        if (def == null)
            AudioDebug.LogWarning($"[AudioEvent] {caller}({id}) - not loaded. Is its bank loaded (startupBanks / SceneAudioBank)?");
        return def;
    }

    // ---- Smart dispatch: routes to track or SFX based on the definition's audioType ----

    /// <summary>Play a sound by id - automatically routed to a track or SFX by its category.</summary>
    public static void Play(SoundId id)
    {
        SoundDefinition def = Resolve(id, "Play");
        if (def == null) return;
        if (def.audioType == AudioType.SFX) PlaySFX(def);
        else PlayTrack(def);
    }

    /// <summary>Play a sound by id attached to a transform - routed to a track or SFX by its category.</summary>
    public static void Play(SoundId id, Transform attachTo)
    {
        SoundDefinition def = Resolve(id, "Play");
        if (def == null) return;
        if (def.audioType == AudioType.SFX) PlaySFX(def, attachTo);
        else PlayTrack(def, attachTo);
    }

    // ---- Explicit track ----

    public static void PlayTrack(SoundId id)
    {
        SoundDefinition def = Resolve(id, "PlayTrack");
        if (def != null) PlayTrack(def);
    }

    public static void PlayTrack(SoundId id, float volume)
    {
        SoundDefinition def = Resolve(id, "PlayTrack");
        if (def != null) PlayTrack(def, volume);
    }

    public static void PlayTrack(SoundId id, Transform attachTo)
    {
        SoundDefinition def = Resolve(id, "PlayTrack");
        if (def != null) PlayTrack(def, attachTo);
    }

    /// <summary>Stop the channel this sound plays on, using the definition's own fade settings.</summary>
    public static void StopTrack(SoundId id)
    {
        SoundDefinition def = Resolve(id, "StopTrack");
        if (def != null) StopTrack(def);
    }

    // ---- Explicit SFX ----

    public static void PlaySFX(SoundId id)
    {
        SoundDefinition def = Resolve(id, "PlaySFX");
        if (def != null) PlaySFX(def);
    }

    public static void PlaySFX(SoundId id, float volume)
    {
        SoundDefinition def = Resolve(id, "PlaySFX");
        if (def != null) PlaySFX(def, volume);
    }

    public static void PlaySFX(SoundId id, Transform attachTo)
    {
        SoundDefinition def = Resolve(id, "PlaySFX");
        if (def != null) PlaySFX(def, attachTo);
    }

    public static void PlaySFX(SoundId id, Vector3 position)
    {
        SoundDefinition def = Resolve(id, "PlaySFX");
        if (def != null) PlaySFX(def, position);
    }

    public static void PlaySFX3D(SoundId id, Transform attachTo, float minDist, float maxDist)
    {
        SoundDefinition def = Resolve(id, "PlaySFX3D");
        if (def != null) PlaySFX3D(def, attachTo, minDist, maxDist);
    }

    public static void PlayLoopedSFX(SoundId id, Transform attachTo = null)
    {
        SoundDefinition def = Resolve(id, "PlayLoopedSFX");
        if (def != null) PlayLoopedSFX(def, attachTo);
    }
}