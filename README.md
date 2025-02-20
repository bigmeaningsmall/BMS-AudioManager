
---

# BMS Audio Manager

The BMS Audio Manager is a Unity-based audio management system that allows you to play, pause, and stop background music (BGM), ambient audio, and sound effects (SFX) using event-based triggers or direct method calls from your scripts.

## Installation

1. Copy the `BMS AudioManager` folder into your Unity project's `Assets` directory or import the Unity package.
2. Ensure all necessary scripts and audio files are placed in the appropriate directories.
3. Run the demo scene to check the functionality.

---

### Loading Audio from Resources

Ensure that your audio files are placed in a `Resources` folder within your `Assets` directory. The `AudioManager` will make these audio files available in catagory pools at runtime.

**Directory structure**:

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

Note - Files in any sub folders will be available in the `AudioManager` and can be organised in folder names or catagories in your projects assets `Resources` folder.

---

### Calling Audio Events from Code

#### Background Music (BGM)

1. **Play BGM**:
   ```csharp
   AudioEventManager.PlayBGM(index, trackName, volume, fadeType, fadeDuration, loopBGM, eventName);
   ```

   **Parameters**:
   - **`index` (int)**: Track index for playlist management.
   - **`trackName` (string)**: Name of the BGM file in `Resources/BGM`.
   - **`volume` (float)**: Initial volume (1.0 for full volume).
   - **`fadeType` (FadeType)**: Type of fade (`FadeInOut` or `Crossfade`).
   - **`fadeDuration` (float)**: Duration of the fade effect.
   - **`loopBGM` (bool)**: Set to `true` to loop the track.
   - **`eventName` (string)**: Identifier for tracking this music event.

2. **Pause BGM**:
   ```csharp
   AudioEventManager.PauseBGM(fadeDuration);
   ```
   
   **Parameters**:
   - **`fadeDuration` (float)**: Duration of fade-out before pausing.

3. **Stop BGM**:
   ```csharp
   AudioEventManager.StopBGM(fadeDuration);
   ```

   **Parameters**:
   - **`fadeDuration` (float)**: Duration of fade-out before stopping.

---

#### Ambient Audio

1. **Play Ambient Audio**:
   ```csharp
   AudioEventManager.PlayAmbientAudio(attachTo, index, trackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopAmbient, eventName);
   ```

   **Parameters**:
   - **`attachTo` (Transform)**: The transform where the audio source will be positioned.
   - **`index` (int)**: Track index for playlist management.
   - **`trackName` (string)**: Name of the ambient audio file in `Resources/Ambient`.
   - **`volume` (float)**: Volume level (1.0 for full volume).
   - **`pitch` (float)**: Pitch (1.0 for normal pitch).
   - **`spatialBlend` (float)**: Controls the 2D/3D blend (0 for 2D, 1 for 3D).
   - **`fadeType` (FadeType)**: Type of fade (`FadeInOut` or `Crossfade`).
   - **`fadeDuration` (float)**: Duration of the fade effect.
   - **`loopAmbient` (bool)**: Set to `true` to loop the ambient audio.
   - **`eventName` (string)**: Identifier for tracking this ambient audio event.

2. **Pause Ambient Audio**:
   ```csharp
   AudioEventManager.PauseAmbientAudio(fadeDuration);
   ```
   
   **Parameters**:
   - **`fadeDuration` (float)**: Duration of fade-out before pausing.

3. **Stop Ambient Audio**:
   ```csharp
   AudioEventManager.StopAmbientAudio(fadeDuration);
   ```

   **Parameters**:
   - **`fadeDuration` (float)**: Duration of fade-out before stopping.

---

#### Sound Effects (SFX)

1. **Play SFX**:
   ```csharp
   AudioEventManager.PlaySFX(attachTo, soundName, volume, pitch, randomizePitch, pitchRange, spatialBlend, eventName);
   ```

   **Parameters**:
   - **`attachTo` (Transform)**: If provided, positions the sound source at this transform.
   - **`soundName` (string)**: Name of the sound file in `Resources/SFX`.
   - **`volume` (float)**: Volume level (1.0 for full volume).
   - **`pitch` (float)**: Pitch (1.0 for normal pitch).
   - **`randomizePitch` (bool)**: If `true`, adds slight pitch variation.
   - **`pitchRange` (float)**: Range for pitch randomisation.
   - **`spatialBlend` (float)**: Controls the 2D/3D blend (0 for 2D, 1 for 3D).
   - **`eventName` (string)**: Identifier for tracking this SFX event.


---

## Usage - Audio Event Senders


1. **Inspector-Exposed Events**: Audio event classes, such as `AudioEventSender_SFX`, `AudioEventSender_BGM`, and `AudioEventSender_Ambient`, expose public events that can be viewed and configured directly in the Unity Inspector. This setup allows for easy event linking without additional code.

2. **Flexible Setup Options**: By exposing these events, you can connect audio triggering to various sources, including triggers, colliders, and UI buttons, or even integrate them with custom scripts. This flexibility supports both simple and complex audio setups tailored to your game’s needs.

3. **Component-Based Audio Control**: Attaching these audio event classes as components enables your GameObjects to handle audio directly. With this approach, you can configure and control sound playback through component properties, making it easy to adjust audio settings in specific game areas or events.

4. **Dynamic Interaction with Unity Events**: The exposed public events can be linked to Unity Events (like `OnClick` for buttons or `OnTriggerEnter` for colliders), allowing seamless integration with Unity’s event system for intuitive and responsive audio feedback.

5. **Modular and Reusable**: Each audio event class serves as a self-contained audio trigger, making it modular and reusable across different GameObjects and scenes. This design reduces the need for repeated code and enables consistent audio handling throughout your project. 

Using these inspector-exposed audio event classes, you can set up robust audio interactions that are easy to configure, adjust, and link to other game events without additional coding. This design is ideal for both designers and developers who want streamlined control over audio in Unity.


### Using Event Senders via Triggers, Colliders, or Direct Calls

#### `AudioEventSender_SFX`

The `AudioEventSender_SFX` script allows you to trigger sound effects using various methods:

1. **Attaching to GameObjects with Triggers or Colliders**:
   - Attach the `AudioEventSender_SFX` script to any GameObject in your scene.
   - Configure parameters in the Inspector, such as `sfxName`, `volume`, `pitch`, etc.
   - Set the `collisionType` to either `Trigger` or `Collision`.
   - Specify the `targetTag` to determine which objects can trigger the audio event.

   This setup allows sound effects to play automatically when an object enters the trigger or collider.

2. **Calling Directly in Code**:
   - `AudioEventSender_SFX` can be called directly in your scripts by getting the component and using its public `Play()` method.
   - Example:
     ```csharp
     AudioEventSender_SFX sfxSender = GetComponent<AudioEventSender_SFX>();
     sfxSender.Play();
     ```

3. **Using Unity Events (e.g., Buttons)**:
   - You can use Unity Events (such as UI buttons) to trigger the `Play()` method of `AudioEventSender_SFX`.
   - In the Unity Inspector, simply link the button’s OnClick event to the `Play()` method of the `AudioEventSender_SFX` component on the target GameObject.

---

### `AudioEventSender_BGM`

The `AudioEventSender_BGM` script is designed to manage background music with the following public methods:

1. **Play BGM**: Starts playing the specified BGM track.
   ```csharp
   AudioEventSender_BGM bgmSender = GetComponent<AudioEventSender_BGM>();
   bgmSender.Play();
   ```

2. **Pause BGM**: Pauses the currently playing BGM track.
   ```csharp
   bgmSender.Pause();
   ```

3. **Stop BGM**: Stops the BGM playback entirely.
   ```csharp
   bgmSender.Stop();
   ```

These methods can also be triggered through Unity Events like buttons, allowing for easy UI integration.

---

### `AudioEventSender_Ambient`

The `AudioEventSender_Ambient` script controls ambient audio with similar functionality:

1. **Play Ambient Audio**:
   ```csharp
   AudioEventSender_Ambient ambientSender = GetComponent<AudioEventSender_Ambient>();
   ambientSender.Play();
   ```

2. **Pause Ambient Audio**:
   ```csharp
   ambientSender.Pause();
   ```

3. **Stop Ambient Audio**:
   ```csharp
   ambientSender.Stop();
   ```

These methods, like `AudioEventSender_SFX`, can also be assigned to Unity Events, such as UI buttons, for easy control over ambient audio from your game’s interface.

---

With these components, you have flexible options for triggering audio in your game. You can use triggers, colliders, code calls, or Unity Events to play, pause, and stop audio, giving you precise control over when and how audio elements are managed within your scenes.


## Conclusion

The BMS Audio Manager provides a flexible and easy-to-use system for managing audio in your Unity projects. Whether you use event-based triggers or call audio events directly from code, this system allows for seamless integration and precise control over your game's audio.


# UML

![BMS-AudioManager-UML](https://github.com/user-attachments/assets/cf7f9d82-f85a-4596-926a-d7ab2ef794ae)

