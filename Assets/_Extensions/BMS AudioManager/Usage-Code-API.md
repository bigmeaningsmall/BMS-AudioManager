# BMS Audio Manager - Code API Reference

> **Bank-only workflow.** Audio is no longer loaded from `Resources` by string name. Clips live in
> **SoundDefinition** assets, grouped into **SoundBank**s that you load into the registry
> (`AudioManager.startupBanks` or a `SceneAudioBank` component). Run **BMS AudioManager → Generate
> Sound Definitions** to (re)build the definitions, banks, and the typed **`SoundId`** enum.
>
> You address audio two string-free ways:
> - **`SoundId`** — a generated typed key (inspector dropdown), resolved from the loaded registry.
> - **`SoundDefinition`** — a direct asset reference that carries its own default params.
>
> The old string/index overloads (`PlayTrack(type, "name")`, `PlaySFX("name")`, `PlayTrack(type, 0)`)
> have been **removed**. Stop/Pause/Adjust still take an `AudioTrackType` because they act on a
> channel, not a specific clip.

---

## AudioEvent - Quick Helper Methods

The easiest way to call audio. Import nothing - just call `AudioEvent.*` from any script.

### Play Track

A track's channel and default params come from its SoundDefinition, so calls are short. Reference a
sound by `SoundId` (typed key) or by a `SoundDefinition` field.

```csharp
[SerializeField] private SoundId mainTheme;          // dropdown of generated ids
[SerializeField] private SoundDefinition forestDef;  // or a direct asset reference

// Minimal - routes to the definition's own channel, uses its default volume/pitch/loop/fade
AudioEvent.PlayTrack(mainTheme);

// Volume override (rest from the definition)
AudioEvent.PlayTrack(mainTheme, 0.8f);

// Attached to a transform (auto-spatialised 3D)
AudioEvent.PlayTrack(mainTheme, someTransform);

// Smart dispatch - plays as a track or SFX depending on the definition's category
AudioEvent.Play(mainTheme);

// SoundDefinition reference instead of a SoundId
AudioEvent.PlayTrack(forestDef);
AudioEvent.PlayTrack(forestDef, 0.6f, 2f);   // volume + fade-duration overrides

// Crossfade vs fade-in-out and the fade duration are set ON the SoundDefinition asset.
// For a full ad-hoc override use the SoundDefinition passthrough:
AudioEvent.PlayTrackFull(forestDef, 0.8f, 1f, 0f, FadeType.Crossfade, 2f, FadeTarget.FadeBoth, true, 0f, null, "MyEvent");
```

### Stop Track

```csharp
// Instant stop
AudioEvent.StopTrack(AudioTrackType.BGM);

// With fade out
AudioEvent.StopTrack(AudioTrackType.BGM, 2f);

// With fade out and specific target
AudioEvent.StopTrack(AudioTrackType.BGM, 2f, FadeTarget.FadeVolume);
```

### Pause / Resume Track

Pause is a **toggle** - call it again to resume.

```csharp
// Instant pause/resume
AudioEvent.PauseTrack(AudioTrackType.BGM);

// With fade
AudioEvent.PauseTrack(AudioTrackType.BGM, 1f);
```

### Adjust Track Parameters

Use these to duck, swell, or pitch-shift a playing track without restarting it.

```csharp
// Volume only - instant
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.3f);

// Volume only - with fade
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.3f, 1.5f);

// Volume + pitch - instant
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.5f, 1.2f);

// Volume + pitch - with fade
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.5f, 1.2f, 2f);
```

---

### Play SFX

A SoundDefinition carries the variation parameters itself, so a single `PlaySFX(def)` is usually all
you need. On the asset (under **SFX Variation & 3D**) you set: `clip` + `variations` (random pool),
`randomizePitch` + `pitchRange`, `volumeRange`, `percentChanceToPlay`, `randomizeDelay` + `delay`, and
`minDistance` / `maxDistance`. A gun, footstep set, or impact then plays with full variety from one call —
no per-call parameters. (Override args like `PlaySFX(def, volume)` still take precedence where given.)

```csharp
[SerializeField] private SoundId explosion;     // SFX-category id
[SerializeField] private SoundDefinition footsteps; // a def with variations for random selection

// Minimal (2D, definition defaults)
AudioEvent.PlaySFX(explosion);

// Volume override
AudioEvent.PlaySFX(explosion, 0.8f);

// 3D - attached to a transform (follows the object)
AudioEvent.PlaySFX(explosion, playerTransform);

// 3D - at a world position
AudioEvent.PlaySFX(explosion, new Vector3(10f, 0f, 5f));

// 3D - explicit min/max distances
AudioEvent.PlaySFX3D(explosion, thunderTransform, 5f, 100f);

// Looped (stop via AudioManager.StopAllLoopedSFX / StopAllSFX)
AudioEvent.PlayLoopedSFX(explosion, carTransform);

// SoundDefinition reference - random variation picked automatically from the def's pool
AudioEvent.PlaySFX(footsteps, playerTransform);

// Smart dispatch also works for SFX-category sounds
AudioEvent.Play(explosion);
```

---

## AudioEventManager - Full Parameter Control

Use this when you need parameters that `AudioEvent` doesn't expose (spatial blend, loop toggle, delay,
event name). The clip is supplied via the trailing **`directClip` / `directClips`** argument (from a
SoundDefinition). The `trackName` / `trackNumber` / `soundNames` parameters are **vestigial - ignored**
at runtime; pass `-1` / `""` / `null`.

### Play Track

```csharp
[SerializeField] private SoundDefinition combatDef;

AudioEventManager.PlayTrack(
    combatDef.TrackType,       // track type (channel)
    -1,                        // track index - ignored
    "",                        // track name - ignored
    0.9f,                      // volume (0–1)
    1f,                        // pitch (0.5–2 typical)
    0f,                        // spatial blend (0 = 2D, 1 = full 3D)
    FadeType.Crossfade,        // FadeInOut | Crossfade
    3f,                        // fade duration (seconds)
    FadeTarget.FadeBoth,       // FadeVolume | FadePitch | FadeBoth | Ignore
    true,                      // loop
    0f,                        // delay before playing (seconds)
    null,                      // attach to Transform (or null)
    "Combat Start",            // event name for debug (can be "")
    combatDef.GetClip()        // directClip - REQUIRED (the actual clip)
);
```

### Stop Track

```csharp
AudioEventManager.StopTrack(
    AudioTrackType.BGM,
    1f,                        // fade duration
    FadeTarget.FadeVolume,     // fade target
    0f,                        // delay
    ""                         // event name
);
```

### Pause Track

```csharp
AudioEventManager.PauseTrack(
    AudioTrackType.Ambient,
    0.5f,                      // fade duration
    FadeTarget.FadeVolume,
    0f,
    ""
);
```

### Adjust Track

```csharp
AudioEventManager.AdjustTrack(
    AudioTrackType.BGM,
    0.3f,                      // volume
    1f,                        // pitch
    0f,                        // spatial blend
    1f,                        // fade duration
    FadeTarget.FadeVolume,
    true,                      // loop
    0f,                        // delay
    null,                      // new transform (or null to keep current)
    ""
);
```

### Play SFX

```csharp
[SerializeField] private SoundDefinition hitDef; // clip + variations = random pool

AudioEventManager.PlaySFX(
    null,                             // soundNames - ignored
    0.8f,                             // volume
    1f,                               // pitch
    true,                             // randomise pitch
    0.2f,                             // pitch variation range
    1f,                               // spatial blend (0–1)
    false,                            // loop
    0f,                               // delay (seconds)
    100f,                             // % chance to play (0–100)
    someTransform,                    // attach to transform (or null)
    Vector3.zero,                     // world position (used if transform is null)
    1f,                               // min distance
    30f,                              // max distance
    "HitEffect",                      // event name
    hitDef.GetClipPool()              // directClips - REQUIRED (the clip pool)
);
```

---

## AudioManager.Instance - Global SFX Controls

```csharp
// Stop everything
AudioManager.Instance.StopAllSFX();
AudioManager.Instance.StopAllLoopedSFX();

// Pause / resume all SFX
AudioManager.Instance.TogglePauseAllSFX();
AudioManager.Instance.PauseAllSFX(true);   // explicit pause
AudioManager.Instance.PauseAllSFX(false);  // explicit resume

// Cancel queued (delayed) SFX
AudioManager.Instance.CancelAllDelayedSFX();

// Global volume multiplier - scales all SFX output (0–1)
AudioManager.Instance.GlobalSFXAttenuation = 0.5f;

// Query active SFX
int count      = AudioManager.Instance.GetActiveSFXCount();
string[] names = AudioManager.Instance.GetActiveSFXNames();
bool paused    = AudioManager.Instance.AllSFXPaused;

// Query a track's current state
AudioTrackParamters bgmState = AudioManager.Instance.GetTrackParameters(AudioTrackType.BGM);
```

---

## Enums Quick Reference

```csharp
// Which track to target
AudioTrackType.BGM
AudioTrackType.Ambient
AudioTrackType.Dialogue
AudioTrackType.Aux1     // general purpose auxiliary track
AudioTrackType.Aux2     // general purpose auxiliary track

// How to transition when playing a new track over an existing one
FadeType.FadeInOut    // fade out current first, then fade in new (sequential)
FadeType.Crossfade    // both overlap - old fades out while new fades in

// What an AudioEventSender zone does on enter or exit (Inspector only)
TriggerAction.Play
TriggerAction.Stop
TriggerAction.Pause
TriggerAction.AdjustParameters
TriggerAction.None

// Which parameter(s) to animate during a fade
FadeTarget.FadeVolume  // only volume transitions
FadeTarget.FadePitch   // only pitch transitions
FadeTarget.FadeBoth    // both volume and pitch transition
FadeTarget.Ignore      // instant change, no fading
```

---

## Common Patterns

### Duck BGM for Dialogue

```csharp
[SerializeField] private SoundId dialogueLine;

void StartDialogue()
{
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.15f, 0.5f);
    AudioEvent.PlayTrack(dialogueLine);
}

void EndDialogue()
{
    AudioEvent.StopTrack(AudioTrackType.Dialogue, 0.3f);
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);
}
```

### Combat Music Crossfade

```csharp
[SerializeField] private SoundId combatTheme;

// Set the combat definition's fadeType to Crossfade and its fadeDuration on the asset;
// then the transition style/timing is intrinsic to the sound.
void EnterCombat() => AudioEvent.PlayTrack(combatTheme);
```

### Randomised Footsteps

```csharp
// Put the step variations in ONE SoundDefinition (clip + variations); the pool is picked from automatically.
[SerializeField] private SoundId footsteps;

void OnStep() => AudioEvent.PlaySFX(footsteps, playerTransform);
```

### Zone Ambient (code version)

```csharp
[SerializeField] private SoundId caveAmbient;

void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
        AudioEvent.PlayTrack(caveAmbient, transform);
}

void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player"))
        AudioEvent.StopTrack(AudioTrackType.Ambient, 2f);
}
```
