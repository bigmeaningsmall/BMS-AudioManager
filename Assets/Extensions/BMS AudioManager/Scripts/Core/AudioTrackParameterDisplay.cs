using UnityEngine;

/// <summary>
/// Component to display AudioManager track parameters in the inspector for debugging/monitoring.
/// Attach this to any GameObject to monitor all three audio tracks.
/// </summary>
public class AudioTrackParameterDisplay : MonoBehaviour
{
    [Header("Audio Track Parameters - Live Display")]
    [Space(10)]
    
    [Header("BGM Track")]
    [SerializeField] private AudioTrackParamters bgmParameters;
    
    [Header("Ambient Track")]
    [SerializeField] private AudioTrackParamters ambientParameters;
    
    [Header("Dialogue Track")]
    [SerializeField] private AudioTrackParamters dialogueParameters;
    
    [Space(10)]
    [Header("Settings")]
    [SerializeField] private bool updateInEditMode = false;
    [SerializeField] private float updateInterval = 0.1f; // Update frequency in seconds
    
    private float lastUpdateTime;
    
    private void Update()
    {
        // Only update at specified intervals for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        // Skip if AudioManager not available
        if (AudioManager.Instance == null) return;
        
        // Update all track parameters using the generic method
        UpdateTrackParameters();
    }
    
    private void OnValidate()
    {
        // Update in edit mode if enabled
        if (updateInEditMode && !Application.isPlaying)
        {
            UpdateTrackParameters();
        }
    }
    
    private void UpdateTrackParameters()
    {
        if (AudioManager.Instance == null) return;
        
        // Get parameters using the generic method
        bgmParameters = AudioManager.Instance.GetTrackParameters(AudioTrackType.BGM);
        ambientParameters = AudioManager.Instance.GetTrackParameters(AudioTrackType.Ambient);
        dialogueParameters = AudioManager.Instance.GetTrackParameters(AudioTrackType.Dialogue);
        
        // Alternative: Non-generic method approach (commented out)
        // bgmParameters = AudioManager.Instance.BGMParameters;
        // ambientParameters = AudioManager.Instance.AmbientParameters;
        // dialogueParameters = AudioManager.Instance.DialogueParameters;
    }
    
    /// <summary>
    /// Manual refresh for testing purposes
    /// </summary>
    [ContextMenu("Refresh Parameters")]
    public void RefreshParameters()
    {
        UpdateTrackParameters();
        Debug.Log("[AudioParameterDisplay] Parameters refreshed manually");
    }
    
    /// <summary>
    /// Get a specific track's parameters
    /// </summary>
    public AudioTrackParamters GetDisplayedParameters(AudioTrackType trackType)
    {
        return trackType switch
        {
            AudioTrackType.BGM => bgmParameters,
            AudioTrackType.Ambient => ambientParameters,
            AudioTrackType.Dialogue => dialogueParameters,
            _ => null
        };
    }
    
    /// <summary>
    /// Check if all tracks are available
    /// </summary>
    public bool AreParametersValid()
    {
        return bgmParameters != null && ambientParameters != null && dialogueParameters != null;
    }
}