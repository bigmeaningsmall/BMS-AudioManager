using System.Collections;
using UnityEngine;

/// <summary>
/// LIVE API REFERENCE + TEST HARNESS for BMS Audio Manager.
///
/// This single component is both the copy-paste cheat sheet (every method below is one
/// self-contained snippet) and a click-to-test panel. Each example is callable two ways:
///   - an on-screen button in Play mode (see the panel, top-left of the Game view), and
///   - a right-click [ContextMenu] entry on this component.
///
/// SETUP
///   1. Run  BMS AudioManager -> Generate Sound Definitions  (builds SoundDefinitions, SoundBanks, SoundId).
///   2. Put an AudioManager in the scene and assign a bank that contains these sounds to its
///      Startup Banks (MasterBank = everything). A sound only plays when its bank is loaded.
///   3. Add this component anywhere, assign the SoundId / SoundDefinition fields below, press Play.
///
/// KEY IDEA
///   - Tracks (BGM/Ambient/Dialogue/Aux) are CHANNELS. Play puts a clip on a channel; Stop/Pause/
///     Adjust act on the channel (take an AudioTrackType), not a specific clip.
///   - SFX are fire-and-forget one-shots; the definition's clip + variations form the random pool.
///   - A SoundDefinition carries its own defaults (volume/pitch/fade/3D/variation), so most calls
///     are one-liners. Pass a SoundId (typed key) or a SoundDefinition reference - both work.
/// </summary>
public class AudioManagerUsageExample : MonoBehaviour
{
    [Header("Track SoundIds")]
    [Tooltip("A BGM/music sound.")]      public SoundId musicTrack;
    [Tooltip("An ambient/aux sound.")]   public SoundId ambientTrack;
    [Tooltip("A dialogue sound.")]       public SoundId dialogueLine;

    [Header("SFX SoundIds")]
    [Tooltip("A one-shot SFX.")]         public SoundId sfxOneShot;
    [Tooltip("A loopable SFX.")]         public SoundId loopingSfx;

    [Header("SoundDefinition references (alternative to SoundId)")]
    [Tooltip("Any track definition - shows the direct asset-reference API.")]
    public SoundDefinition trackDef;
    [Tooltip("An SFX definition (ideally with variations).")]
    public SoundDefinition sfxDef;

    [Header("Quick test")]
    [Tooltip("Pick any sound; the 'Play (smart)' button auto-routes it to a track or SFX.")]
    public SoundId quickTestId;

    [Header("Options")]
    [Tooltip("Transform that 3D examples attach to (defaults to this object).")]
    public Transform attachPoint;

    private Vector2 _scroll;

    private void Awake()
    {
        if (attachPoint == null) attachPoint = transform;
    }

    // ============================================================ TRACKS

    [ContextMenu("Track/Play (definition defaults)")]
    private void PlayMusic() => AudioEvent.PlayTrack(musicTrack);

    [ContextMenu("Track/Play at half volume")]
    private void PlayMusicQuiet() => AudioEvent.PlayTrack(musicTrack, 0.5f);

    [ContextMenu("Track/Play ambient 3D (attached)")]
    private void PlayAmbient3D() => AudioEvent.PlayTrack(ambientTrack, attachPoint);

    [ContextMenu("Track/Play via SoundDefinition reference")]
    private void PlayTrackByRef() => AudioEvent.PlayTrack(trackDef);

    [ContextMenu("Track/Play (smart dispatch by category)")]
    private void PlaySmart() => AudioEvent.Play(quickTestId); // routes to track or SFX automatically

    // ============================================================ TRACK CONTROL (channel-based)

    [ContextMenu("Control/Stop BGM (1.5s fade)")]
    private void StopBGM() => AudioEvent.StopTrack(AudioTrackType.BGM, 1.5f);

    [ContextMenu("Control/Stop the music track's channel (definition fade)")]
    private void StopMusicSymmetric() => AudioEvent.StopTrack(musicTrack); // uses the def's fade settings

    [ContextMenu("Control/Pause-Resume BGM (toggle)")]
    private void PauseBGM() => AudioEvent.PauseTrack(AudioTrackType.BGM, 0.5f);

    [ContextMenu("Control/Adjust BGM volume")]
    private void AdjustVolume() => AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 1f);

    [ContextMenu("Control/Adjust BGM volume + pitch")]
    private void AdjustVolumePitch() => AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.8f, 1.1f, 1f);

    [ContextMenu("Control/Duck for dialogue (pattern)")]
    private void DuckForDialogue() => StartCoroutine(DuckRoutine());

    private IEnumerator DuckRoutine()
    {
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.15f, 0.4f); // duck under
        AudioEvent.PlayTrack(dialogueLine);                           // play the line
        yield return new WaitForSeconds(2f);
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);   // restore
    }

    // ============================================================ SFX

    [ContextMenu("SFX/One-shot (2D)")]
    private void Sfx2D() => AudioEvent.PlaySFX(sfxOneShot);

    [ContextMenu("SFX/One-shot at half volume")]
    private void SfxQuiet() => AudioEvent.PlaySFX(sfxOneShot, 0.5f);

    [ContextMenu("SFX/One-shot 3D (attached)")]
    private void Sfx3D() => AudioEvent.PlaySFX(sfxOneShot, attachPoint);

    [ContextMenu("SFX/One-shot 3D (world position)")]
    private void SfxAtPosition() => AudioEvent.PlaySFX(sfxOneShot, attachPoint.position);

    [ContextMenu("SFX/One-shot 3D (explicit distances)")]
    private void Sfx3DCustom() => AudioEvent.PlaySFX3D(sfxOneShot, attachPoint, 2f, 25f);

    [ContextMenu("SFX/Looped (attached)")]
    private void SfxLooped() => AudioEvent.PlayLoopedSFX(loopingSfx, attachPoint);

    [ContextMenu("SFX/Via SoundDefinition reference (random variation)")]
    private void SfxByRef() => AudioEvent.PlaySFX(sfxDef, attachPoint);

    [ContextMenu("SFX/Stop all SFX")]
    private void StopAllSfx() { if (AudioManager.Instance != null) AudioManager.Instance.StopAllSFX(); }

    [ContextMenu("SFX/Stop looped SFX")]
    private void StopLoopedSfx() { if (AudioManager.Instance != null) AudioManager.Instance.StopAllLoopedSFX(); }

    // ============================================================ GLOBAL / QUERY

    [ContextMenu("Global/Toggle pause all SFX")]
    private void TogglePauseSfx() { if (AudioManager.Instance != null) AudioManager.Instance.TogglePauseAllSFX(); }

    [ContextMenu("Global/Halve SFX volume")]
    private void HalveSfxVolume() { if (AudioManager.Instance != null) AudioManager.Instance.GlobalSFXAttenuation = 0.5f; }

    [ContextMenu("Global/Log active SFX count")]
    private void LogActiveSfx()
    {
        if (AudioManager.Instance == null) return;
        Debug.Log($"[Example] Active SFX: {AudioManager.Instance.GetActiveSFXCount()} | All paused: {AudioManager.Instance.AllSFXPaused}");
    }

    // ============================================================ ON-SCREEN TEST PANEL

    private void OnGUI()
    {
        const float w = 260f;
        GUILayout.BeginArea(new Rect(10, 10, w, Screen.height - 20), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("<b>BMS Audio - API Examples</b>", RichLabel());

        Section("Tracks");
        if (GUILayout.Button("Play music (defaults)")) PlayMusic();
        if (GUILayout.Button("Play music @ 0.5 volume")) PlayMusicQuiet();
        if (GUILayout.Button("Play ambient 3D (attached)")) PlayAmbient3D();
        if (GUILayout.Button("Play via SoundDefinition ref")) PlayTrackByRef();
        if (GUILayout.Button("Play (smart: quickTestId)")) PlaySmart();

        Section("Track control (channel)");
        if (GUILayout.Button("Stop BGM (1.5s fade)")) StopBGM();
        if (GUILayout.Button("Stop music chan (def fade)")) StopMusicSymmetric();
        if (GUILayout.Button("Pause / Resume BGM")) PauseBGM();
        if (GUILayout.Button("Adjust BGM volume")) AdjustVolume();
        if (GUILayout.Button("Adjust BGM volume + pitch")) AdjustVolumePitch();
        if (GUILayout.Button("Duck for dialogue (pattern)")) DuckForDialogue();

        Section("SFX");
        if (GUILayout.Button("One-shot (2D)")) Sfx2D();
        if (GUILayout.Button("One-shot @ 0.5 volume")) SfxQuiet();
        if (GUILayout.Button("One-shot 3D (attached)")) Sfx3D();
        if (GUILayout.Button("One-shot 3D (world pos)")) SfxAtPosition();
        if (GUILayout.Button("One-shot 3D (custom dist)")) Sfx3DCustom();
        if (GUILayout.Button("Looped (attached)")) SfxLooped();
        if (GUILayout.Button("Via SoundDefinition ref")) SfxByRef();
        if (GUILayout.Button("Stop all SFX")) StopAllSfx();
        if (GUILayout.Button("Stop looped SFX")) StopLoopedSfx();

        Section("Global");
        if (GUILayout.Button("Toggle pause all SFX")) TogglePauseSfx();
        if (GUILayout.Button("Halve SFX volume")) HalveSfxVolume();
        if (GUILayout.Button("Log active SFX count")) LogActiveSfx();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static void Section(string title)
    {
        GUILayout.Space(6);
        GUILayout.Label($"<b>{title}</b>", RichLabel());
    }

    private static GUIStyle RichLabel()
    {
        return new GUIStyle(GUI.skin.label) { richText = true };
    }
}
