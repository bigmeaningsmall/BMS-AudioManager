# BMS Audio Manager - Inspector & Editor Usage

How to trigger audio entirely from the Unity Editor - no code required.

**See also:** [README](ReadMe.md) (overview & core concepts) · [Code API Reference](Usage-Code-API.md) (scripting) · [Changelog](CHANGELOG.md)

---

## Setup

1. Place the **AudioManager prefab** in your scene. It must be present for any audio to work.
2. Put your audio clips in a folder with one subfolder per category (the subfolder name sets the
   category). This folder is configurable on the **SoundGeneratorSettings** asset and does **not** need
   to be under `Resources`:

```
<audioSourceRoot>/      ← set on SoundGeneratorSettings (default: Assets/Audio)
    BGM/                ← background music
    Ambient/            ← environmental beds
    Dialogue/           ← speech / narration
    Aux1/  Aux2/        ← auxiliary tracks
    Ambience-Environment/
    SFX/                ← sound effects
```

3. Run **BMS AudioManager → Generate Sound Definitions**. This creates a **SoundDefinition** asset per
   clip, groups them into **SoundBanks** (one per category + a `MasterBank`), and generates the
   `SoundId` enum. Re-run it any time you add/rename clips.
4. On the **AudioManager**, assign a bank to **Startup Banks** so its sounds are available — drop in
   **MasterBank** for "everything loaded" (or use a `SceneAudioBank` component for per-scene loading).

You then assign **SoundDefinition** assets to the sender components below — no clip names, no strings.

> **Auto-generated vs your own definitions.** The generator creates one definition per clip (in
> `SoundDefinitions/`) and owns them. To **group specific clips** into one sound (a primary clip +
> variations) or curate your own, make a definition by hand via **Create → BMS AudioManager → Sound
> Definition** and keep it in a separate folder (e.g. `SoundDefinitions-User/`) so the generator leaves
> it alone. Assign it to senders directly, and add it to a SoundBank to make it registry-available.
> Note: the generator only *adds/updates* — it never deletes definitions for clips you rename or
> remove, so tidy up stale auto-generated ones yourself.

---

## AudioEventSender - Play a Track from a GameObject

Use this to play, stop, or pause a BGM / Ambient / Dialogue track when a player enters a zone, or when any GameObject activates.

**Add component:** `Add Component → AudioEventSender`

### Inspector Fields

| Field | What it does |
|---|---|
| **Audio Track Type** | Which track (channel) to play on: BGM, Ambient, Dialogue, Aux1, or Aux2 |
| **Sound Definition** | **Required** - the sound asset to play (assign a generated SoundDefinition) |
| **Use Definition Defaults** | Pull volume/pitch/loop/spatial/fade from the definition. Untick to use the fields below |
| **Volume** | Playback volume 0–1 (used when *Use Definition Defaults* is off) |
| **Pitch** | Playback pitch (1 = normal, 0.5 = half speed, 2 = double) |
| **Spatial Blend** | 0 = 2D stereo, 1 = full 3D positional |
| **Loop** | Keep looping until stopped |
| **Play On Enabled** | Auto-play when the GameObject is enabled/activated |
| **Fade Type** | `FadeInOut` - fade out old then fade in new; `Crossfade` - overlap simultaneously |
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
| **On Enter Action** | What happens when the target enters - `Play`, `Stop`, `Pause`, `AdjustParameters`, or `None` |
| **On Exit Action** | What happens when the target exits - same options as above |

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

## AudioEventSenderSFX - Play a Sound Effect from a GameObject

Use this for footsteps, pickups, ambient bird chirps, buttons, or any one-shot / looped SFX.

**Add component:** `Add Component → AudioEventSenderSFX`

### Inspector Fields

| Field | What it does |
|---|---|
| **Sound Definition** | **Required** - its `clip` + `variations` form the random pool. One is picked per play |
| **Use Definition Defaults** | Pull volume/pitch/jitter/chance/loop/3D/delay from the definition. Untick to use the fields below |
| **Play On Enabled** | Auto-play when the GameObject activates |
| **Volume** | 0–1 (used when *Use Definition Defaults* is off) |
| **Pitch** | 1 = normal |
| **Randomise Pitch** | Vary pitch slightly on each play |
| **Pitch Range** | How much to vary pitch when Randomise Pitch is on |
| **Spatial Blend** | 0 = 2D, 1 = full 3D |
| **Loop** | Keep looping |
| **Percentage Chance To Play** | 0–100 - e.g. 40 means it only plays 40% of the time |
| **Randomise Delay** | Randomise the wait before playing |
| **Event Delay** | Fixed delay in seconds (used if Randomise Delay is off) |
| **Attach Sound To This Transform** | SFX follows this GameObject's position |
| **Transform To Attach To** | Attach to a different transform instead |
| **Use Custom 3D Settings** | Override the default min/max hearing distances |
| **Min Distance / Max Distance** | Audible range when Use Custom 3D Settings is on |
| **Use Custom Position** | Play at a fixed world position instead of a transform |
| **Custom Position** | The world position to play at when above is enabled |
| **Event Name** | Optional label for debug logs |

> **Tip:** with *Use Definition Defaults* on, configure variation/pitch/chance/3D **on the
> SoundDefinition asset** so every sender and script call reuses them. Untick only for a one-off
> per-placement override.

### Collision / Trigger Zone Setup

Same as AudioEventSender above - set **Collision Type**, **Target Tag**.

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

## AudioTrackParameterDisplay - Monitor Track States

Attach to any GameObject to see live track states (BGM, Ambient, Dialogue, Aux1, Aux2) in the Inspector while the game runs.

**Add component:** `Add Component → AudioTrackParameterDisplay`

Shows for each track: current state, clip name, volume, pitch, spatial blend, loop, and playback progress. Useful for debugging without opening the AudioTrack component directly.

**Context menu:** Right-click the component → **Refresh Parameters** to force an update.

---

## SFXDebugDisplay - Monitor Active SFX

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

The **Mixer Groups** section has one slot per track type plus one for SFX. Drag your mixer groups from the Audio Mixer window into each slot. Each `AudioSource` created at runtime will have its `outputAudioMixerGroup` assigned automatically - the shared prefab's baked-in output is overridden.

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
6. Drag your cave-ambient **Sound Definition** into the **Sound Definition** slot
7. Leave **Use Definition Defaults** on (volume/fade/etc. come from the definition), or untick to set **Volume** `0.7`, **Fade Type** `Crossfade`, **Fade Duration** `3` here
8. Set **Collision Type** → `Trigger`, **Target Tag** → `Player`
9. Set **On Enter Action** → `Play`, **On Exit Action** → `Stop`
10. Optionally tick **Attach To This Transform** if you want the ambient to be positionally 3D

Done - no code written. (Make sure the definition's bank is loaded - MasterBank in the AudioManager's Startup Banks, or a SceneAudioBank.)

---

## Typical SFX Zone Setup (Step by Step)

**Goal:** Play a random footstep crunch sound when the player walks over gravel.

1. First, make a **Sound Definition** for the footsteps: put the three step clips on one definition
   (primary `clip` + two `variations`), tick **Randomise Pitch** (`Pitch Range` `0.15`), set
   **Percentage Chance To Play** `70`, **Spatial Blend** `1`, and **Min/Max Distance** `1`/`10` on it.
   (The generator already creates one definition per clip - either add the extra clips to one as
   variations, or create a definition manually via `Create → BMS AudioManager → Sound Definition`.)
2. Create an empty GameObject, name it `GravelZone`
3. Add a **Box Collider**, tick **Is Trigger**, size it to the gravel patch
4. Add component **AudioEventSenderSFX**
5. Drag the footsteps **Sound Definition** into the **Sound Definition** slot
6. Leave **Use Definition Defaults** on — all the variation/pitch/chance/3D settings come from the definition
7. Set **Collision Type** → `Trigger`, **Target Tag** → `Player`

Done. One configured definition now drives the variety; the same definition works identically from code (`AudioEvent.PlaySFX(...)`).
