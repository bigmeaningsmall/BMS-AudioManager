# BMS Audio Manager v2.1.0

A Unity audio management system featuring 3-source crossfading, an event-driven architecture, spatial audio, and inspector-driven audio zones. Designed for small-to-medium projects requiring clean audio transitions and flexible sound management.

**Unity Version:** 6000.4.8f1+
**Optional Dependency:** `com.unity.splines` (required only for SplineFollower components)

Full documentation is in [`Assets/Extensions/BMS AudioManager/ReadMe.md`](Assets/Extensions/BMS%20AudioManager/ReadMe.md).

---

## Key Features

- **3-Source Audio System** — Seamless crossfading with no audio gaps
- **3 Independent Tracks** — BGM, Ambient, and Dialogue with separate state machines
- **Full Fade Control** — FadeInOut and Crossfade types; target volume, pitch, or both
- **SFX System** — Dynamic instantiation, looping, 3D positioning, pitch randomization, probability-based playback
- **Event-Driven Architecture** — Three API tiers from one-liner helpers to full event control
- **Inspector Audio Zones** — Trigger/collision-based audio with no code required
- **Spline Audio** — Audio sources that follow Unity Spline paths with sleep optimization
- **Real-Time Parameter Control** — Adjust volume, pitch, and spatial blend during playback
- **Editor Tooling** — Custom inspector with live waveform, 3-source state visualization, scene gizmos

---

## Quick Start

1. Import the package and place the **AudioManager prefab** in your scene
2. Put audio clips in `Assets/Resources/Audio/BGM/`, `Ambient/`, `Dialogue/`, or `SFX/`
3. Call:

```csharp
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");
AudioEvent.PlaySFX("ButtonClick");
```

See the full README for the complete API, examples, and improvement roadmap.

---

## License

Open source — free to use and modify for your projects.

*BMS Audio Manager v2.1.0 — Unity 6*
