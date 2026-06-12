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
    [Header("Clip")]
    [Tooltip("Primary audio clip for this sound.")]
    public AudioClip clip;

    [Tooltip("Optional extra clips. When present, GetRandomClip() picks randomly across clip + variations (used for SFX).")]
    public AudioClip[] variations;

    [Tooltip("Category this sound belongs to. Set automatically by the generator from the source folder.")]
    public AudioType audioType = AudioType.BGM;

    [Header("Default Playback Parameters")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 2f)] public float pitch = 1f;
    public bool loop = true;
    [Range(0f, 1f)] public float spatialBlend = 0f;

    [Header("Default Fade")]
    public FadeType fadeType = FadeType.FadeInOut;
    [Range(0f, 10f)] public float fadeDuration = 0.5f;
    public FadeTarget fadeTarget = FadeTarget.FadeBoth;

    /// <summary>
    /// Returns the primary clip. The ONLY way callers should obtain the clip - keep it this
    /// way so the retrieval strategy (direct ref now, Addressables later) stays swappable here.
    /// </summary>
    public AudioClip GetClip() => clip;

    /// <summary>
    /// Returns a random clip from the pool of (clip + variations). Falls back to the primary
    /// clip when no variations are set. Used by the SFX sender for one-shot variety.
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
