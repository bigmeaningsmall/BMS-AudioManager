
# BMS Audio Manager

## Overview

The BMS Audio Manager is a Unity-based audio management system that allows you to play, pause, and stop background music (BGM), ambient audio, and sound effects (SFX) using event-based triggers or direct method calls from your scripts.

## Installation

1. Copy the `BMS AudioManager` folder into your Unity project's `Assets` directory.
2. Ensure that all necessary scripts and audio files are placed in the appropriate directories.

## Usage

### Using Event Senders via Triggers or Colliders

1. **Attach the `AudioEventSender_SFX` Script:**
   - Attach the `AudioEventSender_SFX` script to any GameObject in your scene.
   - Configure the parameters in the Inspector, such as `sfxName`, `volume`, `pitch`, etc.

2. **Configure Triggers or Colliders:**
   - Set the `collisionType` to either `Trigger` or `Collision`.
   - Specify the `targetTag` to determine which objects can trigger the audio event.

3. **Example:**
   ```csharp
   // Attach this script to a GameObject with a Collider component
   public class ExampleTrigger : MonoBehaviour
   {
       private void OnTriggerEnter(Collider other)
       {
           if (other.CompareTag("Player"))
           {
               // The AudioEventSender_SFX script will handle the audio event
           }
       }
   }
   ```

### Calling Audio Events from Code

#### Background Music (BGM)

- **Play BGM:**
  ```csharp
  AudioEventManager.PlayBGM?.Invoke(0, "TrackName", 1.0f, FadeType.FadeInOut, 2.0f, true, "BGM Event");
  ```

- **Pause BGM:**
  ```csharp
  AudioEventManager.PauseBGM?.Invoke(2.0f);
  ```

- **Stop BGM:**
  ```csharp
  AudioEventManager.StopBGM?.Invoke(2.0f);
  ```

#### Ambient Audio

- **Play Ambient Audio:**
  ```csharp
  AudioEventManager.PlayAmbientAudio?.Invoke(null, 0, "AmbientTrack", 1.0f, 1.0f, 0.5f, FadeType.FadeInOut, 2.0f, true, "Ambient Event");
  ```

- **Pause Ambient Audio:**
  ```csharp
  AudioEventManager.PauseAmbientAudio?.Invoke(2.0f);
  ```

- **Stop Ambient Audio:**
  ```csharp
  AudioEventManager.StopAmbientAudio?.Invoke(2.0f);
  ```

#### Sound Effects (SFX)

- **Play SFX:**
  ```csharp
  AudioEventManager.PlaySFX?.Invoke(null, "SFXName", 1.0f, 1.0f, true, 0.1f, 0.5f, "SFX Event");
  ```

## Loading Audio from Resources

Ensure that your audio files are placed in a `Resources` folder within your `Assets` directory. The `AudioEventManager` will load these audio files at runtime.

Example directory structure:
```
Assets/
└── Resources/
    ├── BGM/
    │   └── TrackName.mp3
    ├── Ambient/
    │   └── AmbientTrack.mp3
    └── SFX/
        └── SFXName.mp3
```

## Conclusion

The BMS Audio Manager provides a flexible and easy-to-use system for managing audio in your Unity projects. Whether you are using event-based triggers or calling audio events directly from your code, this system allows for seamless integration and control over your game's audio experience.
