# BMS Audio Manager v2.2.0

A Unity audio management system featuring 3-source crossfading, an event-driven architecture, spatial audio, and inspector-driven audio zones. Designed for small-to-medium projects requiring clean audio transitions and flexible sound management.

**Unity Version:** 6000.4.8f1+
**Optional Dependency:** `com.unity.splines` (required only for SplineFollower components)

---

## Documentation

- [Code API Reference](Usage-Code-API.md) - how to call audio from scripts
- [Inspector & Editor Usage](Usage-Editor-Inspector.md) - how to set up audio zones and components without code

---

## Key Features

- **3-Source Audio System** - Seamless crossfading and fade transitions with no audio gaps
- **5 Independent Tracks** - BGM, Ambient, Dialogue, Aux1, and Aux2 with separate state machines
- **Full Fade Control** - FadeInOut and Crossfade types; target volume, pitch, or both
- **SFX System** - Dynamic instantiation, looping, 3D positioning, pitch randomiation, probability-based playback
- **Spatial Audio** - Full 3D audio with transform attachment, distance attenuation, and world-position playback
- **Audio Mixer Routing** - Each track and SFX route to their own mixer group automatically
- **Event-Driven Architecture** - Three API tiers from one-liner helpers to full event control
- **Assignable Zone Actions** - Per-zone enter/exit actions (Play, Stop, Pause, Adjust) configurable in the Inspector
- **Spline Audio** - Audio sources that follow Unity Spline paths with sleep optimisation
- **Real-Time Parameter Control** - Adjust volume, pitch, and spatial blend during playback
- **Editor Tooling** - Custom inspector with live waveform, 3-source state visualisation, scene gizmos

---

## Quick Setup

### 1. Install

1. Import the BMS Audio Manager package into your Unity project
2. Place the **AudioManager prefab** in your scene (must be present for any audio to work)

### 2. Organise Audio Files

Audio clips are loaded from the Resources folder by name:

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
        ├── Aux1/
        │   └── StingerHit.wav
        ├── Aux2/
        │   └── UIMusic.wav
        └── SFX/
            └── ButtonClick.wav
```

### 3. Play Audio

```csharp
// Play background music
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");

// Play a sound effect
AudioEvent.PlaySFX("ButtonClick");

// Play ambient audio attached to a transform
AudioEvent.PlayTrack(AudioTrackType.Ambient, "ForestAmbient", 0.7f, 2f, forestTransform);
```

---

## API Reference

The system provides three tiers - use whichever fits your needs.

### Method 1: AudioEvent Helper Class (Recommended)

`AudioEvent` is a static class with overloaded convenience methods. Best for most use cases.

#### Track Playback

```csharp
// Play (multiple overloads)
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f);                     // with volume
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 2f);                 // with fade duration
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 2f, someTransform);  // with spatial attachment

// Stop
AudioEvent.StopTrack(AudioTrackType.BGM);
AudioEvent.StopTrack(AudioTrackType.BGM, 2f);                                     // with fade out
AudioEvent.StopTrack(AudioTrackType.BGM, 2f, FadeTarget.FadeVolume);

// Pause / Resume (toggle)
AudioEvent.PauseTrack(AudioTrackType.Ambient);
AudioEvent.PauseTrack(AudioTrackType.Ambient, 1f);                               // with fade

// Adjust parameters during playback
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f);
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 2f);                      // with fade
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.7f, 1.2f);                          // volume + pitch
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.3f, 1.5f, 2f);                      // with fade duration
```

#### Sound Effects

```csharp
// Basic
AudioEvent.PlaySFX("ButtonClick");
AudioEvent.PlaySFX("Explosion", 0.8f);

// Random from array
AudioEvent.PlaySFX(new string[] { "Footstep1", "Footstep2", "Footstep3" });

// With pitch variation
AudioEvent.PlaySFX("Hit", 0.9f, true);                                           // randomise pitch

// 3D positioned
AudioEvent.PlaySFX("MagicSpell", 0.8f, playerTransform);                         // attached to transform
AudioEvent.PlaySFX("Explosion", 1f, new Vector3(10, 0, 5));                       // world position

// 3D with custom distance settings
AudioEvent.PlaySFX3D("DistantThunder", 0.6f, thunderLocation, 5f, 100f);

// Looped SFX (returns GameObject reference for manual stopping)
AudioEvent.PlayLoopedSFX("EngineHum", 0.7f, carTransform);

// Random with delay and chance
AudioEvent.PlayRandomSFX(new string[] { "Bird1", "Bird2", "Bird3" }, 0.4f, 30f, 2f);
```

---

### Method 2: AudioEventManager Direct Events (Full Control)

For maximum parameter control, raise events directly via `AudioEventManager`.

#### Track with all parameters

```csharp
AudioEventManager.PlayTrack?.Invoke(
    AudioTrackType.BGM,       // Track type
    -1,                       // Track index (-1 = use name)
    "CombatTheme",            // Track name
    0.9f,                     // Volume
    1f,                       // Pitch
    0f,                       // Spatial blend (0 = 2D, 1 = 3D)
    FadeType.Crossfade,       // Fade type
    3f,                       // Fade duration (seconds)
    FadeTarget.FadeBoth,      // What to fade
    true,                     // Loop
    0f,                       // Delay before playing
    null,                     // Attach to transform
    "Combat Music"            // Event name (for debugging)
);
```

#### SFX with all parameters

```csharp
AudioEventManager.PlaySFX?.Invoke(
    new string[] { "Explosion1", "Explosion2" }, // Array - one selected at random
    0.8f,                     // Volume
    1f,                       // Pitch
    true,                     // Randomise pitch
    0.3f,                     // Pitch variation range
    1f,                       // Spatial blend
    false,                    // Loop
    0.5f,                     // Delay
    80f,                      // Percentage chance to play (0–100)
    explosionPoint,           // Attach to transform
    Vector3.zero,             // Custom world position (used if transform is null)
    2f,                       // Min distance
    50f,                      // Max distance
    "Explosion Effect"        // Event name
);
```

---

### Method 3: Inspector Components (No Code Required)

#### AudioEventSender - Track control via trigger/collision zones

Attach to any GameObject to play, stop, or pause a track when the player enters an area.

| Inspector Field | Description |
|---|---|
| Audio Track Type | BGM / Ambient / Dialogue |
| Track Name | Clip filename (without extension) |
| Fade Type | FadeInOut or Crossfade |
| Fade Duration / Target | Duration in seconds; what to fade |
| Volume / Pitch | Playback parameters |
| Spatial Blend | 0 = 2D, 1 = full 3D |
| Play On Enabled | Auto-play when the GameObject activates |
| Loop | Loop the track |
| Collision Type | Trigger or Collision |
| Target Tag | Only react to objects with this tag |
| On Enter Action | What to do when the target enters - `Play`, `Stop`, `Pause`, `AdjustParameters`, or `None` |
| On Exit Action | What to do when the target exits - same options as above |
| Event Delay | Seconds to wait before playing |
| Attach To This Transform | Audio follows this zone object |

**Public methods** (callable from scripts or UnityEvents):
```csharp
audioEventSender.Play();
audioEventSender.Stop();
audioEventSender.Pause();
audioEventSender.AdjustParameters();
```

**Inspector test keys:** `M` = Play, `N` = Stop, `B` = Pause, `V` = Adjust

---

#### AudioEventSenderSFX - SFX control via trigger/collision zones

| Inspector Field | Description |
|---|---|
| SFX Name (array) | One or more clip names - selected randomly |
| Play On Enabled | Auto-play on activate |
| Volume / Pitch | Playback parameters |
| Randomise Pitch / Pitch Range | Pitch variation |
| Spatial Blend | 0 = 2D, 1 = full 3D |
| Loop | Looped playback |
| Percentage Chance To Play | 0–100% probability |
| Randomise Delay / Event Delay | Delay control |
| Use Custom 3D Settings | Override min/max distances |
| Use Custom Position | Play at a fixed world position |
| Collision Type / Target Tag | Trigger or Collision zone setup |
| Attach Sound To This Transform | Sound follows zone object |

**Public methods:**
```csharp
sfxEventSender.Play();
sfxEventSender.Stop();   // Stops all active SFX
sfxEventSender.Pause();  // Toggles pause for all SFX
```

**Inspector test keys:** `T` = Play, `P` = Pause All, `S` = Stop All

---

## Track System

### Track Types

| Type | Use Case |
|---|---|
| `AudioTrackType.BGM` | Background music, combat themes, menus |
| `AudioTrackType.Ambient` | Environmental beds, room tone, weather |
| `AudioTrackType.Dialogue` | NPC speech, narration, cutscene audio |
| `AudioTrackType.Aux1` | General purpose auxiliary - stingers, secondary music layers, UI music |
| `AudioTrackType.Aux2` | General purpose auxiliary - additional concurrent track |

Each type is an independent track with its own state machine and 3-source system. All five can play simultaneously.

### State Machine

Each track moves through these states automatically:

| State | Description |
|---|---|
| `Stopped` | No audio playing |
| `Playing` | Normal playback at target volume |
| `Paused` | Audio paused |
| `FadingIn` | Volume/pitch transitioning up |
| `FadingOut` | Volume/pitch transitioning down |
| `Crossfading` | Old and new sources fading simultaneously |
| `AdjustingParameters` | Volume/pitch changing during playback |
| `FadeToPause` | Fading out before entering Paused state |
| `FadeFromPause` | Fading in when resuming from Paused |

### Fade Types

| Fade Type | Behaviour |
|---|---|
| `FadeType.FadeInOut` | Fade out current audio first, then fade in the new audio (sequential) |
| `FadeType.Crossfade` | Old and new audio overlap - old fades out while new fades in simultaneously |

### Fade Targets

| Fade Target | Behaviour |
|---|---|
| `FadeTarget.FadeVolume` | Only volume transitions; pitch snaps immediately |
| `FadeTarget.FadePitch` | Only pitch transitions; volume snaps immediately |
| `FadeTarget.FadeBoth` | Both volume and pitch transition together |
| `FadeTarget.Ignore` | Instant change - no fading |

---

## Audio Mixer Routing

The AudioManager has a **Mixer Groups** section in the Inspector with one slot per track type plus one for SFX. Assign your mixer groups there - each instantiated `AudioSource` automatically has its `outputAudioMixerGroup` set at runtime, regardless of what is baked into the shared prefab.

| Inspector Slot | Routes |
|---|---|
| BGM Mixer Group | All BGM track sources |
| Ambient Mixer Group | All Ambient track sources |
| Dialogue Mixer Group | All Dialogue track sources |
| Aux1 Mixer Group | All Aux1 track sources |
| Aux2 Mixer Group | All Aux2 track sources |
| SFX Mixer Group | All instantiated SFX sources |

Slots can be left empty - unassigned tracks route to the prefab's default output.

---

## SFX System

SFX clips are loaded from `Resources/Audio/SFX/` by name. Each `PlaySFX` call instantiates a temporary GameObject with an `AudioSource`, which auto-destroys when the clip finishes.

### Global SFX Controls

```csharp
AudioManager.Instance.StopAllSFX();
AudioManager.Instance.StopAllLoopedSFX();
AudioManager.Instance.TogglePauseAllSFX();
AudioManager.Instance.PauseAllSFX(true);       // explicit pause
AudioManager.Instance.CancelAllDelayedSFX();

// Query active SFX
int count = AudioManager.Instance.GetActiveSFXCount();
string[] names = AudioManager.Instance.GetActiveSFXNames();
bool paused = AudioManager.Instance.AllSFXPaused;

// Global volume multiplier (0–1)
AudioManager.Instance.GlobalSFXAttenuation = 0.5f;
```

---

## Spline Audio System

Requires the `com.unity.splines` package to be installed.

`SplineFollower` moves an AudioSource along a Unity Spline path tracking the closest point to a target Transform. Useful for river sounds, road traffic, or any audio source that should hug a path rather than emit from a fixed point.

**Key settings:**
- `proximityThreshold` - how close the target must be to activate
- `movementSpeed` - how fast the follower tracks along the spline
- `smoothing` - smoothing factor (0–1) for position transitions
- `sleepThreshold` / `sleepCheckInterval` - distance at which the follower enters sleep mode to save performance

`SimplifiedSplineFollower` is a lightweight version for simpler cases where proximity detection and inside/outside zone logic are not needed.

---

## Debugging & Monitoring

### AudioTrackParameterDisplay (Component)

Attach to any GameObject to see live BGM, Ambient, and Dialogue track states in the Inspector during play mode. Shows state, current clip, volume, pitch, and playback progress.

### SFXDebugDisplay (Component)

Shows active SFX count, queued (delayed) SFX count, and names of currently playing sounds. Provides context-menu controls:

- Stop All SFX
- Stop All Looped SFX
- Pause / Resume All SFX
- Cancel All Delayed SFX
- Log SFX Details to console

### AudioTrackEditor (Custom Inspector)

The `AudioTrack` component has a custom editor showing:

- Live state for Main / Cue / Outgoing sources (colour-coded: green / yellow / cyan)
- Volume and pitch sliders
- Playback progress bar
- Waveform preview for decompressed clips
- Scene view labels showing state above each audio source

### Console Logging

Toggle debug logging on the AudioManager component, or call the context-menu action:

```
AudioManager → (right-click) → Validate Audio Track Setup
```

The `AudioDebug` static class is used internally and can be used in your own extensions:

```csharp
AudioDebug.Log("message");
AudioDebug.LogWarning("message");
AudioDebug.LogCategory("BGM", "crossfade started");
```

---

## Real-World Examples

### Combat Music Transition

```csharp
public void StartCombat()
{
    AudioEvent.PlayTrack(AudioTrackType.BGM, "CombatTheme", 0.8f, 2f);
}

public void EndCombat()
{
    AudioEvent.PlayTrack(AudioTrackType.BGM, "ExplorationTheme", 0.7f, 3f);
}
```

### Environmental Audio Zone (Code)

```csharp
public class AudioZone : MonoBehaviour
{
    [SerializeField] private string ambientTrack = "CaveAmbient";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            AudioEvent.PlayTrack(AudioTrackType.Ambient, ambientTrack, 0.6f, 3f, transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            AudioEvent.StopTrack(AudioTrackType.Ambient, 2f);
    }
}
```

### Dialogue Ducking

```csharp
public void StartDialogue(string clip)
{
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.15f, 0.5f);
    AudioEvent.PlayTrack(AudioTrackType.Dialogue, clip, 1f, 0.3f);
}

public void EndDialogue()
{
    AudioEvent.StopTrack(AudioTrackType.Dialogue, 0.3f);
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);
}
```

### Randomised Weapon Sounds

```csharp
private string[] shotSounds = { "GunShot1", "GunShot2", "GunShot3" };

public void OnFire()
{
    AudioEventManager.PlaySFX?.Invoke(
        shotSounds, 0.9f, 1f, true, 0.1f, 0.8f,
        false, 0f, 100f, transform, Vector3.zero, 1f, 30f, "WeaponFire"
    );
}
```

---

## Audio Loading - Current Approach & Potential Improvements

### Current: Resources-Based Loading

Clips are loaded at runtime from `Resources/Audio/<type>/<name>` using `Resources.Load<AudioClip>()`. Loaded clips are cached in per-type dictionaries for subsequent calls.

**Limitations:**
- All clips in the `Resources` folder are included in the build regardless of whether they are played - this increases build size
- No async loading - first play of a clip can cause a brief hitch if the file is large
- Memory is held until explicitly unloaded

### Potential Improvement: Addressables

Unity's Addressables system loads assets on demand with full async support and control over memory lifecycle.

**Benefits over Resources:**
- Only content that is actually used gets bundled
- Async loading prevents hitches - clips load in the background before playback
- Fine-grained memory control: load per-scene, unload on scene exit
- Supports DLC and remote content delivery

**Integration approach:** Abstract the clip-loading layer behind an `IAudioClipProvider` interface. The existing Resources implementation becomes the default provider. An Addressables provider can be swapped in without changing any call sites. Only `GetBGMClip`, `GetAmbientClip`, `GetDialogueClip`, and the SFX lookup methods in `AudioManager.cs` need to change.

### Simpler Near-Term Alternative: AudioLibrary ScriptableObjects

Create `[CreateAssetMenu]` ScriptableObjects that hold explicit `AudioClip[]` references per category. This removes the Resources dependency entirely - clips become direct Inspector references, build stripping works normally, and there is no magic string lookup. A good fit for projects that don't need async loading or remote content.

---

## FMOD / Wwise Bridge

The event-driven architecture makes BMS Audio Manager well-positioned as a game-facing API that can route to a professional audio middleware backend.

### Architecture Approach

The game never calls FMOD or Wwise directly - it continues to call `AudioEvent.*` or raise `AudioEventManager` events. A bridge MonoBehaviour subscribes to those events and translates them to middleware calls:

```
Game Code
    ↓  AudioEvent.PlayTrack(BGM, "CombatTheme", ...)
AudioEventManager.PlayTrack event
    ↓
FMODBridge.cs (subscriber)
    ↓  FMODUnity.RuntimeManager.PlayOneShot("event:/Music/CombatTheme")
FMOD Studio Runtime
```

The bridge maps BMS track/clip names to middleware event paths via a serialised dictionary in the Inspector.

### What Maps Cleanly

| BMS Call | FMOD Equivalent | Wwise Equivalent |
|---|---|---|
| `PlayTrack` | `RuntimeManager.PlayOneShot` / `EventInstance.start()` | `AkSoundEngine.PostEvent` |
| `StopTrack` | `EventInstance.stop()` | `AkSoundEngine.PostEvent` (stop action) |
| `PlaySFX` | `RuntimeManager.PlayOneShot` | `AkSoundEngine.PostEvent` |

### What Needs Rethinking

`FadeType` and `FadeTarget` are BMS concepts - both FMOD and Wwise handle transitions internally through their own snapshot and transition systems. The bridge would pass fade duration as a hint, but the actual crossfade behaviour would be governed by middleware settings rather than BMS coroutines.

**Effort estimate:** A basic functional bridge is 2–3 days. Full parity (parameter mapping, RTPC integration, snapshot-based ducking) is closer to a week depending on the middleware setup.

---

## Best Practices

### Performance
- Use streaming load type for long ambient tracks (set in the AudioClip import settings)
- Avoid playing large numbers of simultaneous SFX - monitor count with `SFXDebugDisplay`
- Use the SplineFollower's `sleepThreshold` to disable distant spline followers

### Audio Organisation
- Use compressed formats (OGG Vorbis) for ambient and BGM tracks
- Keep individual SFX clips short and uncompressed (or ADPCM) for low-latency playback
- Name clips descriptively - they are referenced by string at runtime

### Fade Configuration
- Crossfade for musical transitions between similar-energy tracks
- FadeInOut for dramatic scene changes where silence between tracks is acceptable
- Keep fade durations between 0.5–3 seconds for natural feel
- Use `FadeTarget.FadeVolume` when pitch should snap immediately

---

## Known Limitations

- Only one clip per track type can play at a time (BGM, Ambient, Dialogue, Aux1, Aux2 - by design; use SFX for additional concurrent sounds)
- SFX clips are loaded synchronously on first play; consider pre-warming critical sounds
- `SplineFollower` requires `com.unity.splines` - the project will not compile if the package is absent and these scripts are included
- Mixer group slots on the AudioManager are optional - if left unassigned, audio sources route to whatever is set on the shared prefab

---

## License

This project is licensed under **CC BY-ND 4.0**.

Free to use in any project, including commercial. You may not redistribute modified versions of the package itself.

© 2026 Niall Mc Shane - [creativecommons.org/licenses/by-nd/4.0](https://creativecommons.org/licenses/by-nd/4.0/)

---

*BMS Audio Manager v2.2.0 - Unity 6*
