# Changelog

All notable changes to **BMS Audio Manager** are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Readiness fixes for dropping the package into new projects. No API changes.

### Fixed
- **Projects without `com.unity.splines` no longer fail to compile.** The spline scripts are in their own
  assembly (`BMS.AudioManager.Splines`) that only builds when the Splines package is installed.
- **Spline followers no longer go the long way round closed splines.** When the target crossed the first
  knot (where position wraps from 1 back to 0) the follower travelled the whole loop the wrong way. Both
  followers now always take the shortest route, and stay on their current part of the spline when two parts
  are about equally close instead of flip-flopping.
- **Leaving the inside of a closed spline no longer snaps the follower back to where the target entered.** It
  now rejoins the spline at the exit point, easing there at a capped speed (new **Rejoin Max Speed** and
  **Rejoin Smooth Time** settings on both followers) instead of rushing across - the old exit reached ~30 m/s
  and caused an audible loudness bump. `SplineFollower` also no longer restarts its "entering" transition every
  frame while the target is inside.
- A track stopped instantly (e.g. `AudioEvent.StopTrack(AudioTrackType.BGM)`) no longer stays silent the
  next time it plays with a fade.
- `PlayTrack` now works while a track is paused, fading to/from pause, or mid volume-adjust (previously ignored).
- `AdjustTrackVolume` / `AdjustTrack` keep the track's current spatial blend, loop and pitch (previously reset
  3D tracks to 2D and made one-shot dialogue loop).
- SFX pitch randomisation is correct when pitch isn't 1 (pitch was applied twice).
- Percent chance: 0% now never plays (previously 1%).
- A duplicate AudioManager (prefab in every scene) no longer receives events before it destroys itself.
- The generator never reuses a `SoundId` value, even after a definition is deleted, and warns about
  unrecognised category folders or finding no clips.
- No obsolete-API warnings in Unity 6; zone test keys and the demo controller no longer throw in projects
  using only the new Input System.

### Changed
- Zone prefabs ship with **test mode off** (test keys could fire during gameplay, e.g. S stopped all SFX).
- The AudioManager prefab loads **MasterBank** at startup so `AudioEvent.Play(SoundId)` works out of the box,
  and ships with debug logging off.
- Setup mistakes (bank not loaded, missing Sound Definition, no AudioManager in the scene) always log a
  warning, regardless of the debug-logging toggle.
- `AudioEventSender.useDefinitionDefaults` now defaults to on, matching `AudioEventSenderSFX`; zone Stop/Pause
  use the definition's fade settings when it's on.
- A track zone that is disabled or unloaded only stops its channel if it started the playback.
- `SplineFollower`: removed `directionBias` (part of the old workaround) and `exitTransitionSpeedMultiplier`
  (replaced by the rejoin settings); `equidistantThreshold` now sets how much closer another part of the
  spline must be before the follower switches to it.

## [3.1.0] - 2026-06-17

Major, breaking release (vs 2.2.0). Clip loading moves off `Resources` + magic strings onto
**asset-based SoundDefinitions**, grouped into loadable **SoundBanks**, and addressed by a generated,
compile-safe **`SoundId`** enum. The runtime no longer references `Resources` at all.

### Added
- **`SoundDefinition`** ScriptableObject — wraps an `AudioClip` (+ optional `variations`) with its
  default playback parameters (volume, pitch, loop, spatial blend, fade type/duration/target).
- **SFX Variation & 3D** fields on `SoundDefinition` — `randomizePitch`/`pitchRange`,
  `volumeRange`, `percentChanceToPlay`, `randomizeDelay`/`delay`, `minDistance`/`maxDistance`, plus
  `NextSfxVolume()` / `NextSfxDelay()` helpers. A fully-configured definition plays with full variety
  from a single call.
- **`SoundBank`** ScriptableObject — a named list of definitions and the unit of loading.
- **`AudioRegistry`** — runtime catalogue of loaded definitions with **ref-counted** bank load/unload
  (a definition shared by two loaded banks survives until the last unloads).
- **`SceneAudioBank`** component — loads banks on enable, unloads on disable (per-scene availability).
- **`AudioManager.startupBanks`** — banks loaded into the registry on `Awake`.
- **`SoundId`** generated enum — typed, string-free keys (stable id values) for every definition;
  resolved through the registry.
- **`SoundGeneratorSettings`** asset — configurable source/output folders for the generator
  (auto-created with defaults on first run).
- **Generator** (`BMS AudioManager → Generate Sound Definitions`) — mirrors a configurable audio
  folder into definitions, one bank per category + a `MasterBank`, and emits `SoundId.cs`. Idempotent
  and **rename/move-safe** (definitions matched to clips by GUID; stable ids preserved).
- **Hand-authored (user) definitions** — definitions you create in `SoundGeneratorSettings.userDefinitionsRoot`
  (default `SoundDefinitions-User/`) for grouping specific clips are picked up by the generator: each gets a
  `SoundId`, and they're added to the `MasterBank` plus a dedicated `UserBank`. The generator never
  creates, moves, or deletes them.
- **`AudioEvent` SoundDefinition & SoundId overloads** — `Play(SoundId)` smart-dispatch (routes to
  track vs SFX by category), plus `PlayTrack` / `PlaySFX` / `PlaySFX3D` / `PlayLoopedSFX` / `StopTrack`
  taking a `SoundId` or `SoundDefinition`.
- **3D rolloff for spatialised tracks** — `minDistance`/`maxDistance` now apply to 3D-attached tracks,
  not just SFX.
- **Variation support for tracks** — track playback picks a random clip from `clip + variations`
  per play (single-clip definitions are unaffected).
- **`AudioManagerUsageExample`** — a single click-to-test reference component (on-screen buttons +
  right-click `[ContextMenu]` entries) that exercises the whole API and doubles as an annotated
  copy-paste cheat sheet.

### Changed
- **Sender components require a `SoundDefinition`.** `AudioEventSender` and `AudioEventSenderSFX` gained
  a `useDefinitionDefaults` toggle; with it on, all playback params come from the definition.
- **SFX parameters live on the definition.** Pitch/volume jitter, percent-chance, delay, and 3D
  distances are read from the `SoundDefinition` instead of being passed per call.
- **Build size:** audio is now reference-driven (definitions/banks), so unused clips are stripped from
  builds; source clips no longer need to live under `Resources`.

### Removed
- **Runtime `Resources` loading** — `LoadAudioResources`, the per-type clip dictionaries, the
  `GetBGMClip`/`GetAmbientClip`/… accessors, and the inspector name lists.
- **String/index playback API** — `AudioEvent.PlayTrack(type, "name")`, `PlayTrack(type, index)`,
  `PlaySFX("name")`, `PlaySFX(string[])`, `PlaySFX3D(string,…)`, `PlayLoopedSFX(string,…)`,
  `PlayRandomSFX(…)`. Replaced by the `SoundId` / `SoundDefinition` overloads.
- `trackName` / `trackNumber` fields from `AudioEventSender` and `sfxName` from `AudioEventSenderSFX`.

### Fixed
- Scripted track stop is now symmetric with play via `AudioEvent.StopTrack(SoundDefinition)` (uses the
  definition's fade duration/target instead of a volume-only default).
- Generator no longer creates duplicate definitions when a source clip is renamed or moved
  (GUID-based identity; the matching definition updates in place and its `SoundId` value stays stable).

### Migration
1. Run **BMS AudioManager → Generate Sound Definitions** to create the definitions, banks, and
   `SoundId` enum (auto-creates `SoundGeneratorSettings`; point `audioSourceRoot` at your clips).
2. Assign **MasterBank** to `AudioManager.startupBanks` for parity with the old "everything available"
   behaviour, or use `SceneAudioBank` for per-scene loading.
3. Replace string/index calls with `SoundId` (e.g. `AudioEvent.Play(SoundId.MainTheme)`); assign the
   `Sound Definition` slot on existing sender components.
4. Move source clips out of `Resources` (optional but recommended) and update `audioSourceRoot`.

### Deferred (not in this release)
- **Addressables provider** — `SoundDefinition.GetClip()`/`GetClipPool()` is the seam. Until added,
  `AudioRegistry.UnloadBank` controls *availability*, not memory (clips stay resident).

## [2.2.0]

Previous release: Resources-based clip loading with the string/index `AudioEvent` API, 3-source
crossfading, 5 tracks, SFX system, spline audio, and inspector audio zones.
