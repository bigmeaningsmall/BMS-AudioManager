using UnityEngine;

/// <summary>
/// Configuration for the Sound Definition generator (editor tool). Lives as an asset so the paths
/// travel with the project/template. Only the editor generator reads this - it has no runtime cost.
///
/// The generator auto-creates one of these (with the defaults below) the first time it runs if none
/// exists. Edit the asset to point the generator at a different audio folder or output locations.
/// </summary>
[CreateAssetMenu(fileName = "SoundGeneratorSettings", menuName = "BMS AudioManager/Sound Generator Settings")]
public class SoundGeneratorSettings : ScriptableObject
{
    [Header("Source - Where the Audio Clips are Located")]
    [Tooltip("Folder the generator scans for AudioClips. Must contain category subfolders " +
             "(BGM, Ambient, Dialogue, Aux1, Aux2, SFX, Ambience-Environment). It does NOT need to be " +
             "under Resources - and shouldn't be, since runtime loads via SoundDefinitions, not Resources " +
             "(Resources folders are always bundled into builds).")]
    public string audioSourceRoot = "Assets/Audio";

    [Header("Output - Generated Sound Banks and Definitions")]
    [Tooltip("Where generated SoundDefinition assets are written.")]
    public string soundDefinitionsRoot = "Assets/_Extensions/BMS AudioManager/SoundDefinitions";

    [Tooltip("Where generated SoundBank assets are written.")]
    public string soundBanksRoot = "Assets/_Extensions/BMS AudioManager/SoundBanks";

    [Tooltip("Where the generated SoundId.cs enum is written (runtime script).")]
    public string generatedScriptsRoot = "Assets/_Extensions/BMS AudioManager/Scripts/Generated";
}
