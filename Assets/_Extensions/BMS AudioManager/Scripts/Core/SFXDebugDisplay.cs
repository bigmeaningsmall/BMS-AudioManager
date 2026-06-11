using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component to display SFX debugging information in the inspector.
/// Shows active SFX count, names, and provides controls for SFX management.
/// Attach this to the AudioManager GameObject for easy SFX monitoring.
/// </summary>
public class SFXDebugDisplay : MonoBehaviour
{
    [Header("SFX Debug Information - Live Display")]
    [Space(10)]
    
    [Header("Active SFX Statistics")]
    [SerializeField] private int activeSFXCount;
    [SerializeField] private int delayedSFXCount;
    
    [Header("Active SFX Names")]
    [SerializeField] private List<string> activeSFXNames = new List<string>();
    
    [Space(10)]
    [Header("Settings")]
    [SerializeField] private bool updateInEditMode = false;
    [SerializeField] private float updateInterval = 0.5f; // Update frequency in seconds
    [SerializeField] private int maxNamesToShow = 20; // Limit displayed names for performance
    
    [Space(10)]
    [Header("SFX Control Buttons")]
    [SerializeField] private bool showControlButtons = true;
    
    private float lastUpdateTime;
    
    private void Update()
    {
        // Only update at specified intervals for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        // Skip if AudioManager not available
        if (AudioManager.Instance == null) return;
        
        // Update SFX information
        UpdateSFXInformation();
    }
    
    private void OnValidate()
    {
        // Update in edit mode if enabled
        if (updateInEditMode && !Application.isPlaying)
        {
            UpdateSFXInformation();
        }
    }
    
    private void UpdateSFXInformation()
    {
        if (AudioManager.Instance == null) return;
        
        // Get active SFX count
        activeSFXCount = AudioManager.Instance.GetActiveSFXCount();
        
        // Get delayed SFX count
        delayedSFXCount = GetDelayedSFXCount();
        
        // Get active SFX names
        string[] sfxNames = AudioManager.Instance.GetActiveSFXNames();
        activeSFXNames.Clear();
        
        // Limit displayed names for performance
        int namesToShow = Mathf.Min(sfxNames.Length, maxNamesToShow);
        for (int i = 0; i < namesToShow; i++)
        {
            activeSFXNames.Add(sfxNames[i]);
        }
        
        // Add note if there are more SFX than displayed
        if (sfxNames.Length > maxNamesToShow)
        {
            activeSFXNames.Add($"... and {sfxNames.Length - maxNamesToShow} more");
        }
    }
    
    private int GetDelayedSFXCount()
    {
        // Access the delayed SFX coroutines count
        // Note: This would require making delayedSFXCoroutines public or adding a getter
        // For now, return 0 or implement a getter in AudioManager
        return 0; // TODO: Implement getter in AudioManager if needed
    }
    
    /// <summary>
    /// Manual refresh for testing purposes
    /// </summary>
    [ContextMenu("Refresh SFX Information")]
    public void RefreshSFXInformation()
    {
        UpdateSFXInformation();
        AudioDebug.Log("[SFXDebugDisplay] SFX information refreshed manually");
    }
    
    /// <summary>
    /// Stop all SFX - callable from context menu
    /// </summary>
    [ContextMenu("Stop All SFX")]
    public void StopAllSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSFX();
            AudioDebug.Log("[SFXDebugDisplay] Stopped all SFX via debug display");
        }
    }
    
    /// <summary>
    /// Stop all looped SFX - callable from context menu
    /// </summary>
    [ContextMenu("Stop All Looped SFX")]
    public void StopAllLoopedSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllLoopedSFX();
            AudioDebug.Log("[SFXDebugDisplay] Stopped all looped SFX via debug display");
        }
    }
    
    /// <summary>
    /// Pause all SFX - callable from context menu
    /// </summary>
    [ContextMenu("Pause All SFX")]
    public void PauseAllSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAllSFX(true);
            AudioDebug.Log("[SFXDebugDisplay] Paused all SFX via debug display");
        }
    }
    
    /// <summary>
    /// Resume all SFX - callable from context menu
    /// </summary>
    [ContextMenu("Resume All SFX")]
    public void ResumeAllSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAllSFX(false);
            AudioDebug.Log("[SFXDebugDisplay] Resumed all SFX via debug display");
        }
    }
    
    /// <summary>
    /// Cancel all delayed SFX - callable from context menu
    /// </summary>
    [ContextMenu("Cancel All Delayed SFX")]
    public void CancelAllDelayedSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CancelAllDelayedSFX();
            AudioDebug.Log("[SFXDebugDisplay] Cancelled all delayed SFX via debug display");
        }
    }
    
    /// <summary>
    /// Log detailed SFX information to console
    /// </summary>
    [ContextMenu("Log SFX Details")]
    public void LogSFXDetails()
    {
        if (AudioManager.Instance == null)
        {
            AudioDebug.LogWarning("[SFXDebugDisplay] AudioManager not available");
            return;
        }
        
        string[] sfxNames = AudioManager.Instance.GetActiveSFXNames();
        int count = AudioManager.Instance.GetActiveSFXCount();
        
        AudioDebug.Log($"[SFXDebugDisplay] === SFX DEBUG REPORT ===");
        AudioDebug.Log($"[SFXDebugDisplay] Active SFX Count: {count}");
        AudioDebug.Log($"[SFXDebugDisplay] Delayed SFX Count: {delayedSFXCount}");
        
        if (sfxNames.Length > 0)
        {
            AudioDebug.Log($"[SFXDebugDisplay] Active SFX Names:");
            for (int i = 0; i < sfxNames.Length; i++)
            {
                AudioDebug.Log($"[SFXDebugDisplay]   {i + 1}. {sfxNames[i]}");
            }
        }
        else
        {
            AudioDebug.Log($"[SFXDebugDisplay] No active SFX currently playing");
        }
        
        AudioDebug.Log($"[SFXDebugDisplay] === END REPORT ===");
    }
    
    /// <summary>
    /// Check if SFX system is working properly
    /// </summary>
    public bool IsSFXSystemHealthy()
    {
        return AudioManager.Instance != null && activeSFXCount >= 0;
    }
    
    /// <summary>
    /// Get a summary string for external use
    /// </summary>
    public string GetSFXSummary()
    {
        return $"Active: {activeSFXCount}, Delayed: {delayedSFXCount}";
    }
}