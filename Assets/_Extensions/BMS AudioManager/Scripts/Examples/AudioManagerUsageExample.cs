using UnityEngine;

/// <summary>
/// CORRECTED usage examples showing the right way to use AudioManager
/// Uses both direct event calls (with all parameters) and helper method overloads
/// </summary>
public class AudioManagerUsageExample : MonoBehaviour
{
    [Header("Example Objects")]
    public Transform playerTransform;
    public Transform ambientLocationTransform;
    
    private void Start()
    {
        StartCoroutine(WaitAndRunExamples());
    }
    
    private System.Collections.IEnumerator WaitAndRunExamples()
    {
        yield return null; // Wait one frame
        
        // Uncomment the examples you want to test
        // DirectEventExamples();
        // HelperMethodExamples();
        // ComplexScenarioExamples();
    }
    
    // ==================== DIRECT EVENT USAGE (Original Way) ====================
    /// <summary>
    /// Examples using the events directly - you MUST provide ALL parameters
    /// </summary>
    private void DirectEventExamples()
    {
        AudioDebug.Log("=== DIRECT EVENT EXAMPLES (All Parameters Required) ===");
        
        // PLAY TRACK - ALL parameters must be provided
        AudioEventManager.PlayTrack(
            AudioTrackType.BGM,           // trackType
            -1,                          // trackNumber (-1 to ignore)
            "MainTheme",                 // trackName
            1f,                          // volume
            1f,                          // pitch
            0f,                          // spatialBlend
            FadeType.FadeInOut,          // fadeType
            0.5f,                        // fadeDuration
            FadeTarget.FadeBoth,         // fadeTarget
            true,                        // loop
            0f,                          // delay
            null,                        // attachTo
            ""                           // eventName
        );
        
        // STOP TRACK - ALL parameters must be provided
        AudioEventManager.StopTrack(
            AudioTrackType.BGM,          // trackType
            1f,                          // fadeDuration
            FadeTarget.FadeVolume,       // fadeTarget
            0f,                          // delay
            ""                           // eventName
        );
        
        // PAUSE TRACK - ALL parameters must be provided
        AudioEventManager.PauseTrack(
            AudioTrackType.Ambient,      // trackType
            0f,                          // fadeDuration
            FadeTarget.FadeVolume,       // fadeTarget
            0f,                          // delay
            ""                           // eventName
        );
        
        // ADJUST TRACK - ALL parameters must be provided
        AudioEventManager.AdjustTrack(
            AudioTrackType.BGM,          // trackType
            0.5f,                        // volume
            1f,                          // pitch
            0f,                          // spatialBlend
            1f,                          // fadeDuration
            FadeTarget.FadeVolume,       // fadeTarget
            true,                        // loop
            0f,                          // delay
            null,                        // attachTo
            ""                           // eventName
        );
        
        // PLAY SFX - ALL parameters must be provided
        AudioEventManager.PlaySFX(
            new string[] { "ButtonClick" }, // soundName array
            1f,                             // volume
            1f,                             // pitch
            false,                          // randomisePitch
            0.1f,                           // pitchRange
            0f,                             // spatialBlend
            false,                          // loop
            0f,                             // delay
            100f,                           // percentChanceToPlay
            null,                           // attachTo
            Vector3.zero,                   // position
            1f,                             // minDist
            500f,                           // maxDist
            ""                              // eventName
        );
    }
    
    // ==================== HELPER METHOD USAGE (Easy Way) ====================
    /// <summary>
    /// Examples using the helper methods - much easier!
    /// </summary>
    private void HelperMethodExamples()
    {
        AudioDebug.Log("=== HELPER METHOD EXAMPLES (Easy Usage) ===");
        
        // SIMPLE TRACK OPERATIONS
        AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");
        AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f);
        AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 2f);
        AudioEvent.PlayTrack(AudioTrackType.Ambient, "ForestAmbient", 0.6f, 2f, ambientLocationTransform);
        
        // STOP AND PAUSE
        AudioEvent.StopTrack(AudioTrackType.BGM);
        AudioEvent.StopTrack(AudioTrackType.BGM, 2f);
        AudioEvent.PauseTrack(AudioTrackType.Ambient);
        AudioEvent.PauseTrack(AudioTrackType.Ambient, 1f);
        
        // ADJUST TRACKS
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f);
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 2f);
        AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.7f, 1.2f);
        AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.7f, 1.2f, 3f);
        
        // SIMPLE SFX
        AudioEvent.PlaySFX("ButtonClick");
        AudioEvent.PlaySFX("Explosion", 0.8f);
        AudioEvent.PlaySFX(new string[] { "Footstep1", "Footstep2", "Footstep3" });
        AudioEvent.PlaySFX("DoorCreak", 0.7f, true); // with random pitch
        AudioEvent.PlaySFX("MagicSpell", 0.8f, playerTransform); // 3D attached
        AudioEvent.PlaySFX("Explosion", 1f, new Vector3(10, 0, 5)); // 3D position
        
        // ADVANCED SFX
        AudioEvent.PlaySFX3D("DistantThunder", 0.6f, ambientLocationTransform, 5f, 100f);
        AudioEvent.PlayLoopedSFX("EngineHum", 0.7f, playerTransform);
        AudioEvent.PlayRandomSFX(new string[] { "Bird1", "Bird2", "Bird3" }, 0.4f, 30f, 2f);
    }
    
    // ==================== COMPLEX SCENARIOS ====================
    /// <summary>
    /// Real-world usage scenarios combining both approaches
    /// </summary>
    private void ComplexScenarioExamples()
    {
        AudioDebug.Log("=== COMPLEX SCENARIO EXAMPLES ===");
        
        StartCoroutine(CombatSequenceExample());
        StartCoroutine(EnvironmentalTransitionExample());
    }
    
    private System.Collections.IEnumerator CombatSequenceExample()
    {
        AudioDebug.Log("Starting Combat Sequence...");
        
        // 1. Quick fade out current music
        AudioEvent.StopTrack(AudioTrackType.BGM, 1f);
        
        // 2. Warning sound
        yield return new WaitForSeconds(0.5f);
        AudioEvent.PlaySFX("WarningAlarm", 0.8f);
        
        // 3. Start tense ambient
        AudioEvent.PlayTrack(AudioTrackType.Ambient, "TensionAmbient", 0.5f, 1f);
        
        // 4. Combat music with crossfade (needs full control - use direct event)
        yield return new WaitForSeconds(2f);
        AudioEventManager.PlayTrack(
            AudioTrackType.BGM,
            -1,
            "BattleMusic",
            0.9f,                        // volume
            1f,                          // pitch
            0f,                          // spatialBlend
            FadeType.Crossfade,          // crossfade for smooth transition
            3f,                          // fadeDuration
            FadeTarget.FadeBoth,         // fadeTarget
            true,                        // loop
            0f,                          // delay
            null,                        // attachTo
            "Combat Start"               // eventName
        );
        
        AudioDebug.Log("Combat Sequence Complete!");
    }
    
    private System.Collections.IEnumerator EnvironmentalTransitionExample()
    {
        AudioDebug.Log("Starting Environmental Transition...");
        
        // 1. Start forest ambient (simple)
        AudioEvent.PlayTrack(AudioTrackType.Ambient, "ForestAmbient", 0.7f, 1f, ambientLocationTransform);
        
        // 2. Forest sounds with randomization (needs full control)
        AudioEventManager.PlaySFX(
            new string[] { "LeafRustle1", "LeafRustle2", "BirdChirp" },
            0.4f,                        // volume
            1f,                          // pitch
            true,                        // randomisePitch
            0.3f,                        // pitchRange
            0.8f,                        // spatialBlend
            false,                       // loop
            Random.Range(1f, 3f),        // delay
            60f,                         // percentChanceToPlay
            ambientLocationTransform,    // attachTo
            Vector3.zero,                // position
            2f,                          // minDist
            30f,                         // maxDist
            "Forest Sounds"              // eventName
        );
        
        yield return new WaitForSeconds(5f);
        
        // 3. Fade to cave (simple adjustment)
        AudioEvent.AdjustTrackVolume(AudioTrackType.Ambient, 0.3f, 2f);
        
        yield return new WaitForSeconds(1f);
        
        // 4. Cave entrance sound
        AudioEvent.PlaySFX3D("CaveEcho", 0.8f, ambientLocationTransform, 3f, 25f);
        
        yield return new WaitForSeconds(2f);
        
        // 5. Switch to cave ambient with crossfade
        AudioEventManager.PlayTrack(
            AudioTrackType.Ambient,
            -1,
            "CaveAmbient",
            0.6f,
            1f,
            1f,                          // Full spatial blend for cave
            FadeType.Crossfade,
            4f,
            FadeTarget.FadeBoth,
            true,
            0f,
            ambientLocationTransform,
            "Enter Cave"
        );
        
        AudioDebug.Log("Environmental Transition Complete!");
    }
    
    // ==================== CONVENIENCE WRAPPER METHODS ====================
    /// <summary>
    /// You can also create your own wrapper methods for specific game needs
    /// </summary>
    
    public void StartCombatMusic()
    {
        // Stop current BGM and start combat music with crossfade
        AudioEventManager.PlayTrack(AudioTrackType.BGM, -1, "CombatTheme", 0.8f, 1f, 0f, 
                                   FadeType.Crossfade, 2f, FadeTarget.FadeBoth, true, 0f, null, "Combat");
    }
    
    public void PlayFootstepSFX()
    {
        // Random footstep with slight pitch variation
        AudioEventManager.PlaySFX(new string[] { "Footstep1", "Footstep2", "Footstep3" }, 
                                 0.6f, 1f, true, 0.2f, 0.5f, false, 0f, 100f, 
                                 playerTransform, Vector3.zero, 1f, 10f, "Footstep");
    }
    
    public void DuckMusicForDialogue(float duckLevel = 0.2f)
    {
        // Duck background music for dialogue
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, duckLevel, 0.5f);
    }
    
    public void RestoreMusicAfterDialogue(float normalLevel = 0.8f)
    {
        // Restore music after dialogue
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, normalLevel, 1f);
    }
    
    // ==================== DEBUG METHODS ====================
    
    [ContextMenu("Test Direct Events")]
    public void TestDirectEvents()
    {
        DirectEventExamples();
    }
    
    [ContextMenu("Test Helper Methods")]
    public void TestHelperMethods()
    {
        HelperMethodExamples();
    }
    
    [ContextMenu("Test Complex Scenarios")]
    public void TestComplexScenarios()
    {
        ComplexScenarioExamples();
    }
}