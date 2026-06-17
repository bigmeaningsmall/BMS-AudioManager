# BMS Audio Manager v3.0.0 (pre-RC)

- Release package is to be added once example usage and demo scenes are finalised

A Unity audio management system featuring 3-source crossfading, an event-driven architecture, spatial audio, and inspector-driven audio zones. Audio is organised through **asset-based SoundDefinitions** grouped into loadable **SoundBanks**, and addressed in code by a generated, compile-safe **`SoundId`** enum — no magic strings, no `Resources` dependency. Designed for small-to-medium projects requiring clean audio transitions and flexible sound management.

**Unity Version:** 6000.4.8f1+
**Optional Dependency:** `com.unity.splines` (required only for SplineFollower components)

Full documentation is in [`Assets/_Extensions/BMS AudioManager/ReadMe.md`](Assets/_Extensions/BMS%20AudioManager/ReadMe.md).

- [Code API Reference](Assets/_Extensions/BMS%20AudioManager/Usage-Code-API.md) - calling audio from scripts
- [Inspector & Editor Usage](Assets/_Extensions/BMS%20AudioManager/Usage-Editor-Inspector.md) - audio zones and components without code
- [Changelog](Assets/_Extensions/BMS%20AudioManager/CHANGELOG.md) - version history and migration notes

---

## Installation

1. Go to the [Releases](../../releases) page and download the latest `BMS-AudioManager(v3.0.0).unitypackage`
2. Open your Unity project
3. Double-click the downloaded file, or drag it into the Unity Editor
4. In the Import dialog, make sure everything is ticked and click **Import**

The package will install into `Assets/_Extensions/BMS AudioManager/` - keep it there and separate from your game logic.

### Updating

Download the new `.unitypackage` from the Releases page and import it into the same project. Unity will overwrite the existing files in place.

---

## Key Features

- **Asset-Based Sound Library** - `SoundDefinition` assets carry each clip + its default playback/variation settings
- **Typed, String-Free API** - a generated `SoundId` enum gives compile-safe, autocompleting sound keys (no `Resources`)
- **Loadable Sound Banks** - group definitions into banks loaded globally or per-scene (ref-counted)
- **One-Click Generation** - editor tool builds definitions, banks, and the `SoundId` enum; idempotent and rename-safe
- **3-Source Audio System** - Seamless crossfading with no audio gaps
- **5 Independent Tracks** - BGM, Ambient, Dialogue, Aux1, Aux2 with separate state machines
- **Full Fade Control** - FadeInOut and Crossfade types; target volume, pitch, or both
- **SFX System** - Dynamic instantiation, looping, 3D positioning, pitch/volume randomisation, probability-based playback
- **Audio Mixer Routing** - Per-track and SFX mixer group assignment in the AudioManager Inspector
- **Event-Driven Architecture** - Three API tiers from one-liner helpers to full event control
- **Assignable Zone Actions** - Enter/exit trigger actions (Play, Stop, Pause, Adjust) set in the Inspector
- **Spline Audio** - Audio sources that follow Unity Spline paths with sleep optimisation
- **Real-Time Parameter Control** - Adjust volume, pitch, and spatial blend during playback
- **Editor Tooling** - Custom inspector with live waveform, 3-source state visualisation, scene gizmos

---

## Quick Start

1. Import the package and place the **AudioManager prefab** in your scene
2. Put audio clips in a folder with category subfolders (`BGM/`, `Ambient/`, `Dialogue/`, `Aux1/`, `Aux2/`, `SFX/`) — anywhere under `Assets/`, no `Resources` needed
3. Run **BMS AudioManager → Generate Sound Definitions** (point `SoundGeneratorSettings.audioSourceRoot` at that folder)
4. Assign the generated **MasterBank** to the AudioManager's **Startup Banks**
5. Play by typed id:

```csharp
[SerializeField] private SoundId mainTheme;
[SerializeField] private SoundId buttonClick;

AudioEvent.PlayTrack(mainTheme);   // music on its channel
AudioEvent.Play(buttonClick);      // auto-routed track-vs-SFX
```

See the full README for the complete API, examples, and improvement roadmap.

---

## License

Licensed under **CC BY-ND 4.0** - free to use in any project including commercial. No redistributing modified versions.

© 2026 Niall Mc Shane - [creativecommons.org/licenses/by-nd/4.0](https://creativecommons.org/licenses/by-nd/4.0/)

*BMS Audio Manager v3.0.0 - Unity 6*
