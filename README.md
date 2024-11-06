
---

# BMS Audio Manager

The **BMS Audio Manager** is a Unity-based audio management system that allows you to play, pause, and stop background music (BGM), ambient audio, and sound effects (SFX) using event-based triggers or direct method calls from your scripts.

---

## Installation

1. Copy the `BMS AudioManager` folder into your Unity project's `Assets` directory or import the Unity package.
2. Ensure all necessary scripts and audio files are placed in the appropriate directories.
3. Run the demo scene to check the functionality.

---

## Usage

### Using Event Senders via Triggers or Colliders

1. **Attach the `AudioEventSender_SFX` Script:**
   - Attach the `AudioEventSender_SFX` script to any GameObject in your scene.
   - Configure parameters in the Inspector, such as `sfxName`, `volume`, `pitch`, etc.

2. **Configure Triggers or Colliders:**
   - Set the `collisionType` to either `Trigger` or `Collision`.
   - Specify the `targetTag` to determine which objects can trigger the audio event.
   - Use the pre-made Event Sender prefabs with the script and collider attached.
   - The event will send audio parameters to the audio manager when triggered.

Alternatively, you can get the EventSender as a component and call the public methods for `Play()`, `Pause()`, and `Stop()`.

---

### Calling Audio Events from Code

#### Background Music (BGM)

- **Play BGM:**
  ```csharp
  AudioEventManager.PlayBGM(int index, string trackName, float volume, FadeType fadeType, float fadeDuration, bool loopBGM, string eventName);
  ```

- **Pause BGM:**
  ```csharp
  AudioEventManager.PauseBGM(float fadeDuration);
  ```

- **Stop BGM:**
  ```csharp
  AudioEventManager.StopBGM(float fadeDuration);
  ```

#### Ambient Audio

- **Play Ambient Audio:**
  ```csharp
  AudioEventManager.PlayAmbientAudio(Transform attachTo, int index, string trackName, float volume, float pitch, float spatialBlend, FadeType fadeType, float fadeDuration, bool loopAmbient, string eventName);
  ```

- **Pause Ambient Audio:**
  ```csharp
  AudioEventManager.PauseAmbientAudio(float fadeDuration);
  ```

- **Stop Ambient Audio:**
  ```csharp
  AudioEventManager.StopAmbientAudio(float fadeDuration);
  ```

#### Sound Effects (SFX)

- **Play SFX:**
  ```csharp
  AudioEventManager.PlaySFX(Transform attachTo, string soundName, float volume, float pitch, bool randomizePitch, float pitchRange, float spatialBlend, string eventName);
  ```

---

## Loading Audio from Resources

Ensure that your audio files are placed in a `Resources` folder within your `Assets` directory. The `AudioEventManager` will load these audio files at runtime.

Example directory structure:
```
Assets/
└── Resources/
    ├── BGM/
    │   └── TrackName.wav
    ├── Ambient/
    │   └── AmbientTrack.wav
    └── SFX/
        └── SFXName.wav
```

---

## Conclusion

The BMS Audio Manager provides a flexible and easy-to-use system for managing audio in your Unity projects. Whether you use event-based triggers or call audio events directly from code, this system allows for seamless integration and control over your game's audio.

---

