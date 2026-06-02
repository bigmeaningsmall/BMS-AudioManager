# BMS Audio Manager — Code API Reference

Audio clips are loaded by name from `Resources/Audio/<Type>/`. The filename without extension is the string you pass.

---

## AudioEvent — Quick Helper Methods

The easiest way to call audio. Import nothing — just call `AudioEvent.*` from any script.

### Play Track

```csharp
// Minimal — plays at volume 1, loops, 0.5s fade in
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme");

// With volume (0–1)
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f);

// With volume + fade duration (seconds)
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 2f);

// With volume + fade + spatial attachment (auto-sets 3D)
AudioEvent.PlayTrack(AudioTrackType.Ambient, "ForestAmbient", 0.6f, 2f, someTransform);

// By index instead of name
AudioEvent.PlayTrack(AudioTrackType.BGM, 0);

// With volume, pitch, fade duration, and fade type
AudioEvent.PlayTrack(AudioTrackType.BGM, "MainTheme", 0.8f, 1f, 2f, FadeType.Crossfade);

// Full control (all common parameters)
AudioEvent.PlayTrackFull(
    AudioTrackType.BGM,        // track type
    "MainTheme",               // clip name
    0.8f,                      // volume
    1f,                        // pitch
    0f,                        // spatial blend (0 = 2D, 1 = 3D)
    FadeType.FadeInOut,        // fade type
    2f,                        // fade duration
    FadeTarget.FadeBoth,       // what to fade
    true,                      // loop
    0f,                        // delay before playing
    null,                      // attach to transform
    "MyEvent"                  // event name (for debug)
);
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

Pause is a **toggle** — call it again to resume.

```csharp
// Instant pause/resume
AudioEvent.PauseTrack(AudioTrackType.BGM);

// With fade
AudioEvent.PauseTrack(AudioTrackType.BGM, 1f);
```

### Adjust Track Parameters

Use these to duck, swell, or pitch-shift a playing track without restarting it.

```csharp
// Volume only — instant
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.3f);

// Volume only — with fade
AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.3f, 1.5f);

// Volume + pitch — instant
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.5f, 1.2f);

// Volume + pitch — with fade
AudioEvent.AdjustTrack(AudioTrackType.BGM, 0.5f, 1.2f, 2f);
```

---

### Play SFX

```csharp
// Minimal
AudioEvent.PlaySFX("ButtonClick");

// With volume
AudioEvent.PlaySFX("Explosion", 0.8f);

// Random pick from array
AudioEvent.PlaySFX(new string[] { "Footstep1", "Footstep2", "Footstep3" });

// With random pitch variation (±0.1 default)
AudioEvent.PlaySFX("DoorCreak", 0.7f, true);

// 3D — attached to a transform (follows the object)
AudioEvent.PlaySFX("MagicSpell", 0.8f, playerTransform);

// 3D — at a world position
AudioEvent.PlaySFX("Explosion", 1f, new Vector3(10f, 0f, 5f));

// 3D — explicit pitch range (x = min, y = max)
AudioEvent.PlaySFX("HitSound", 0.9f, new Vector2(0.9f, 1.2f), enemyTransform);

// 3D — with random pitch toggle + transform
AudioEvent.PlaySFX("Footstep", 0.8f, true, playerTransform);

// 3D — with custom min/max distances
AudioEvent.PlaySFX3D("DistantThunder", 0.6f, thunderTransform, 5f, 100f);

// Looped (returns nothing — stop via StopAllLoopedSFX or StopAllSFX)
AudioEvent.PlayLoopedSFX("EngineHum", 0.7f, carTransform);
AudioEvent.PlayLoopedSFX("Ambience", 0.5f);   // 2D looped (no transform)

// Random pick + chance to play + delay
AudioEvent.PlayRandomSFX(new string[] { "Bird1", "Bird2" }, 0.4f, 30f, 2f);
//                                                           ^vol   ^%   ^delay(s)
```

---

## AudioEventManager — Full Parameter Control

Use this when you need parameters that `AudioEvent` doesn't expose (spatial blend, loop toggle, delay, event name).

### Play Track

```csharp
AudioEventManager.PlayTrack(
    AudioTrackType.BGM,        // track type: BGM | Ambient | Dialogue
    -1,                        // track index (-1 = use name instead)
    "CombatTheme",             // clip name
    0.9f,                      // volume (0–1)
    1f,                        // pitch (0.5–2 typical)
    0f,                        // spatial blend (0 = 2D, 1 = full 3D)
    FadeType.Crossfade,        // FadeInOut | Crossfade
    3f,                        // fade duration (seconds)
    FadeTarget.FadeBoth,       // FadeVolume | FadePitch | FadeBoth | Ignore
    true,                      // loop
    0f,                        // delay before playing (seconds)
    null,                      // attach to Transform (or null)
    "Combat Start"             // event name for debug (can be "")
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
AudioEventManager.PlaySFX(
    new string[] { "Hit1", "Hit2" },  // array — one picked at random
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
    "HitEffect"                       // event name
);
```

---

## AudioManager.Instance — Global SFX Controls

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

// Global volume multiplier — scales all SFX output (0–1)
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

// How to transition when playing a new track over an existing one
FadeType.FadeInOut    // fade out current first, then fade in new (sequential)
FadeType.Crossfade    // both overlap — old fades out while new fades in

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
void StartDialogue(string clip)
{
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.15f, 0.5f);
    AudioEvent.PlayTrack(AudioTrackType.Dialogue, clip, 1f, 0.3f);
}

void EndDialogue()
{
    AudioEvent.StopTrack(AudioTrackType.Dialogue, 0.3f);
    AudioEvent.AdjustTrackVolume(AudioTrackType.BGM, 0.8f, 1f);
}
```

### Combat Music Crossfade

```csharp
void EnterCombat()
{
    AudioEventManager.PlayTrack(AudioTrackType.BGM, -1, "CombatTheme",
        0.9f, 1f, 0f, FadeType.Crossfade, 2f, FadeTarget.FadeBoth, true, 0f, null, "");
}
```

### Randomised Footsteps

```csharp
private string[] steps = { "Step1", "Step2", "Step3", "Step4" };

void OnStep()
{
    AudioEvent.PlaySFX(steps, 0.7f, new Vector2(0.9f, 1.1f), playerTransform);
}
```

### Zone Ambient (code version)

```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
        AudioEvent.PlayTrack(AudioTrackType.Ambient, "CaveAmbient", 0.6f, 3f, transform);
}

void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player"))
        AudioEvent.StopTrack(AudioTrackType.Ambient, 2f);
}
```
