# Architecture

## Layering

```
                 ┌──────────────────────────────┐
                 │    Assembly-CSharp-Editor    │  Editor-only tools, menus, scaffolding
                 └──────────────┬───────────────┘
                                │
                 ┌──────────────▼───────────────┐
   Unity types → │      Assembly-CSharp         │  MonoBehaviours: flow, HUD, player
                 └──────────────┬───────────────┘
                                │  depends on
                 ┌──────────────▼───────────────┐
   No Unity    → │        RocketBoost.Core      │  Pure C#: rules, formatting, storage
                 └──────────────────────────────┘
```

The arrow points one way only. `Core` has `noEngineReferences: true`, so it cannot accidentally
pick up `UnityEngine` and will keep compiling in a plain .NET test project, and its tests run in
`RocketBoost.Tests.EditMode` without touching a scene.

**Why gameplay code is not in its own assembly.** It was tried, and it does not work with DOTween in
this project's configuration: DOTween's editor tooling deletes any `.asmdef` inside `Assets/DOTween`,
and DOTween's extension methods (`DOAnchorPos`, `DOFade`, `DOColor`, …) live in `.cs` files there.
They must be compiled into the same assembly as the code calling them, which is
`Assembly-CSharp`. The DOTween utility panel can generate asmdefs for DOTween
(`createASMDEF` in `Assets/Resources/DOTweenSettings.asset`), but that is a separate migration.

**Why this split.** Everything a player notices about timing — how a duration is rendered, whether
a run counts as a record, which rank a run earns, what "next level" means on the last scene — is
arithmetic. Arithmetic is cheap to test and expensive to debug by playing the game. The
MonoBehaviours keep the Unity concerns: input, physics, tweening, audio, scene loading.

## Layer contents

| Layer | Type | Responsibility |
| --- | --- | --- |
| `RocketBoost.Core` (asmdef) | static / plain classes | `MissionTimeFormatter`, `MissionRankCalculator`, `BestTimeStore`, `LevelProgression` |
| `Assembly-CSharp` | `MonoBehaviour` | `GameManager`, `Movement`, `CollisionHandler`, `InGameHUD`, `LevelCompletionUI`, `MainMenuUI`, `ApplicationQuit`, `Oscillator`, `Rotator`, `MenuShowcase`, `UIGlowPulse`, `UIButtonJuice`, `PlayerPrefsKeyValueStore`, `GameTags` |
| `Assembly-CSharp-Editor` | static editor tools | `RocketBoostSetupEditor`, `SciFiUIGenerator`, `AllScenesSnapshotter`, `Scene2Inspector` |
| `RocketBoost.Tests.EditMode` (asmdef) | NUnit | Tests for `Core` only — no scene, no play mode, no frame timing |

## Session state machine

`GameManager` owns the state and survives scene loads via `DontDestroyOnLoad`.

```
                    scene loaded (menu)
        ┌────────────────────────────────────────┐
        │                                        ▼
   ┌──────────┐   scene loaded (level)     ┌──────────┐   Esc    ┌────────┐
   │ MainMenu │ ─────────────────────────► │ Playing  │ ───────► │ Paused │
   └──────────┘                            └──────────┘ ◄─────── └────────┘
                                                │  Esc
                              ┌─────────────────┼──────────────────┐
                              │ success         │ crash            │
                              ▼                 ▼                  │
                       ┌──────────────┐   ┌──────────┐              │
                       │ LevelComplete│   │ GameOver │              │
                       └──────────────┘   └──────────┘              │
                              │                 │                  │
                              └──── load next / reload ─────────────┘
```

Rules enforced by the class:

- `TogglePause` / `SetPaused` only act from `Playing` or `Paused`, so a crash or the debrief screen
  can never leave `Time.timeScale` frozen at `0`.
- `OnPlayerSuccess` and `OnPlayerCrashed` only act from `Playing`, so a double collision cannot
  count twice.
- Every scene/level load resets `Time.timeScale` to `1` and the level timer to `0`.

**Domain reload is disabled** in this project (*Project Settings → Editor → Enter Play Mode
Options*). Static fields therefore survive a play-mode exit, so both singletons clear themselves in
`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`. Any new static state needs the same
treatment.

## Level completion flow

```
CollisionHandler (finish pad)
  └─ disables Movement          (so OnDisable does not cut the success sting)
  └─ plays particles + one-shot SFX
  └─ GameManager.OnPlayerSuccess()
       └─ state → LevelComplete
       └─ BestTimeStore.Submit(levelId, time)  →  isNewRecord, bestTime
       └─ OnLevelCompleted(LevelResult)
            └─ LevelCompletionUI.ShowLevelCompleted(result)
                 ├─ MissionRankCalculator.Evaluate(attempts, time)
                 ├─ MissionTimeFormatter.Format(bestTime)
                 └─ DOTween reveal sequence (unscaled time, so it plays while paused)
  └─ if no debrief UI in the scene: advance after `delay`
```

`LevelResult` carries the record flag explicitly. The UI used to infer "new record" by comparing
the run time to the best time *after* the best time had already been overwritten, which was only
correct by accident.

## Persistence

`BestTimeStore` is constructed once by `GameManager` and wraps an `IKeyValueStore`:

```
BestTimeStore → IKeyValueStore → PlayerPrefsKeyValueStore → PlayerPrefs
```

- Key format: `RocketBoost_BestTime_Level_<buildIndex>`.
  **The prefix must not change** without a migration — it would silently wipe every player's records.
- Level identity is the build index as an invariant-culture string. It is stable as long as the
  build-settings order is stable.
- `Submit` writes and flushes only when the run beats the stored value; corrupted (negative, NaN,
  infinite) stored values are treated as "no record" instead of poisoning comparisons.

## Testing strategy

Tests live in `Assets/Tests/EditMode` and reference `Core` only.

| Suite | What it pins down |
| --- | --- |
| `MissionTimeFormatterTests` | Truncation vs rounding, float drift at hundredths, 99:59.99 clamp, negative/NaN/infinite input, invariant culture |
| `MissionRankCalculatorTests` | Exact S/A/B/C boundaries, the exclusive 20s threshold, attempt ordering, invalid input |
| `BestTimeStoreTests` | First run, slower/faster/equal runs, per-level isolation, save-flush count, corrupted values, key compatibility |
| `LevelProgressionTests` | Wrap from last level to menu, invalid current index, empty build settings |

Not covered yet: MonoBehaviour behaviour (would need play-mode tests and a scene fixture) and
visual/tween behaviour. The UI classes are intentionally thin so that the untested surface stays
small.

## Deliberate omissions

| Topic | Decision |
| --- | --- |
| ScriptableObject level catalogue | Right long-term fix for hard-coded sector titles and per-level tuning, but it needs new assets wired into three scenes. Deferred to keep this change reviewable. |
| Object pooling | Three levels of static geometry and two particle one-shots; no runtime spawning to pool. |
| Addressables | All content fits in memory; setup cost is not justified yet. |
| Audio mixer | Would add per-channel volume and a settings screen. Worth doing when a settings menu exists. |
| CI | The batch-mode test command is documented in the README; a workflow needs Unity license secrets. |
