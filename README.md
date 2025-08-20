# BMS Audio Manager v1.4

A comprehensive Unity audio management system featuring sophisticated track management, 3-source crossfading, and flexible event-driven architecture. Best used in small projects requiring professional-quality audio transitions and sound management.

## Key Features

- **3-Source Audio System**: Seamless crossfading and fade transitions without audio gaps
- **Track-Based Management**: Separate BGM, Ambient, and Dialogue tracks with individual control
- **Spatial Audio Support**: Full 3D audio with distance attenuation and positioning
- **Event-Driven Architecture**: Flexible triggering via events, triggers, colliders, or direct code calls
- **Advanced Fade Control**: Multiple fade types (FadeInOut, Crossfade) with customizable targets
- **SFX Management**: Comprehensive sound effects system with pooling and state management
- **Real-Time Parameter Control**: Adjust volume, pitch, and spatial blend during playback
- **Visual Debugging**: Custom editor tools for monitoring audio states and waveforms

---

## Quick Setup

### 1. Installation
1. Import the BMS Audio Manager package into your Unity project
2. Place the AudioManager prefab in your scene
3. Ensure your audio files are organized in the Resources folder structure:

```
Assets/
└── Resources/
    └── Audio/
        ├── BGM/
        │   └── MainTheme.wav
        ├── Ambient/
        │   └── ForestAmbient.wav
        ├── Dialogue/
        │   └── NPCDialogue.wav
        └── SFX/
            └── ButtonClick.wav
```

### 2. Basic Usage
The simplest way to play audio:

```csharp
// Play background music
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");

// Play a sound effect
AudioEvent.PlaySFX("ButtonClick");

// Play ambient audio at a location
AudioEvent.PlayTrack(AudioTrackType.Ambient, "ForestAmbient", 0.7f, 2f, forestTransform);
```

---

## Usage Guide

### Method 1: Helper Methods (Recommended for Most Use Cases)

The `AudioEvent` static class provides easy-to-use methods with sensible defaults:

#### Track Management
```csharp
// Simple track playback
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f); // with volume
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 2f); // with fade duration

// Stop and pause
AudioEvent.StopTrack(AudioTrackType.BGM);
AudioEvent.StopTrack(AudioTrackType.BGM, 2f); // with fade out
AudioEvent.PauseTrack(AudioTrackType.Ambient);

// Adjust parameters
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f);
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 2f); // with fade
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.7f, 1.2f); // volume and pitch
```

#### Sound Effects
```csharp
// Basic SFX
AudioEvent.PlaySFX("ButtonClick");
AudioEvent.PlaySFX("Explosion", 0.8f);

// Random selection from multiple sounds
AudioEvent.PlaySFX(new string[] { "Footstep1", "Footstep2", "Footstep3" });

// 3D positioned audio
AudioEvent.PlaySFX("MagicSpell", 0.8f, playerTransform); // attached to transform
AudioEvent.PlaySFX("Explosion", 1f, new Vector3(10, 0, 5)); // world position

// Advanced SFX
AudioEvent.PlaySFX3D("DistantThunder", 0.6f, thunderLocation, 5f, 100f); // with distance settings
AudioEvent.PlayLoopedSFX("EngineHum", 0.7f, carTransform);
AudioEvent.PlayRandomSFX(new string[] { "Bird1", "Bird2", "Bird3" }, 0.4f, 30f, 2f); // with chance and delay
```

### Method 2: Direct Event Calls (Full Control)

For maximum control over all parameters, use `AudioEventManager` directly:

#### Background Music with Crossfade
```csharp
AudioEventManager.playTrack(
    AudioTrackType.BGM,           // Track type
    -1,                          // Track index (-1 to use name)
    "CombatTheme",               // Track name
    0.9f,                        // Volume
    1f,                          // Pitch
    0f,                          // Spatial blend (0 = 2D)
    FadeType.Crossfade,          // Fade type
    3f,                          // Fade duration
    FadeTarget.FadeBoth,         // Fade target (volume and pitch)
    true,                        // Loop
    0f,                          // Delay
    null,                        // Attach to transform
    "Combat Music"               // Event name
);
```

#### Advanced SFX with All Parameters
```csharp
AudioEventManager.PlaySFX(
    new string[] { "Explosion1", "Explosion2" }, // Sound name array (random selection)
    0.8f,                        // Volume
    1f,                          // Pitch
    true,                        // Randomize pitch
    0.3f,                        // Pitch range
    1f,                          // Spatial blend (1 = 3D)
    false,                       // Loop
    0.5f,                        // Delay
    80f,                         // Percentage chance to play
    explosionPoint,              // Attach to transform
    Vector3.zero,                // Custom position (if not using transform)
    2f,                          // Min distance
    50f,                         // Max distance
    "Explosion Effect"           // Event name
);
```

### Method 3: Visual Event Senders (No Code Required)

Use the provided MonoBehaviour components for trigger-based audio:

#### AudioEventSender (for Tracks)
Attach to GameObjects for trigger-based track control:

- **Collision/Trigger Support**: Automatically plays when player enters area
- **Visual Debugging**: Shows trigger zones and activation states in Scene view
- **Inspector Configuration**: All parameters exposed for easy tweaking

```csharp
// Available public methods for script/button integration:
audioEventSender.Play();
audioEventSender.Stop();
audioEventSender.Pause();
audioEventSender.AdjustParameters();
```

#### AudioEventSenderSFX
Perfect for environmental sounds and interactive elements:

- **Multiple Sound Support**: Random selection from sound arrays
- **3D Audio Configuration**: Custom min/max distances and positioning
- **Probability System**: Percentage chance for sound variation
- **Transform Attachment**: Follow moving objects or play at fixed positions

```csharp
// Public methods available:
sfxEventSender.Play();
sfxEventSender.Stop();  // Stops all SFX
sfxEventSender.Pause(); // Toggles pause state
```

---

## 🎛Advanced Features

### Fade Types and Targets

#### Fade Types
- **`FadeInOut`**: Fade out current audio, then fade in new audio
- **`Crossfade`**: Simultaneously fade out old and fade in new audio

#### Fade Targets
- **`FadeVolume`**: Only fade volume (pitch changes instantly)
- **`FadePitch`**: Only fade pitch (volume changes instantly)
- **`FadeBoth`**: Fade both volume and pitch
- **`Ignore`**: No fading (instant change)

### Real-Time Parameter Adjustment

```csharp
// Adjust track parameters during playback
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.3f, 1.5f, 2f); // Duck volume, increase pitch

// Direct parameter control
AudioEventManager.adjustTrack(
    AudioTrackType.Ambient,
    0.4f,                    // New volume
    0.8f,                    // New pitch
    1f,                      // Spatial blend
    1.5f,                    // Fade duration
    FadeTarget.FadeBoth,     // Fade target
    true,                    // Loop
    0f,                      // Delay
    newLocation,             // New transform
    "Environment Change"     // Event name
);
```

### SFX Management

```csharp
// Global SFX control
AudioManager.Instance.StopAllSFX();
AudioManager.Instance.StopAllLoopedSFX();
AudioManager.Instance.TogglePauseAllSFX();
AudioManager.Instance.CancelAllDelayedSFX();

// Get SFX information
int activeSFXCount = AudioManager.Instance.GetActiveSFXCount();
string[] activeSFXNames = AudioManager.Instance.GetActiveSFXNames();
bool isPaused = AudioManager.Instance.AllSFXPaused;
```

---

## Real-World Examples

### Combat Music Transition
```csharp
public void StartCombatMusic()
{
    // Crossfade from exploration to combat music
    AudioEventManager.playTrack(
        AudioTrackType.BGM, -1, "CombatTheme", 
        0.8f, 1f, 0f, FadeType.Crossfade, 
        2f, FadeTarget.FadeBoth, true, 0f, null, "Combat Start"
    );
}
```

### Environmental Audio Zone
```csharp
public class EnvironmentalAudioZone : MonoBehaviour
{
    [SerializeField] private string ambientTrackName = "CaveAmbient";
    [SerializeField] private float volume = 0.6f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Crossfade to zone ambient
            AudioEvent.PlayTrack(AudioTrackType.Ambient, ambientTrackName, volume, 3f, transform);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioEvent.StopTrack(AudioTrackType.Ambient, 2f);
        }
    }
}
```

### Dialogue System Integration
```csharp
public class DialogueManager : MonoBehaviour
{
    public void StartDialogue(string dialogueTrack)
    {
        // Duck background music for dialogue
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.2f, 0.5f);
        
        // Play dialogue
        AudioEvent.PlayTrack(AudioTrackType.Dialogue, dialogueTrack, 1f, 0.3f);
    }
    
    public void EndDialogue()
    {
        // Stop dialogue and restore BGM volume
        AudioEvent.StopTrack(AudioTrackType.Dialogue, 0.3f);
        AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);
    }
}
```

### Interactive Sound Effects
```csharp
public class WeaponSounds : MonoBehaviour
{
    [SerializeField] private string[] fireSound = { "GunShot1", "GunShot2", "GunShot3" };
    [SerializeField] private string reloadSound = "Reload";
    
    public void OnFire()
    {
        // Random gunshot with pitch variation
        AudioEventManager.PlaySFX(
            fireSound, 0.9f, 1f, true, 0.1f, 0.8f, false, 0f, 100f,
            transform, Vector3.zero, 1f, 30f, "Weapon Fire"
        );
    }
    
    public void OnReload()
    {
        AudioEvent.PlaySFX(reloadSound, 0.7f, transform);
    }
}
```

---

## Debugging and Monitoring

### Built-in Debug Components

#### AudioTrackParameterDisplay
Attach to any GameObject to monitor all track states in real-time:
- Live parameter updates
- Track state visualization
- Performance monitoring

#### SFXDebugDisplay
Monitor SFX system health and activity:
- Active SFX count and names
- Delayed SFX tracking
- One-click SFX management controls

### Custom Editor Tools

The system includes custom editors with:
- **Real-time waveform visualization**
- **3-source state monitoring**
- **Volume/pitch sliders**
- **Playback progress bars**
- **Scene view audio source visualization**

### Console Debugging

Enable detailed logging by checking AudioManager debug options:
```csharp
[ContextMenu("Validate Audio Track Setup")]
public void ValidateSetup() // Available in AudioManager context menu
```

---

## Best Practices

### Performance Optimization
- Use streaming audio for long ambient tracks
- Set appropriate AudioClip load types based on usage
- Monitor active SFX count to prevent audio overflow
- Use object pooling for frequently played sounds

### Audio Organization
- Keep BGM tracks under 2MB for memory efficiency
- Use compressed formats (OGG) for ambient audio
- Organize SFX by category in subfolders
- Name audio files descriptively for easy identification

### Fade Configuration
- Use crossfades for musical transitions
- Use FadeInOut for dramatic scene changes
- Keep fade durations between 0.5-3 seconds for natural feel
- Match fade targets to the type of transition needed

---

## Migration from v1.2

If upgrading from the previous version:

1. **AudioEventManager Changes**: Method signatures now include additional parameters
2. **New Helper Methods**: Use `AudioEvent` class for simplified calls
3. **Track System**: BGM and Ambient are now unified under track types
4. **SFX Enhancements**: New parameters for 3D audio and randomization
5. **Component Updates**: EventSender components have new configuration options

---

## 🤝 Contributing

This is an ongoing work in progress. The system is designed to be modular and extensible. Feel free to contribute improvements or report issues.

## 📄 License

Open source - feel free to use and modify for your projects.

---

*BMS Audio Manager v1.4 - Professional audio management for Unity games*
