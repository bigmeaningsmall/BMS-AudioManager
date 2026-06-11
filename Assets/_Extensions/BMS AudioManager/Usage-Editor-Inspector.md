# BMS Audio Manager — Inspector & Editor Usage

How to trigger audio entirely from the Unity Editor — no code required.

---

## Setup

1. Place the **AudioManager prefab** in your scene. It must be present for any audio to work.
2. Put your audio clips in the correct `Resources/Audio/` subfolder:

```
Assets/Resources/Audio/
    BGM/         ← background music
    Ambient/     ← environmental beds
    Dialogue/    ← speech / narration
    SFX/         ← sound effects
```

The **filename without extension** is the name you type into the inspector fields.

---

## AudioEventSender — Play a Track from a GameObject

Use this to play, stop, or pause a BGM / Ambient / Dialogue track when a player enters a zone, or when any GameObject activates.

**Add component:** `Add Component → AudioEventSender`

### Inspector Fields

| Field | What it does |
|---|---|
| **Audio Track Type** | Which track to control: BGM, Ambient, Dialogue, Aux1, or Aux2 |
| **Track Name** | Clip filename (without extension) e.g. `ForestAmbient` |
| **Track Number** | Play by index instead of name (leave at -1 to use Track Name) |
| **Volume** | Playback volume 0–1 |
| **Pitch** | Playback pitch (1 = normal, 0.5 = half speed, 2 = double) |
| **Spatial Blend** | 0 = 2D stereo, 1 = full 3D positional |
| **Loop** | Keep looping until stopped |
| **Play On Enabled** | Auto-play when the GameObject is enabled/activated |
| **Fade Type** | `FadeInOut` — fade out old then fade in new; `Crossfade` — overlap simultaneously |
| **Fade Duration** | Seconds for the fade to complete |
| **Fade Target** | `FadeVolume`, `FadePitch`, `FadeBoth`, or `Ignore` (instant) |
| **Attach To This Transform** | Audio source moves with this GameObject (good for 3D ambient zones) |
| **Transform To Attach To** | Attach to a different specific transform instead |
| **Event Delay** | Seconds to wait before playing after trigger |
| **Event Name** | Optional label shown in debug logs |

### Collision / Trigger Zone Setup

| Field | What it does |
|---|---|
| **Collision Type** | `Trigger` (OnTriggerEnter) or `Collision` (OnCollisionEnter), or `Null` to disable |
| **Target Tag** | Only respond to objects with this tag (e.g. `Player`) |
| **On Enter Action** | What happens when the target enters — `Play`, `Stop`, `Pause`, `AdjustParameters`, or `None` |
| **On Exit Action** | What happens when the target exits — same options as above |

> The GameObject also needs a **Collider** set as a Trigger (or a solid collider if using Collision mode).

**Common zone configurations:**

| Goal | On Enter | On Exit |
|---|---|---|
| Play on enter, stop on exit | `Play` | `Stop` |
| Stop any music when entering a zone | `Stop` | `None` |
| Pause on exit, resume on re-entry | `Play` | `Pause` |
| Zone that never auto-stops | `Play` | `None` |
| Adjust parameters only | `AdjustParameters` | `None` |

### Stop / Pause / Adjust via UnityEvents or Buttons

The component exposes public methods you can wire to UI buttons or UnityEvent fields:

| Method | Effect |
|---|---|
| `Play()` | Plays the track with current inspector settings |
| `Stop()` | Stops the track with a fade |
| `Pause()` | Toggles pause/resume |
| `AdjustParameters()` | Re-applies the current volume/pitch/spatial settings to the playing track |

### Inspector Test Keys (Play Mode only)

With the component selected in the Inspector during Play mode:

| Key | Action |
|---|---|
| `M` | Play |
| `N` | Stop |
| `B` | Pause / Resume |
| `V` | Adjust Parameters |

---

## AudioEventSenderSFX — Play a Sound Effect from a GameObject

Use this for footsteps, pickups, ambient bird chirps, buttons, or any one-shot / looped SFX.

**Add component:** `Add Component → AudioEventSenderSFX`

### Inspector Fields

| Field | What it does |
|---|---|
| **SFX Name** (array) | One or more clip filenames — one is picked at random each play |
| **Play On Enabled** | Auto-play when the GameObject activates |
| **Volume** | 0–1 |
| **Pitch** | 1 = normal |
| **Randomise Pitch** | Vary pitch slightly on each play |
| **Pitch Range** | How much to vary pitch when Randomise Pitch is on |
| **Spatial Blend** | 0 = 2D, 1 = full 3D |
| **Loop** | Keep looping |
| **Percentage Chance To Play** | 0–100 — e.g. 40 means it only plays 40% of the time |
| **Randomise Delay** | Randomise the wait before playing |
| **Event Delay** | Fixed delay in seconds (used if Randomise Delay is off) |
| **Attach Sound To This Transform** | SFX follows this GameObject's position |
| **Transform To Attach To** | Attach to a different transform instead |
| **Use Custom 3D Settings** | Override the default min/max hearing distances |
| **Min Distance / Max Distance** | Audible range when Use Custom 3D Settings is on |
| **Use Custom Position** | Play at a fixed world position instead of a transform |
| **Custom Position** | The world position to play at when above is enabled |
| **Event Name** | Optional label for debug logs |

### Collision / Trigger Zone Setup

Same as AudioEventSender above — set **Collision Type**, **Target Tag**.

### Public Methods (for buttons / UnityEvents)

| Method | Effect |
|---|---|
| `Play()` | Plays the SFX with current inspector settings |
| `Stop()` | Stops **all** active SFX system-wide |
| `Pause()` | Toggles pause for **all** SFX system-wide |

### Inspector Test Keys (Play Mode only)

| Key | Action |
|---|---|
| `T` | Play |
| `S` | Stop All SFX |
| `P` | Pause / Resume All SFX |

---

## AudioTrackParameterDisplay — Monitor Track States

Attach to any GameObject to see live BGM, Ambient, and Dialogue track states in the Inspector while the game runs.

**Add component:** `Add Component → AudioTrackParameterDisplay`

Shows for each track: current state, clip name, volume, pitch, spatial blend, loop, and playback progress. Useful for debugging without opening the AudioTrack component directly.

**Context menu:** Right-click the component → **Refresh Parameters** to force an update.

---

## SFXDebugDisplay — Monitor Active SFX

Attach to any GameObject to see what SFX are currently playing.

**Add component:** `Add Component → SFXDebugDisplay`

Shows: active SFX count, delayed SFX count, and the names of currently playing sounds. Updates on a configurable interval.

**Context menu actions** (right-click the component in Play mode):

| Action | Effect |
|---|---|
| Stop All SFX | Immediately stops all playing SFX |
| Stop All Looped SFX | Stops only looped SFX |
| Pause All SFX | Pauses all SFX |
| Resume All SFX | Resumes paused SFX |
| Cancel All Delayed SFX | Removes queued sounds that haven't played yet |
| Log SFX Details | Prints a full report to the Console |

---

## AudioTrack Custom Inspector

The **AudioTrack** component (on the AudioManager prefab) has a custom editor. Select the AudioManager in the Hierarchy during Play mode to see:

- **Main / Cue / Outgoing** source states, each colour-coded (green / yellow / cyan)
- Live **volume** and **pitch** sliders
- **Playback progress bar**
- **Waveform preview** for decompressed clips
- **State label** floating above each audio source in the Scene view

---

## AudioManager Inspector

Select the **AudioManager** in the Hierarchy to:

- Toggle **Enable Debug Logging** for verbose console output
- **Right-click → Validate Audio Track Setup** to check that tracks are correctly configured

### Mixer Groups

The **Mixer Groups** section has one slot per track type plus one for SFX. Drag your mixer groups from the Audio Mixer window into each slot. Each `AudioSource` created at runtime will have its `outputAudioMixerGroup` assigned automatically — the shared prefab's baked-in output is overridden.

| Slot | Routes to |
|---|---|
| BGM Mixer Group | BGM track sources |
| Ambient Mixer Group | Ambient track sources |
| Dialogue Mixer Group | Dialogue track sources |
| Aux1 Mixer Group | Aux1 track sources |
| Aux2 Mixer Group | Aux2 track sources |
| SFX Mixer Group | All instantiated SFX sources |

Slots left empty will fall back to the prefab's default output.

---

## Typical Zone Setup (Step by Step)

**Goal:** Play cave ambient music when the player walks into an area, stop it when they leave.

1. Create an empty GameObject, name it `CaveZone`
2. Add a **Box Collider**, tick **Is Trigger**
3. Size and position the collider to cover the cave entrance area
4. Add component **AudioEventSender**
5. Set **Audio Track Type** → `Ambient`
6. Set **Track Name** → `CaveAmbient` (must match the filename in `Resources/Audio/Ambient/`)
7. Set **Volume** → `0.7`
8. Set **Fade Type** → `Crossfade`, **Fade Duration** → `3`
9. Set **Collision Type** → `Trigger`, **Target Tag** → `Player`
10. Tick **Stop On Exit**
11. Optionally tick **Attach To This Transform** if you want the ambient to be positionally 3D

Done — no code written.

---

## Typical SFX Zone Setup (Step by Step)

**Goal:** Play a random footstep crunch sound when the player walks over gravel.

1. Create an empty GameObject, name it `GravelZone`
2. Add a **Box Collider**, tick **Is Trigger**, size it to the gravel patch
3. Add component **AudioEventSenderSFX**
4. Set **SFX Name** array size to 3, fill with `GravelStep1`, `GravelStep2`, `GravelStep3`
5. Set **Volume** → `0.8`, tick **Randomise Pitch**, set **Pitch Range** → `0.15`
6. Set **Percentage Chance To Play** → `70` (plays 70% of the time for variety)
7. Set **Collision Type** → `Trigger`, **Target Tag** → `Player`
8. Set **Spatial Blend** → `1`, tick **Use Custom 3D Settings**, **Min Distance** `1`, **Max Distance** `10`

Done.
