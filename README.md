# BMS Audio Manager v2.2.0

A Unity audio management system featuring 3-source crossfading, an event-driven architecture, spatial audio, and inspector-driven audio zones. Designed for small-to-medium projects requiring clean audio transitions and flexible sound management.

**Unity Version:** 6000.4.8f1+
**Optional Dependency:** `com.unity.splines` (required only for SplineFollower components)

Full documentation is in [`Assets/Extensions/BMS AudioManager/ReadMe.md`](Assets/Extensions/BMS%20AudioManager/ReadMe.md).

- [Code API Reference](Assets/Extensions/BMS%20AudioManager/Usage-Code-API.md) — calling audio from scripts
- [Inspector & Editor Usage](Assets/Extensions/BMS%20AudioManager/Usage-Editor-Inspector.md) — audio zones and components without code

---

## Installation

### Via Unity Package Manager (Git URL) — Recommended

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Paste:

```
https://github.com/bigmeaningsmall/BMS-AudioManager.git?path=Assets/Extensions/BMS AudioManager
```

> The `?path=` suffix tells Unity where the `package.json` lives inside the repo.

### Pinning to a specific version

To lock to a release tag rather than always pulling the latest:

```
https://github.com/bigmeaningsmall/BMS-AudioManager.git?path=Assets/Extensions/BMS AudioManager#v2.2.0
```

Change `v2.2.0` to whichever tag you want. Tags are listed on the [GitHub Releases](../../releases) page.

### Updating

Unity does not auto-update Git-sourced packages. To update:
- Open **Package Manager**, find **BMS Audio Manager**, and click **Update** if a new commit is available, or
- Edit `Packages/manifest.json` in your project and change the tag on the end of the URL to the new version tag.

---

## Key Features

- **3-Source Audio System** — Seamless crossfading with no audio gaps
- **5 Independent Tracks** — BGM, Ambient, Dialogue, Aux1, Aux2 with separate state machines
- **Full Fade Control** — FadeInOut and Crossfade types; target volume, pitch, or both
- **SFX System** — Dynamic instantiation, looping, 3D positioning, pitch randomization, probability-based playback
- **Audio Mixer Routing** — Per-track and SFX mixer group assignment in the AudioManager Inspector
- **Event-Driven Architecture** — Three API tiers from one-liner helpers to full event control
- **Assignable Zone Actions** — Enter/exit trigger actions (Play, Stop, Pause, Adjust) set in the Inspector
- **Spline Audio** — Audio sources that follow Unity Spline paths with sleep optimization
- **Real-Time Parameter Control** — Adjust volume, pitch, and spatial blend during playback
- **Editor Tooling** — Custom inspector with live waveform, 3-source state visualization, scene gizmos

---

## Quick Start

1. Import the package and place the **AudioManager prefab** in your scene
2. Put audio clips in `Assets/Resources/Audio/BGM/`, `Ambient/`, `Dialogue/`, `Aux1/`, `Aux2/`, or `SFX/`
3. Call:

```csharp
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");
AudioEvent.PlaySFX("ButtonClick");
```

See the full README for the complete API, examples, and improvement roadmap.

---

## License

Licensed under **CC BY-ND 4.0** — free to use in any project including commercial. No redistributing modified versions.

© 2026 Niall Mc Shane — [creativecommons.org/licenses/by-nd/4.0](https://creativecommons.org/licenses/by-nd/4.0/)

*BMS Audio Manager v2.2.0 — Unity 6*
