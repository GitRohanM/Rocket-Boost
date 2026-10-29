# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Pure domain layer** (`Assets/Scripts/Core`, assembly `RocketBoost.Core`) with no `UnityEngine`
  dependency: `MissionTimeFormatter`, `MissionRankCalculator`, `BestTimeStore`, `LevelProgression`,
  and the `IKeyValueStore` persistence seam.
- **Edit-mode test suite** (`Assets/Tests/EditMode`, 62 cases) covering timer formatting, rank
  thresholds, best-time/record handling, and level wrap-around.
- **Assembly definition** for the pure domain layer (`RocketBoost.Core`, with
  `noEngineReferences: true`) and for the test assembly, so the compiler enforces the layering.
  Gameplay code stays in Unity's predefined `Assembly-CSharp` assembly: DOTween deletes any
  `.asmdef` placed inside `Assets/DOTween`, and its extension methods only resolve when compiled
  into the same assembly as their callers.
- Removed `com.besty.unity-skills` from `testables` in `Packages/manifest.json`. The package ships
  ~1,700 editor tests of its own, which ran with the game's suite and produced 30 unrelated
  failures on a clean checkout.
- `PlayerPrefsKeyValueStore` — the single place that talks to `PlayerPrefs`.
- `GameTags` constants for the `Friendly` / `Finish` / `Player` tag names.
- `README.md`, `docs/ARCHITECTURE.md`, and this changelog.
- `.editorconfig` and `.gitattributes` (Unity SmartMerge rules, binary asset declarations).
- Static-reset hooks on the singletons, required because **Domain Reload is disabled** in this
  project.

### Changed

- `GameManager.OnLevelCompletedEvent` is now `OnLevelCompleted` and publishes a `GameManager.LevelResult`
  carrying `Time`, `BestTime`, `Attempts`, and an explicit `IsNewRecord` flag.
- Best-time lookups return `bool` (`TryGetBestTime`) instead of returning `0` for "no record" while
  internally using `float.MaxValue`.
- HUD and debrief timers are formatted by `MissionTimeFormatter` (culture-invariant, truncating,
  clamped), replacing three copies of `$"{seconds:00.00}"`.
- HUD text updates are change-driven and use TMP format overloads, so the timer no longer allocates
  a string every frame.
- Rank colours moved to the UI layer; `MissionRankCalculator` returns a rank and a title only.
- Collision handling uses a coroutine instead of `Invoke`, is re-entrancy guarded, and cancels cleanly
  when disabled.
- New scripts use LF line endings (`.editorconfig`, `EditorSettings.m_LineEndingsForNewScripts`).
- `GameManager` validates scene indices and logs a duplicate-instance warning with the scene name.

### Fixed

- **Escape quit the game while pausing.** In Scene 3, `ApplicationQuit` and `InGameHUD` both handled
  `Esc` on the same frame, so the first pause closed the application. `ApplicationQuit` now stands
  down whenever a gameplay screen owns the key.
- **Success and crash sounds played twice**, once on the rocket's `AudioSource` and once via
  `AudioClip.PlayClipAtPoint` at the camera, which doubled the volume and could clip. One source is
  used now, with `[Range(0,1)]` volumes.
- **Oscillating obstacles could never move** when `useDOTween` was enabled on a dynamic `Rigidbody`:
  the tween was skipped and both movement paths were gated on `!useDOTween`. It now falls back to
  physics movement and warns.
- **"New record" was inferred by comparing the run time to an already-updated best time**, which
  worked only because the update happened in a specific order. The record flag is explicit now.
- `Movement` no longer calls `AudioSource.Play()` every fixed step when no engine clip is assigned,
  and it reports missing `InputAction` references instead of failing silently.
- `UIButtonJuice` no longer attempts `Resources.Load("UI_Hover")` / `("UI_Click")` for assets that
  do not exist; it warns when clips are unassigned.
- Pausing no longer leaves the HUD controls hint visible behind the pause panel.
- `LevelCompletionUI` unsubscribes and clears its static instance on destroy.

### Removed

- `GameManager.GetBestTime(int)`, superseded by `TryGetBestTime(int, out float)`.
- Unused `Resources` lookups in `UIButtonJuice`.

## [0.1.0]

- Initial arcade prototype: three levels, menu, HUD, pause, debrief screen, DOTween UI polish.
