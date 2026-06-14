using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads one or more SoundBanks into the AudioRegistry while this component is enabled, and
/// unloads them when it is disabled/destroyed. Drop it on a GameObject in a scene (or on a scene
/// root) and assign the banks that scene needs - they become available on enter and are released
/// on exit. Ref-counting in the registry means banks shared with other loaders/scenes survive
/// until the last one unloads.
/// </summary>
public class SceneAudioBank : MonoBehaviour
{
    [Header("Enable / Disable Component to Load or Unload Sound Banks to the AudioManager")]
    [Space(5)]
    [Header("Used to control which assets are available in the scene")]
    [Space(5)]
    [Header("Assign the SoundBanks that should be loaded while this component is active.")]
    [Space(10)]
    [Tooltip("Banks to load while this component is active.")]
    public List<SoundBank> banks = new List<SoundBank>();

    private bool loaded = false;
    private Coroutine waitRoutine;

    private void OnEnable()
    {
        // AudioManager may not exist yet on the first frame - wait for it like the senders do.
        if (AudioManager.Instance != null)
        {
            LoadAll();
        }
        else
        {
            waitRoutine = StartCoroutine(WaitForAudioManagerThenLoad());
        }
    }

    private IEnumerator WaitForAudioManagerThenLoad()
    {
        while (AudioManager.Instance == null)
            yield return null;

        waitRoutine = null;
        LoadAll();
    }

    private void LoadAll()
    {
        foreach (var bank in banks)
        {
            if (bank == null) continue;
            AudioManager.Instance.LoadBank(bank);
        }
        loaded = true;
    }

    private void OnDisable()
    {
        // Cancel a pending wait (AudioManager never arrived, or we're disabled mid-wait)
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        // Only unload what we actually loaded, and only if the manager still exists
        if (loaded && AudioManager.Instance != null)
        {
            foreach (var bank in banks)
            {
                if (bank == null) continue;
                AudioManager.Instance.UnloadBank(bank);
            }
        }
        loaded = false;
    }
}
