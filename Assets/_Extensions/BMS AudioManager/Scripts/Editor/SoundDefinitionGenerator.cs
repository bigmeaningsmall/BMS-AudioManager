#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    // Where the generated SoundId enum (typed, string-free keys) is written. Runtime code, not editor.
    private const string GeneratedScriptsRoot = "Assets/_Extensions/BMS AudioManager/Scripts/Generated";
    private const string SoundIdPath = GeneratedScriptsRoot + "/SoundId.cs";

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

        // Intended display name (sanitized clip name) per def, captured at loop time. Used for enum
        // member names so codegen doesn't depend on MoveAsset updating def.name within the batch.
        var displayNames = new Dictionary<SoundDefinition, string>();

        // Index existing definitions by the GUID of the clip they reference. This is what makes
        // identity survive renames/moves: a renamed .wav keeps its GUID, so we find and update the
        // existing definition (preserving its id) instead of spawning a duplicate.
        // Built BEFORE StartAssetEditing so the search index is complete.
        Dictionary<string, SoundDefinition> byClipGuid = BuildClipGuidIndex();

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
                    string desiredPath = $"{outputFolder}/{safeName}.asset";

                    // Match by the clip's GUID (the loop variable `guid` IS the clip GUID), so a
                    // renamed/moved clip updates its existing definition in place.
                    if (byClipGuid.TryGetValue(guid, out SoundDefinition def) && def != null)
                    {
                        bool changed = false;

                        // Rename/move the definition asset to track the clip's current name & category.
                        string currentPath = AssetDatabase.GetAssetPath(def);
                        if (currentPath != desiredPath)
                        {
                            string moveError = AssetDatabase.MoveAsset(currentPath, desiredPath);
                            if (!string.IsNullOrEmpty(moveError))
                                Debug.LogWarning($"[SoundDefinitionGenerator] Could not move '{currentPath}' -> '{desiredPath}': {moveError}");
                            else
                                changed = true;
                        }

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
                    else
                    {
                        def = ScriptableObject.CreateInstance<SoundDefinition>();
                        // Defaults only applied on creation so re-runs don't clobber hand-tuned params
                        def.loop = info.loop;
                        def.clip = clip;
                        def.audioType = info.audioType;
                        AssetDatabase.CreateAsset(def, desiredPath);
                        byClipGuid[guid] = def; // so duplicate clip refs in one run don't double-create
                        created++;
                    }

                    // Record for bank generation (both new and existing defs)
                    categoryDefs.Add(def);
                    allDefs.Add(def);
                    displayNames[def] = safeName; // enum member name source (rename-safe)
                }
            }

            // (Re)build one bank per category plus a master bank of everything.
            foreach (var pair in byCategory)
            {
                WriteBank($"{BanksRoot}/{pair.Key}.asset", pair.Key, pair.Value);
            }
            WriteBank($"{BanksRoot}/MasterBank.asset", "Master", allDefs);

            // Stage 3: assign stable ids and (re)generate the SoundId enum.
            // Use the in-memory allDefs list (every definition touched this run) rather than
            // re-querying with FindAssets - newly created assets aren't in the search index until
            // StopAssetEditing, so a FindAssets here would miss them on a first run.
            AssignStableIds(allDefs);
            GenerateSoundIdEnum(allDefs, displayNames);
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
    /// Maps each existing SoundDefinition to the GUID of the clip it references. Used to match
    /// definitions to clips by GUID (rename/move safe) rather than by asset name.
    /// </summary>
    private static Dictionary<string, SoundDefinition> BuildClipGuidIndex()
    {
        var index = new Dictionary<string, SoundDefinition>();
        string[] guids = AssetDatabase.FindAssets("t:SoundDefinition", new[] { OutputRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<SoundDefinition>(path);
            if (def == null || def.clip == null) continue;

            string clipPath = AssetDatabase.GetAssetPath(def.clip);
            string clipGuid = AssetDatabase.AssetPathToGUID(clipPath);
            if (string.IsNullOrEmpty(clipGuid)) continue;

            if (index.ContainsKey(clipGuid))
                Debug.LogWarning($"[SoundDefinitionGenerator] Multiple definitions reference the same clip ('{clipPath}'). Keeping '{path}', ignoring earlier duplicate.");
            index[clipGuid] = def; // last wins
        }
        return index;
    }

    /// <summary>
    /// Assigns a stable, never-reused id to any definition that doesn't have one (id == 0).
    /// Existing ids are preserved so enum values stay stable across re-runs and renames.
    /// </summary>
    private static void AssignStableIds(List<SoundDefinition> defs)
    {
        int maxId = 0;
        foreach (var d in defs)
            if (d.id > maxId) maxId = d.id;

        // Deterministic order (by asset name) so first-run id assignment is reproducible.
        var unassigned = new List<SoundDefinition>();
        foreach (var d in defs)
            if (d.id == 0) unassigned.Add(d);
        unassigned.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        foreach (var d in unassigned)
        {
            d.id = ++maxId;
            EditorUtility.SetDirty(d);
        }
    }

    /// <summary>Writes the SoundId enum (one member per definition, value = its stable id).</summary>
    private static void GenerateSoundIdEnum(List<SoundDefinition> defs, Dictionary<SoundDefinition, string> displayNames)
    {
        // Sort by id for stable, readable output.
        var sorted = new List<SoundDefinition>(defs);
        sorted.Sort((a, b) => a.id.CompareTo(b.id));

        var usedNames = new HashSet<string>();
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("//   Generated by BMS AudioManager -> Generate Sound Definitions.");
        sb.AppendLine("//   DO NOT EDIT BY HAND - re-run the generator to update.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("/// <summary>Typed, string-free keys for every SoundDefinition. Enum values are the definitions' stable ids.</summary>");
        sb.AppendLine("public enum SoundId");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");

        foreach (var d in sorted)
        {
            if (d.id == 0) continue; // shouldn't happen after AssignStableIds

            // Prefer the captured display name (rename-safe); fall back to the asset name.
            string rawName = (displayNames != null && displayNames.TryGetValue(d, out string dn)) ? dn : d.name;
            string member = SanitizeIdentifier(rawName);
            if (usedNames.Contains(member))
                member = $"{member}_{d.id}"; // id is unique, so this is guaranteed unique
            usedNames.Add(member);

            sb.AppendLine($"    {member} = {d.id},");
        }

        sb.AppendLine("}");

        // Working directory is the Unity project root, so the project-relative path is valid on disk.
        Directory.CreateDirectory(GeneratedScriptsRoot);
        File.WriteAllText(SoundIdPath, sb.ToString());
        AssetDatabase.ImportAsset(SoundIdPath);
    }

    // C# reserved keywords - a generated enum member matching one would not compile, so prefix '_'.
    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const",
        "continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern",
        "false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface",
        "internal","is","lock","long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc",
        "static","string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked",
        "unsafe","ushort","using","virtual","void","volatile","while"
    };

    /// <summary>Converts an arbitrary asset name into a valid, non-keyword C# identifier.</summary>
    private static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "_";

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

        string s = sb.ToString();
        if (char.IsDigit(s[0])) s = "_" + s;        // can't start with a digit
        if (CSharpKeywords.Contains(s)) s = "_" + s; // can't be a reserved keyword
        return s;
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
