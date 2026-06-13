using System.Collections.Generic;

/// <summary>
/// Runtime catalogue of the SoundDefinitions that are currently loaded (available to play).
///
/// Banks are loaded/unloaded into the registry with reference counting, so a SoundDefinition that
/// lives in more than one loaded bank (e.g. a shared UI click in both a global bank and a scene
/// bank) is only removed once the LAST referencing bank unloads. This prevents one scene unloading
/// from yanking sounds another scene still needs.
///
/// Stage 1-2 keys by SoundDefinition reference (plus a by-name index for convenience/testing).
/// The Stage 3 typed-catalog (SoundId) layer will add an id index on top without changing this.
/// </summary>
public class AudioRegistry
{
    private class Entry
    {
        public SoundDefinition def;
        public int refCount;
    }

    private readonly Dictionary<SoundDefinition, Entry> entries = new Dictionary<SoundDefinition, Entry>();
    private readonly Dictionary<string, SoundDefinition> byName = new Dictionary<string, SoundDefinition>();
    private readonly Dictionary<int, SoundDefinition> byId = new Dictionary<int, SoundDefinition>();

    /// <summary>Number of distinct SoundDefinitions currently loaded.</summary>
    public int Count => entries.Count;

    /// <summary>Registers every SoundDefinition in the bank, incrementing ref counts.</summary>
    public void LoadBank(SoundBank bank)
    {
        if (bank == null)
        {
            AudioDebug.LogWarning("[AudioRegistry] LoadBank called with null bank.");
            return;
        }

        int added = 0;
        foreach (var def in bank.sounds)
        {
            if (def == null) continue;

            if (entries.TryGetValue(def, out Entry e))
            {
                e.refCount++;
            }
            else
            {
                entries[def] = new Entry { def = def, refCount = 1 };
                byName[def.name] = def; // last-wins if two defs share a name

                // Id index powers the SoundId typed API. id 0 = not yet generated.
                if (def.id != 0)
                    byId[def.id] = def;
                else
                    AudioDebug.LogWarning($"[AudioRegistry] '{def.name}' has id 0 (not generated) - it won't be reachable via SoundId. Run 'Generate Sound Definitions'.");

                added++;
            }
        }

        AudioDebug.Log($"[AudioRegistry] Loaded bank '{bank.Label}' (+{added} new, {bank.sounds.Count} total in bank). Registry now holds {entries.Count}.");
    }

    /// <summary>Decrements ref counts for the bank's SoundDefinitions, removing any that hit zero.</summary>
    public void UnloadBank(SoundBank bank)
    {
        if (bank == null)
        {
            AudioDebug.LogWarning("[AudioRegistry] UnloadBank called with null bank.");
            return;
        }

        int removed = 0;
        foreach (var def in bank.sounds)
        {
            if (def == null) continue;

            if (entries.TryGetValue(def, out Entry e))
            {
                e.refCount--;
                if (e.refCount <= 0)
                {
                    entries.Remove(def);
                    // Only clear the name index if it still points at THIS def
                    if (byName.TryGetValue(def.name, out SoundDefinition mapped) && mapped == def)
                        byName.Remove(def.name);
                    // Same for the id index
                    if (def.id != 0 && byId.TryGetValue(def.id, out SoundDefinition mappedById) && mappedById == def)
                        byId.Remove(def.id);
                    removed++;
                }
            }
        }

        AudioDebug.Log($"[AudioRegistry] Unloaded bank '{bank.Label}' (-{removed} removed). Registry now holds {entries.Count}.");
    }

    /// <summary>True if the given definition is currently loaded.</summary>
    public bool Contains(SoundDefinition def) => def != null && entries.ContainsKey(def);

    /// <summary>Look up a loaded definition by its asset name (convenience / transition aid).</summary>
    public SoundDefinition Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return byName.TryGetValue(name, out SoundDefinition def) ? def : null;
    }

    /// <summary>Look up a loaded definition by its stable id (the SoundId enum value).</summary>
    public SoundDefinition Get(int id)
    {
        if (id == 0) return null;
        return byId.TryGetValue(id, out SoundDefinition def) ? def : null;
    }

    /// <summary>All currently loaded definitions (e.g. for debug listings).</summary>
    public IEnumerable<SoundDefinition> All => entries.Keys;

    /// <summary>Clears the entire registry. Mainly for teardown / tests.</summary>
    public void Clear()
    {
        entries.Clear();
        byName.Clear();
        byId.Clear();
    }
}
