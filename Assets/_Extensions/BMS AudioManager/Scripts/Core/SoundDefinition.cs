using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// A reusable, asset-safe "sound asset" that holds an AudioClip plus its default
/// playback parameters. Senders reference a SoundDefinition directly (drag-drop) instead
/// of relying on a string/index lookup into Resources.
///
/// This is the single choke point for clip retrieval: today GetClip() returns a direct
/// reference, but it can later be swapped to an Addressables async load WITHOUT touching
/// AudioTrack, the senders, or the event delegates.
/// </summary>
[CreateAssetMenu(fileName = "SoundDef", menuName = "BMS AudioManager/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable unique id - assigned by the generator and used as the SoundId enum value. Do not edit. 0 = unassigned (run Generate Sound Definitions).")]
    public int id;

    [Header("Clip")]
    [Tooltip("Primary audio clip for this sound.")]
    public AudioClip clip;

    [Tooltip("Optional extra clips. When present, one is picked at random per play across clip + variations - for SFX one-shots AND track playback (e.g. a random intro/ambient bed). Empty = always the primary clip.")]
    public AudioClip[] variations;

    [Tooltip("Category this sound belongs to. Set automatically by the generator from the source folder.")]
    public AudioType audioType = AudioType.BGM;

    /// <summary>
    /// The track channel this definition routes to when played via AudioEvent.PlayTrack(def).
    /// AudioType includes SFX/Null which are not valid track channels - those fall back to Aux1
    /// with a warning (an SFX definition should be played via PlaySFX, not PlayTrack).
    /// </summary>
    public AudioTrackType TrackType
    {
        get
        {
            switch (audioType)
            {
                case AudioType.BGM:      return AudioTrackType.BGM;
                case AudioType.Ambient:  return AudioTrackType.Ambient;
                case AudioType.Dialogue: return AudioTrackType.Dialogue;
                case AudioType.Aux1:     return AudioTrackType.Aux1;
                case AudioType.Aux2:     return AudioTrackType.Aux2;
                default:
                    AudioDebug.LogWarning($"[SoundDefinition] '{name}' has audioType {audioType}, which is not a track channel. Falling back to Aux1. Use PlaySFX for SFX definitions.");
                    return AudioTrackType.Aux1;
            }
        }
    }

    [Header("Default Playback Parameters")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 2f)] public float pitch = 1f;
    public bool loop = true;
    [Range(0f, 1f)] public float spatialBlend = 0f;

    [Header("Default Fade")]
    public FadeType fadeType = FadeType.FadeInOut;
    [Range(0f, 10f)] public float fadeDuration = 0.5f;
    public FadeTarget fadeTarget = FadeTarget.FadeBoth;

    [Header("SFX Variation (one-shots only)")]
    [Tooltip("These are applied by PlaySFX one-shots. Track playback ignores them.")]
    public bool randomizePitch = false;
    [Tooltip("Pitch is randomised within +/- this range around 'pitch'.")]
    [Range(0f, 1f)] public float pitchRange = 0.1f;
    [Tooltip("Volume is randomised within +/- this range around 'volume' (0 = no jitter).")]
    [Range(0f, 1f)] public float volumeRange = 0f;
    [Tooltip("Chance (0-100) that a PlaySFX call actually plays. 100 = always.")]
    [Range(0f, 100f)] public float percentChanceToPlay = 100f;
    [Tooltip("Randomise the delay between 0 and 'delay' on each play.")]
    public bool randomizeDelay = false;
    [Tooltip("Delay before the SFX starts (seconds). With 'randomizeDelay', this is the maximum.")]
    [Range(0f, 5f)] public float delay = 0f;

    [Header("3D Settings (spatialised tracks + SFX)")]
    [Tooltip("Rolloff distances used whenever spatialBlend > 0 - applies to 3D-attached tracks AND SFX.")]
    [Range(0f, 100f)] public float minDistance = 1f;
    [Tooltip("3D rolloff far distance.")]
    [Range(1f, 500f)] public float maxDistance = 500f;

    /// <summary>Volume for this play - jittered within +/- volumeRange when set, else the base volume.</summary>
    public float NextSfxVolume()
        => volumeRange > 0f ? Mathf.Clamp01(Random.Range(volume - volumeRange, volume + volumeRange)) : volume;

    /// <summary>Delay for this play - randomised in [0, delay] when randomizeDelay is set, else the fixed delay.</summary>
    public float NextSfxDelay()
        => randomizeDelay ? Random.Range(0f, delay) : delay;

    /// <summary>
    /// Returns the primary clip. The ONLY way callers should obtain the clip - keep it this
    /// way so the retrieval strategy (direct ref now, Addressables later) stays swappable here.
    /// </summary>
    public AudioClip GetClip() => clip;

    /// <summary>
    /// Returns a random clip from the pool of (clip + variations). Falls back to the primary
    /// clip when no variations are set. Used for both SFX one-shot variety and track playback
    /// (picked once per Play - a looping track repeats the chosen clip, it doesn't reshuffle mid-loop).
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (variations == null || variations.Length == 0)
            return clip;

        // Build the pool lazily-ish: primary clip plus any non-null variations
        int validVariations = 0;
        for (int i = 0; i < variations.Length; i++)
            if (variations[i] != null) validVariations++;

        if (validVariations == 0) return clip;

        // index 0 represents the primary clip; 1..validVariations map to variations
        int poolSize = (clip != null ? 1 : 0) + validVariations;
        int pick = Random.Range(0, poolSize);

        if (clip != null)
        {
            if (pick == 0) return clip;
            pick -= 1;
        }

        for (int i = 0; i < variations.Length; i++)
        {
            if (variations[i] == null) continue;
            if (pick == 0) return variations[i];
            pick--;
        }

        return clip;
    }

    /// <summary>
    /// Returns the full clip pool (primary + non-null variations) for callers that want to do
    /// their own selection (e.g. the SFX manager's existing random-pick logic).
    /// </summary>
    public AudioClip[] GetClipPool()
    {
        var pool = new System.Collections.Generic.List<AudioClip>();
        if (clip != null) pool.Add(clip);
        if (variations != null)
        {
            foreach (var v in variations)
                if (v != null) pool.Add(v);
        }
        return pool.ToArray();
    }
}
