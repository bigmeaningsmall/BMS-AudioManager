using UnityEngine;

/// <summary>
/// Static debug helper for the Audio System
/// Provides centralized debug logging control
/// </summary>
public static class AudioDebug
{
    // This will be controlled by AudioManager
    public static bool IsEnabled => AudioManager.Instance != null && AudioManager.Instance.EnableDebugLogging;
    
    /// <summary>
    /// Log debug message if debugging is enabled
    /// </summary>
    public static void Log(string message)
    {
        if (IsEnabled)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Log warning message if debugging is enabled
    /// </summary>
    public static void LogWarning(string message)
    {
        if (IsEnabled)
        {
            Debug.LogWarning(message);
        }
    }
    
    /// <summary>
    /// Log error message (always shown regardless of debug setting)
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
    
    /// <summary>
    /// Conditional log - only logs if condition is true AND debugging enabled
    /// </summary>
    public static void LogIf(bool condition, string message)
    {
        if (condition && IsEnabled)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Log with color formatting (Unity Rich Text)
    /// </summary>
    public static void LogColored(string message, string color = "cyan")
    {
        if (IsEnabled)
        {
            Debug.Log($"<color={color}>{message}</color>");
        }
    }
    
    /// <summary>
    /// Log with category prefix
    /// </summary>
    public static void LogCategory(string category, string message)
    {
        if (IsEnabled)
        {
            Debug.Log($"[{category}] {message}");
        }
    }
}