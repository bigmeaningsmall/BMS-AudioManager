using UnityEngine;

/// <summary>
/// Usage examples for the bank-only workflow.
///
/// Audio is addressed two ways now - both string-free:
///   - SoundId        : a generated typed key (dropdown in the inspector). Resolved from the
///                       registry, so the sound's bank must be loaded (startupBanks / SceneAudioBank).
///   - SoundDefinition : a direct asset reference (drag-drop) that carries its own default params.
///
/// Track ops that act on a CHANNEL (Stop/Pause/Adjust) still take an AudioTrackType - they operate
/// on whatever is currently playing on that channel, so they need no clip identity.
///
/// Assign the SoundId/SoundDefinition fields in the inspector, then call the menu items
/// (right-click the component) or press the keys in Update.
/// </summary>
public class AudioManagerUsageExample : MonoBehaviour
{
    [Header("Scene references")]
    public Transform playerTransform;
    public Transform ambientLocationTransform;

    [Header("Tracks (pick from the generated SoundId list)")]
    public SoundId musicTrack;
    public SoundId combatTrack;
    public SoundId ambientTrack;

    [Header("SFX")]
    public SoundId buttonClickSfx;
    public SoundId explosionSfx;
    public SoundId engineLoopSfx;

    [Header("SoundDefinition example (direct asset reference)")]
    [Tooltip("Demonstrates the asset-reference API alongside SoundId.")]
    public SoundDefinition footstepDefinition;

    private void Update()
    {
        // Smart dispatch: Play() routes to a track or a one-shot SFX based on the definition's type.
        if (Input.GetKeyDown(KeyCode.Space))
            AudioEvent.Play(buttonClickSfx);
    }

    // ==================== TRACKS ====================
    [ContextMenu("Tracks: Play music")]
    private void PlayMusic()
    {
        AudioEvent.PlayTrack(musicTrack);            // uses the definition's own defaults
        AudioEvent.PlayTrack(musicTrack, 0.8f);      // volume override, rest from the definition
    }

    [ContextMenu("Tracks: Play ambient (3D at location)")]
    private void PlayAmbient()
    {
        AudioEvent.PlayTrack(ambientTrack, ambientLocationTransform); // attached -> spatialised
    }

    [ContextMenu("Tracks: Stop / Pause / Adjust (channel-based)")]
    private void ChannelOps()
    {
        AudioEvent.StopTrack(AudioTrackType.BGM, 2f);           // fade out the BGM channel
        AudioEvent.PauseTrack(AudioTrackType.Ambient, 1f);      // toggle-pause the Ambient channel
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 1f); // duck BGM volume over 1s
    }

    // ==================== SFX ====================
    [ContextMenu("SFX: One-shots")]
    private void PlaySfx()
    {
        AudioEvent.PlaySFX(explosionSfx);                       // 2D, definition defaults
        AudioEvent.PlaySFX(explosionSfx, 0.8f);                 // volume override
        AudioEvent.PlaySFX(explosionSfx, playerTransform);      // 3D attached
        AudioEvent.PlaySFX(explosionSfx, new Vector3(10, 0, 5)); // 3D at world position
    }

    [ContextMenu("SFX: Looped + SoundDefinition ref")]
    private void PlaySfxAdvanced()
    {
        AudioEvent.PlayLoopedSFX(engineLoopSfx, playerTransform);   // looped engine
        AudioEvent.PlaySFX(footstepDefinition, playerTransform);    // via direct asset reference
    }

    // ==================== SCENARIO ====================
    [ContextMenu("Scenario: Combat transition")]
    private void CombatTransition()
    {
        StartCoroutine(CombatSequence());
    }

    private System.Collections.IEnumerator CombatSequence()
    {
        AudioEvent.PlaySFX(buttonClickSfx, 0.8f);    // alert
        AudioEvent.PlayTrack(ambientTrack, 0.5f);    // tension bed

        yield return new WaitForSeconds(2f);

        // Switch to combat music. The transition style (crossfade vs fade-in-out) and timing come
        // from the combat definition's own fade settings - set those on the SoundDefinition asset.
        AudioEvent.PlayTrack(combatTrack);
    }

    // ==================== GAME WRAPPERS ====================
    public void DuckMusicForDialogue(float duckLevel = 0.2f) => AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, duckLevel, 0.5f);
    public void RestoreMusicAfterDialogue(float normalLevel = 0.8f) => AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, normalLevel, 1f);
}
