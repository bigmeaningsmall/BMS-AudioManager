#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that mirrors the Resources/Audio clip tree into asset-safe SoundDefinition
/// assets. Re-runnable and idempotent: existing definitions are updated in place rather than
/// duplicated, so you can run it again after adding new clips.
///
/// Menu: BMS AudioManager -> Generate Sound Definitions
/// </summary>
public static class SoundDefinitionGenerator
{
    // Where the source clips live (project-level Resources folder).
    private const string AudioRoot = "Assets/Resources/Audio";

    // Where the generated SoundDefinition assets are written (parallel tree, NOT under Resources).
    private const string OutputRoot = "Assets/_Extensions/BMS AudioManager/SoundDefinitions";

    // Where generated SoundBank assets are written.
    private const string BanksRoot = "Assets/_Extensions/BMS AudioManager/SoundBanks";

    // Maps a source category folder name to its AudioType + a sensible default loop value.
    private struct CategoryInfo
    {
        public AudioType audioType;
        public bool loop;
        public CategoryInfo(AudioType type, bool loop) { this.audioType = type; this.loop = loop; }
    }

    private static readonly Dictionary<string, CategoryInfo> CategoryMap = new Dictionary<string, CategoryInfo>
    {
        { "BGM",                  new CategoryInfo(AudioType.BGM,      true)  },
        { "Ambient",              new CategoryInfo(AudioType.Ambient,  true)  },
        { "Ambience-Environment", new CategoryInfo(AudioType.Ambient,  true)  },
        { "Dialogue",             new CategoryInfo(AudioType.Dialogue, false) },
        { "Aux1",                 new CategoryInfo(AudioType.Aux1,     true)  },
        { "Aux2",                 new CategoryInfo(AudioType.Aux2,     true)  },
        { "SFX",                  new CategoryInfo(AudioType.SFX,      false) },
    };

    [MenuItem("BMS AudioManager/Generate Sound Definitions")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(AudioRoot))
        {
            AudioDebug.LogError($"[SoundDefinitionGenerator] Audio root not found: {AudioRoot}");
            return;
        }

        EnsureFolder(OutputRoot);
        EnsureFolder(BanksRoot);

        int created = 0;
        int updated = 0;
        int skipped = 0;

        // Accumulate definitions per category (+ a master list) so we can (re)build banks afterward.
        var byCategory = new Dictionary<string, List<SoundDefinition>>();
        var allDefs = new List<SoundDefinition>();

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var kvp in CategoryMap)
            {
                string category = kvp.Key;
                CategoryInfo info = kvp.Value;
                string sourceFolder = $"{AudioRoot}/{category}";

                if (!AssetDatabase.IsValidFolder(sourceFolder))
                {
                    // Category folder is optional - quietly skip if absent
                    continue;
                }

                string outputFolder = $"{OutputRoot}/{category}";
                EnsureFolder(outputFolder);

                if (!byCategory.TryGetValue(category, out var categoryDefs))
                {
                    categoryDefs = new List<SoundDefinition>();
                    byCategory[category] = categoryDefs;
                }

                // FindAssets recurses into subfolders (e.g. Ambient/Gentle Music), matching
                // the runtime's Resources.LoadAll behaviour.
                string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { sourceFolder });

                foreach (string guid in guids)
                {
                    string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    if (clip == null) { skipped++; continue; }

                    string safeName = SanitizeFileName(clip.name);
                    string assetPath = $"{outputFolder}/{safeName}.asset";

                    SoundDefinition def = AssetDatabase.LoadAssetAtPath<SoundDefinition>(assetPath);
                    bool isNew = def == null;

                    if (isNew)
                    {
                        def = ScriptableObject.CreateInstance<SoundDefinition>();
                        // Defaults only applied on creation so re-runs don't clobber hand-tuned params
                        def.loop = info.loop;
                        def.clip = clip;
                        def.audioType = info.audioType;
                        AssetDatabase.CreateAsset(def, assetPath);
                        created++;
                    }
                    else
                    {
                        // Update only the asset-identity fields; preserve any tweaked playback params
                        bool changed = false;
                        if (def.clip != clip) { def.clip = clip; changed = true; }
                        if (def.audioType != info.audioType) { def.audioType = info.audioType; changed = true; }
                        if (changed)
                        {
                            EditorUtility.SetDirty(def);
                            updated++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }

                    // Record for bank generation (both new and existing defs)
                    categoryDefs.Add(def);
                    allDefs.Add(def);
                }
            }

            // (Re)build one bank per category plus a master bank of everything.
            foreach (var pair in byCategory)
            {
                WriteBank($"{BanksRoot}/{pair.Key}.asset", pair.Key, pair.Value);
            }
            WriteBank($"{BanksRoot}/MasterBank.asset", "Master", allDefs);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Use Debug.Log directly (not AudioDebug) - this runs in edit mode where there's no
        // AudioManager.Instance to gate logging, so AudioDebug would be silently suppressed.
        Debug.Log($"[SoundDefinitionGenerator] Done. Created: {created}, Updated: {updated}, Unchanged: {skipped}. " +
                  $"Banks: {byCategory.Count} category + 1 master ({allDefs.Count} defs). Output: {OutputRoot}, {BanksRoot}");
    }

    /// <summary>
    /// Creates or updates a SoundBank asset at the given path, replacing its contents with the
    /// supplied definitions. Idempotent: existing bank assets are reused (keeps references intact).
    /// </summary>
    private static void WriteBank(string assetPath, string bankId, List<SoundDefinition> defs)
    {
        SoundBank bank = AssetDatabase.LoadAssetAtPath<SoundBank>(assetPath);
        if (bank == null)
        {
            bank = ScriptableObject.CreateInstance<SoundBank>();
            bank.bankId = bankId;
            bank.sounds = new List<SoundDefinition>(defs);
            AssetDatabase.CreateAsset(bank, assetPath);
        }
        else
        {
            bank.bankId = bankId;
            bank.sounds = new List<SoundDefinition>(defs);
            EditorUtility.SetDirty(bank);
        }
    }

    /// <summary>
    /// Creates a folder (and any missing parents) using AssetDatabase, which requires each
    /// level's parent to already exist.
    /// </summary>
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
#endif
