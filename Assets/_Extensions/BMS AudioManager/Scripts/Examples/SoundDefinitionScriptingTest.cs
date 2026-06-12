using UnityEngine;

/// <summary>
/// Minimal scripting test for the SoundDefinition code API.
///
/// HOW TO USE:
///   1. Run "BMS AudioManager -> Generate Sound Definitions" so SoundDefinition assets exist.
///   2. Put an AudioManager in the scene (with its child tracks), as usual.
///   3. Add this component to any GameObject and assign the four SoundDefinition fields
///      by dragging generated assets from Assets/_Extensions/BMS AudioManager/SoundDefinitions/.
///   4. Enter Play mode and press the keys below.
///
/// This demonstrates that scripting still goes purely through events + parameters -
/// the SoundDefinition simply supplies the clip (and its defaults) instead of a string.
/// </summary>
public class SoundDefinitionScriptingTest : MonoBehaviour
{
    [Header("Assign generated SoundDefinition assets")]
    [Tooltip("A BGM/Ambient/Aux definition - played on its own channel via PlayTrack.")]
    public SoundDefinition musicTrack;

    [Tooltip("A one-shot SFX definition (optionally with variations for random selection).")]
    public SoundDefinition sfx;

    [Tooltip("A looping SFX definition (e.g. an engine/fire loop).")]
    public SoundDefinition loopingSfx;

    [Header("Options")]
    [Tooltip("When playing the 3D SFX, attach it to this transform (defaults to this object).")]
    public Transform sfxAttachPoint;

    private void Awake()
    {
        if (sfxAttachPoint == null) sfxAttachPoint = transform;
    }

    private void Update()
    {
        // ---- TRACKS (channel-based) ----
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Simplest call: routes to the definition's own channel, uses its default params.
            AudioEvent.PlayTrack(musicTrack);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Same clip, volume override - everything else from the definition.
            AudioEvent.PlayTrack(musicTrack, 0.3f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Stop is CHANNEL based, not clip based. Passing the definition makes the fade-out
            // use the SAME fade duration + target as the fade-in (symmetric), instead of the
            // string overload's volume-only default.
            AudioEvent.StopTrack(musicTrack);
        }

        // ---- SFX (fire-and-forget instances) ----
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 2D one-shot using the definition's defaults (random variation if it has any).
            AudioEvent.PlaySFX(sfx);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            // 3D one-shot attached to a transform.
            AudioEvent.PlaySFX(sfx, sfxAttachPoint);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Looping SFX (remember to stop SFX via the AudioManager when needed).
            AudioEvent.PlayLoopedSFX(loopingSfx, sfxAttachPoint);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // Stop ALL SFX (looped + one-shots) through the AudioManager.
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopAllSFX();
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 360, 200), GUI.skin.box);
        GUILayout.Label("SoundDefinition Scripting Test");
        GUILayout.Label("1 - Play music track (def defaults)");
        GUILayout.Label("2 - Play music track @ volume 0.3");
        GUILayout.Label("3 - Stop music channel (fade 1.5s)");
        GUILayout.Label("Q - SFX 2D one-shot");
        GUILayout.Label("W - SFX 3D at attach point");
        GUILayout.Label("E - Looping SFX");
        GUILayout.Label("R - Stop all SFX");
        GUILayout.EndArea();
    }
}
