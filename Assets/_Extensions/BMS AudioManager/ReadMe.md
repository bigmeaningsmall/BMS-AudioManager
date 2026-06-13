# BMS Audio Manager v3.0.0

A Unity audio management system featuring 3-source crossfading, an event-driven architecture, spatial audio, and inspector-driven audio zones. Audio is organised through **asset-based SoundDefinitions** grouped into loadable **SoundBanks**, and addressed in code by a generated, compile-safe **`SoundId`** enum — no magic strings, no `Resources` dependency. Designed for small-to-medium projects requiring clean audio transitions and flexible sound management.

**Unity Version:** 6000.4.8f1+
**Optional Dependency:** `com.unity.splines` (required only for SplineFollower components)

> **v3.0.0 is a major, breaking release.** Clip loading moved off `Resources` strings onto SoundDefinition assets + a typed `SoundId` API. See [CHANGELOG](CHANGELOG.md) for the migration summary.

---

## Documentation

- [Code API Reference](Usage-Code-API.md) - how to call audio from scripts
- [Inspector & Editor Usage](Usage-Editor-Inspector.md) - how to set up audio zones and components without code
- [Changelog](CHANGELOG.md) - version history and migration notes

---

## Key Features

- **Asset-Based Sound Library** - `SoundDefinition` assets wrap each clip with its default volume/pitch/fade/3D/variation settings; one configured definition plays from a single call
- **Typed, String-Free API** - a generated `SoundId` enum gives compile-safe, autocompleting sound keys; no magic strings, no `Resources` folder dependency
- **Loadable Sound Banks** - group definitions into `SoundBank` assets, loaded globally at startup or per-scene (ref-counted) for availability control
- **One-Click Generation** - the editor generator mirrors a (configurable) audio folder into definitions, banks, and the `SoundId` enum; idempotent and rename-safe (GUID-matched)
- **3-Source Audio System** - Seamless crossfading and fade transitions with no audio gaps
- **5 Independent Tracks** - BGM, Ambient, Dialogue, Aux1, and Aux2 with separate state machines
- **Full Fade Control** - FadeInOut and Crossfade types; target volume, pitch, or both
- **SFX System** - Dynamic instantiation, looping, 3D positioning, pitch/volume randomisation, probability-based playback - all driven by the definition
- **Spatial Audio** - Full 3D audio with transform attachment, distance attenuation, and world-position playback (tracks and SFX)
- **Audio Mixer Routing** - Each track and SFX route to their own mixer group automatically
- **Event-Driven Architecture** - Three API tiers from one-liner helpers to full event control
- **Assignable Zone Actions** - Per-zone enter/exit actions (Play, Stop, Pause, Adjust) configurable in the Inspector
- **Spline Audio** - Audio sources that follow Unity Spline paths with sleep optimisation
- **Real-Time Parameter Control** - Adjust volume, pitch, and spatial blend during playback
- **Editor Tooling** - Custom inspector with live waveform, 3-source state visualisation, scene gizmos

---

## Core Concepts

The system is built from five pieces. Understanding them is enough to use everything else.

| Concept | What it is |
|---|---|
| **SoundDefinition** | A ScriptableObject "sound asset" — wraps an `AudioClip` (+ optional `variations`) and its default playback parameters (volume, pitch, loop, spatial blend, fade, SFX pitch/volume jitter, chance, delay, 3D distances). The single source of truth for *how a sound plays*. |
| **SoundBank** | A named list of SoundDefinitions and the **unit of loading**. The generator emits one bank per category plus a `MasterBank` of everything. |
| **AudioRegistry** | The runtime catalogue of *currently loaded* definitions. Banks are loaded/unloaded into it with **ref-counting**, so a definition shared by two loaded banks survives until the last one unloads. |
| **SoundId** | A generated `enum` (one member per definition, stable id values) — the **string-free key** you use in code and inspector dropdowns. Resolved through the registry, so a sound only plays when its bank is loaded. |
| **Senders / AudioEvent** | Two ways to trigger sounds: inspector **sender components** (trigger/collision zones) and the static **`AudioEvent`** code API. Both raise the same events that the `AudioManager` routes to the track/SFX system. |

**The flow:** author clips → **Generate Sound Definitions** (builds definitions + banks + `SoundId`) → load a bank (startup or per-scene) → play by `SoundId` / `SoundDefinition` from code or a sender.

---

## Quick Setup

### 1. Install

1. Import the BMS Audio Manager package into your Unity project
2. Place the **AudioManager prefab** in your scene (must be present for any audio to work)

### 2. Organise Audio Files

Put your clips in a folder with one subfolder per category (the subfolder name sets the category).
The generator scans this folder; **it does NOT need to be under `Resources`** — and shouldn't be,
since runtime loads via SoundDefinitions, not `Resources` (anything in a `Resources` folder is bundled
into every build, defeating dependency stripping). The scan path is set on the **SoundGeneratorSettings**
asset (auto-created in the package root on first generate); its `audioSourceRoot` defaults to
`Assets/Audio`. Point it wherever your clips live.

```
<your audio root - configurable>/
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
├── Ambience-Environment/
└── SFX/
    └── ButtonClick.wav
```

> **Source clips are edit-time only.** They're consumed by the generator to build SoundDefinitions
> (which reference the clips by GUID). Keep them anywhere under `Assets/` outside `Resources`.

### 3. Play Audio

> **Bank-only workflow.** Run **BMS AudioManager → Generate Sound Definitions** to build the
> SoundDefinition assets, SoundBanks, and the typed `SoundId` enum. Load a bank (assign **MasterBank**
> to `AudioManager.startupBanks`, or add a `SceneAudioBank`), then address sounds by `SoundId`
> (typed key) or a `SoundDefinition` reference. The old string/index API has been removed.

```csharp
[SerializeField] private SoundId mainTheme;       // inspector dropdown of generated ids
[SerializeField] private SoundId buttonClick;
[SerializeField] private SoundId forestAmbient;

// Play background music (channel + defaults come from the definition)
AudioEvent.PlayTrack(mainTheme);

// Play a sound effect (Play() auto-routes track vs SFX by category)
AudioEvent.Play(buttonClick);

// Play ambient audio attached to a transform
AudioEvent.PlayTrack(forestAmbient, forestTransform);
```

---

## API Reference

The system provides three tiers - use whichever fits your needs.

### Method 1: AudioEvent Helper Class (Recommended)

`AudioEvent` is a static class with overloaded convenience methods. Best for most use cases.

#### Track Playback

Track playback takes a `SoundId` or `SoundDefinition` (the channel + defaults are intrinsic to it).
Stop/Pause/Adjust take an `AudioTrackType` because they act on a channel, not a clip.

```csharp
[SerializeField] private SoundId mainTheme;

// Play (clip identity via SoundId / SoundDefinition)
AudioEvent.PlayTrack(mainTheme);                  // definition defaults
AudioEvent.PlayTrack(mainTheme, 0.8f);            // volume override
AudioEvent.PlayTrack(mainTheme, someTransform);   // spatial attachment

// Stop (channel-based)
AudioEvent.StopTrack(AudioTrackType.BGM);
AudioEvent.StopTrack(AudioTrackType.BGM, 2f);                                     // with fade out
AudioEvent.StopTrack(AudioTrackType.BGM, 2f, FadeTarget.FadeVolume);
AudioEvent.StopTrack(mainTheme);                                                  // or stop the channel a definition uses

// Pause / Resume (toggle, channel-based)
AudioEvent.PauseTrack(AudioTrackType.Ambient);
AudioEvent.PauseTrack(AudioTrackType.Ambient, 1f);                               // with fade

// Adjust parameters during playback (channel-based)
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f);
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.5f, 2f);                      // with fade
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.7f, 1.2f);                          // volume + pitch
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.3f, 1.5f, 2f);                      // with fade duration
```

#### Sound Effects

```csharp
[SerializeField] private SoundId explosion;
[SerializeField] private SoundDefinition footsteps;  // a def with variations = built-in random pool

// Basic
AudioEvent.PlaySFX(explosion);
AudioEvent.PlaySFX(explosion, 0.8f);                                             // volume override

// 3D positioned
AudioEvent.PlaySFX(explosion, playerTransform);                                  // attached to transform
AudioEvent.PlaySFX(explosion, new Vector3(10, 0, 5));                            // world position

// 3D with custom distance settings
AudioEvent.PlaySFX3D(explosion, thunderLocation, 5f, 100f);

// Looped SFX (stop via AudioManager.StopAllLoopedSFX / StopAllSFX)
AudioEvent.PlayLoopedSFX(explosion, carTransform);

// Random variation is automatic when the SoundDefinition has variations.
// Pitch/volume jitter, percent-chance, delay, and 3D distance also come from the
// definition (SFX Variation & 3D section) - so a configured gun/footstep def plays
// with full variety from just PlaySFX(def), no per-call parameters.
AudioEvent.PlaySFX(footsteps, playerTransform);
```

---

### Method 2: AudioEventManager Direct Events (Full Control)

For maximum parameter control, raise events directly via `AudioEventManager`. The clip is supplied
through the trailing **`directClip` / `directClips`** argument (from a SoundDefinition). The legacy
`trackName` / `trackNumber` / `soundNames` parameters are **vestigial - ignored** at runtime; pass
`-1` / `""` / `null`.

#### Track with all parameters

```csharp
[SerializeField] private SoundDefinition combatDef;

AudioEventManager.PlayTrack?.Invoke(
    combatDef.TrackType,      // Track type (channel)
    -1,                       // Track index - ignored
    "",                       // Track name - ignored
    0.9f,                     // Volume
    1f,                       // Pitch
    0f,                       // Spatial blend (0 = 2D, 1 = 3D)
    FadeType.Crossfade,       // Fade type
    3f,                       // Fade duration (seconds)
    FadeTarget.FadeBoth,      // What to fade
    true,                     // Loop
    0f,                       // Delay before playing
    null,                     // Attach to transform
    "Combat Music",           // Event name (for debugging)
    combatDef.GetClip()       // directClip - REQUIRED (the actual clip)
);
```

#### SFX with all parameters

```csharp
[SerializeField] private SoundDefinition explosionDef; // clip + variations = random pool

AudioEventManager.PlaySFX?.Invoke(
    null,                     // soundNames - ignored
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
    "Explosion Effect",       // Event name
    explosionDef.GetClipPool() // directClips - REQUIRED (the clip pool)
);
```

---

### Method 3: Inspector Components (No Code Required)

#### AudioEventSender - Track control via trigger/collision zones

Attach to any GameObject to play, stop, or pause a track when the player enters an area.

| Inspector Field | Description |
|---|---|
| Audio Track Type | BGM / Ambient / Dialogue / Aux1 / Aux2 |
| Sound Definition | **Required** - the sound asset to play |
| Use Definition Defaults | Pull volume/pitch/loop/spatial/fade from the definition (untick to use the fields below) |
| Fade Type | FadeInOut or Crossfade |
| Fade Duration / Target | Duration in seconds; what to fade |
| Volume / Pitch | Playback parameters (used when *Use Definition Defaults* is off) |
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
| Sound Definition | **Required** - its `clip` + `variations` form the random pool |
| Use Definition Defaults | Pull volume/pitch/jitter/chance/loop/3D/delay from the definition (untick to use the fields below) |
| Play On Enabled | Auto-play on activate |
| Volume / Pitch | Playback parameters (used when *Use Definition Defaults* is off) |
| Randomise Pitch / Pitch Range | Pitch variation |
| Spatial Blend | 0 = 2D, 1 = full 3D |
| Loop | Looped playback |
| Percentage Chance To Play | 0–100% probability |
| Randomise Delay / Event Delay | Delay control |
| Use Custom 3D Settings | Override min/max distances |
| Use Custom Position | Play at a fixed world position |
| Collision Type / Target Tag | Trigger or Collision zone setup |
| Attach Sound To This Transform | Sound follows zone object |

> Most SFX fields are now better set **on the SoundDefinition asset** (so every sender/call reuses
> them). Leave *Use Definition Defaults* on and configure the definition; untick only for a one-off
> per-placement override.

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

SFX play from a `SoundDefinition` (by `SoundId` or direct reference) whose bank is loaded. Each `PlaySFX` call instantiates a temporary GameObject with an `AudioSource`, which auto-destroys when the clip finishes. The definition's `clip` + `variations` form the random pool, and its **SFX Variation & 3D** fields (pitch/volume jitter, percent-chance, delay, min/max distance) are applied automatically — so a configured definition needs only `PlaySFX(def)`.

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
[SerializeField] private SoundId combatTheme;
[SerializeField] private SoundId explorationTheme;

public void StartCombat() => AudioEvent.PlayTrack(combatTheme);
public void EndCombat()   => AudioEvent.PlayTrack(explorationTheme);
```

### Environmental Audio Zone (Code)

```csharp
public class AudioZone : MonoBehaviour
{
    [SerializeField] private SoundId ambientTrack;   // pick from the generated dropdown

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            AudioEvent.PlayTrack(ambientTrack, transform);
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
[SerializeField] private SoundId dialogueLine;

public void StartDialogue()
{
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.15f, 0.5f);
    AudioEvent.PlayTrack(dialogueLine);
}

public void EndDialogue()
{
    AudioEvent.StopTrack(AudioTrackType.Dialogue, 0.3f);
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);
}
```

### Randomised Weapon Sounds

```csharp
// Put the shot variations in ONE SoundDefinition (clip + variations); selection is automatic.
[SerializeField] private SoundId gunShots;

public void OnFire() => AudioEvent.PlaySFX(gunShots, transform);
```

---

## Audio Loading - Bank Workflow

### Current: SoundDefinition + SoundBank + Registry

Clips are no longer loaded from `Resources`. Instead:

1. **SoundDefinition** assets wrap each clip (+ optional variations) and its default playback params. Generated by **BMS AudioManager → Generate Sound Definitions**, which mirrors your configured audio folder (`SoundGeneratorSettings.audioSourceRoot`) into asset-safe definitions, groups them into **SoundBank**s (one per category + a `MasterBank`), and emits the typed **`SoundId`** enum.
2. **SoundBanks** are loaded into the **AudioRegistry** at runtime - globally via `AudioManager.startupBanks`, or per-scene via a `SceneAudioBank` component (ref-counted, so banks shared across scenes aren't unloaded early).
3. Code addresses sounds by **`SoundId`** (typed dropdown key, resolved from the registry) or by a direct **`SoundDefinition`** reference. No strings, no index, compile-safe.

**Benefits over the old Resources approach:**
- Asset-safe: renaming a clip never breaks a reference (matched by GUID; the `SoundId` value is stable).
- Only referenced banks are bundled in the build (no blanket `Resources` inclusion).
- Per-scene **availability** control: a sound only plays when its bank is loaded.
- No magic-string lookup; typos become compile errors.

**Parity with the old "everything available":** assign **MasterBank** to `startupBanks`.

> **Note on memory:** `AudioRegistry.UnloadBank` currently controls **availability only** — it removes
> the sound from the registry but does **not** free the clip from RAM (the SoundDefinition still
> references the clip). True per-bank memory release arrives only with an Addressables provider behind
> `SoundDefinition.GetClip()` (see below). Don't rely on `UnloadBank` for memory management yet.

### Future: Addressables (async / remote)

The clip-retrieval choke point is `SoundDefinition.GetClip()` / `GetClipPool()`. Swapping those to an Addressables async load (triggered on `SoundBank` load, released on unload) adds background loading, per-scene memory lifecycle, and DLC/remote delivery - without touching `AudioTrack`, the senders, or the event delegates.

---

## FMOD / Wwise Bridge

The event-driven architecture makes BMS Audio Manager well-positioned as a game-facing API that can route to a professional audio middleware backend.

### Architecture Approach

The game never calls FMOD or Wwise directly - it continues to call `AudioEvent.*` or raise `AudioEventManager` events. A bridge MonoBehaviour subscribes to those events and translates them to middleware calls:

```
Game Code
    ↓  AudioEvent.PlayTrack(SoundId.CombatTheme)
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
- Name clips descriptively - the clip name becomes the generated `SoundId` member name

### Fade Configuration
- Crossfade for musical transitions between similar-energy tracks
- FadeInOut for dramatic scene changes where silence between tracks is acceptable
- Keep fade durations between 0.5–3 seconds for natural feel
- Use `FadeTarget.FadeVolume` when pitch should snap immediately

---

## Known Limitations

- Only one clip per track type can play at a time (BGM, Ambient, Dialogue, Aux1, Aux2 - by design; use SFX for additional concurrent sounds)
- A sound only plays when its bank is loaded into the registry (assign **MasterBank** to `startupBanks` for "everything available")
- `AudioRegistry.UnloadBank` controls **availability**, not memory - clips stay resident until an Addressables provider is added (see Audio Loading)
- Clips referenced by definitions load with their owning scene/assets (synchronous); async/streaming needs the deferred Addressables provider
- `SplineFollower` requires `com.unity.splines` - the project will not compile if the package is absent and these scripts are included
- Mixer group slots on the AudioManager are optional - if left unassigned, audio sources route to whatever is set on the shared prefab

---

## License

This project is licensed under **CC BY-ND 4.0**.

Free to use in any project, including commercial. You may not redistribute modified versions of the package itself.

© 2026 Niall Mc Shane - [creativecommons.org/licenses/by-nd/4.0](https://creativecommons.org/licenses/by-nd/4.0/)

---

*BMS Audio Manager v3.0.0 - Unity 6*
