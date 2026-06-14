# Changelog

All notable changes to **BMS Audio Manager** are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-06-13

Major, breaking release. Clip loading moves off `Resources` + magic strings onto **asset-based
SoundDefinitions**, grouped into loadable **SoundBanks**, and addressed by a generated, compile-safe
**`SoundId`** enum. The runtime no longer references `Resources` at all.

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
- **`AudioEvent` SoundDefinition & SoundId overloads** — `Play(SoundId)` smart-dispatch (routes to
  track vs SFX by category), plus `PlayTrack` / `PlaySFX` / `PlaySFX3D` / `PlayLoopedSFX` / `StopTrack`
  taking a `SoundId` or `SoundDefinition`.
- **3D rolloff for spatialised tracks** — `minDistance`/`maxDistance` now apply to 3D-attached tracks,
  not just SFX.
- **Variation support for tracks** — track playback picks a random clip from `clip + variations`
  per play (single-clip definitions are unaffected).
- **`SoundDefinitionScriptingTest`** example component.

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
