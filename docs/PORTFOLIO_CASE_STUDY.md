# Portfolio Case Study: Rocket Boost
## Engineering a Clean Architecture, Test-Driven Physics Flight Game in Unity 6

---

### 📌 Project Metadata
- **Project:** Rocket Boost
- **Role:** Lead Gameplay Programmer & Technical Architect
- **Engine & Version:** Unity 6 (`6000.0.34f1`)
- **Pipeline:** Universal Render Pipeline (URP)
- **Language / Framework:** C# 12 / .NET Standard 2.1, NUnit, DOTween
- **Target Platforms:** Windows Standalone, WebGL (HTML5 / WebAssembly)
- **Repository:** [github.com/GitRohanM/Rocket-Boost](https://github.com/GitRohanM/Rocket-Boost)

---

## 🎯 Executive Summary

**Rocket Boost** is an arcade-style precision flight game where players pilot a rocket thruster through dangerous obstacle-dense sectors, manage inertia and gravity, and execute precise landings against the clock.

While appearing straightforward on the surface, the primary engineering objective was to take a classic arcade prototype and **elevate it to production-grade enterprise software architecture**. The project demonstrates how to eliminate common Unity technical debt: decoupling business arithmetic into a pure C# engine-agnostic domain layer, establishing **62 automated EditMode unit tests**, designing an idempotent procedural UI generation pipeline, and implementing a robust session state machine with zero-allocation performance hot paths.

---

## ⚡ The Problem Statement & Engineering Challenges

In standard Unity development, prototypes frequently evolve into fragile codebases dominated by bloated "God MonoBehaviours". Rocket Boost tackled five distinct architectural and gameplay hurdles:

### 1. The Unity "Monolith Trap" & Tight Coupling
* **The Problem:** In conventional game scripts, physics forces, user input, audio playback, particle triggering, timekeeping, and UI updates are tightly coupled inside single classes. If a designer changes how best times are saved or how rank thresholds are calculated, gameplay scripts often break.
* **The Risk:** Hard coupling makes it impossible to run unit tests without instantiating scene objects, loading assets, and waiting for engine frames.

### 2. Arithmetic Determinism & Precision Drift
* **The Problem:** In precision time-attack games, milliseconds matter. Standard `Time.timeSinceLevelLoad` conversions and naive string interpolation (`$"{time:00.00}"`) suffer from floating-point precision drift, culture differences (e.g., commas vs. dots in European locales), and accidental rounding that incorrectly awards records.
* **The Risk:** Inconsistent timing, false new-record flags, and subtle bugs that cannot be reliably tested through manual play alone.

### 3. Collision Race Conditions & Audio Doubling
* **The Problem:** Physics collision callbacks (`OnCollisionEnter`) in Unity can fire multiple times across consecutive frames during high-speed contact or multi-contact physics meshes. In the original prototype, this caused victory fanfares to trigger twice, explosion sounds to clip audio channels, and scene transitions to race each other.
* **The Risk:** Jarring audio pops, distorted audio volume, and broken game progression loops.

### 4. UI Technical Debt Across Multiple Scenes
* **The Problem:** Traditional Unity UI (uGUI) workflows require manually configuring Canvas hierarchies across each scene (`MainMenu`, `Scene 1`, `Scene 2`, `Scene 3`). Modifying a button style, color palette, or font required manually updating dozens of prefabs and scene hierarchies, leading to visual drift and synchronization bugs. Furthermore, freezing `Time.timeScale = 0` to pause physics broke standard UI animations.
* **The Risk:** High maintenance cost, inconsistent user experience, and frozen pause menus.

### 5. Memory Allocation & Garbage Collection Spikes
* **The Problem:** Formatting string timers and score displays inside `Update()` every frame generates heap memory allocations. In Unity's garbage-collected runtime (especially in WebGL/browser environments with limited memory), frequent garbage collection cycles produce noticeable micro-stutters and frame drops.
* **The Risk:** Ruined player immersion and input drops during critical precision flight maneuvers.

---

## 🛠️ Architectural & Technical Solutions

```
┌─────────────────────────────────────────────────────────────────┐
│                     Assembly-CSharp-Editor                      │
│   • UIOverhaulBuilder (Idempotent 1-click rebuild)              │
│   • UIScreenFactory & UISpriteFactory (Procedural Generation)   │
│   • Scene snapshot & diagnostic tools                           │
└────────────────────────────────┬────────────────────────────────┘
                                 │ Generates & inspects
┌────────────────────────────────▼────────────────────────────────┐
│                   Assembly-CSharp / UI Layer                    │
│   • GameManager (Session FSM & pause lifecycle)                 │
│   • Movement (Dual-thruster vector physics)                     │
│   • CollisionHandler (Re-entrant guarded transitions)           │
│   • UIManager & UIScreen (Modal stack & navigation)             │
│   • UITween (UnscaledDeltaTime tween runner)                    │
│   • PlayerPrefsKeyValueStore (Persistence Seam)                 │
└────────────────────────────────┬────────────────────────────────┘
                                 │ Depends on (Unidirectional)
┌────────────────────────────────▼────────────────────────────────┐
│                        RocketBoost.Core                         │
│                   (noEngineReferences: true)                    │
│   • MissionTimeFormatter (Culture-invariant truncation)         │
│   • MissionRankCalculator (S/A/B/C tier evaluation)             │
│   • BestTimeStore (Corrupt-resilient record keeping)            │
│   • LevelProgression (Boundary & wrap-around math)              │
│   • IKeyValueStore (Storage abstraction)                        │
└─────────────────────────────────────────────────────────────────┘
                                 ▲
                                 │ Verified by
┌────────────────────────────────┴────────────────────────────────┐
│                   RocketBoost.Tests.EditMode                    │
│   • 62 NUnit automated unit tests running in pure C# memory     │
└─────────────────────────────────────────────────────────────────┘
```

### 1. Pure C# Domain Layer (`noEngineReferences: true`)
- Segregated all non-visual game logic into a standalone assembly definition (`RocketBoost.Core.asmdef`) that forbids any reference to `UnityEngine`.
- Core services like `MissionTimeFormatter`, `MissionRankCalculator`, `BestTimeStore`, and `LevelProgression` are plain, stateless, deterministic C# classes.
- Created an `IKeyValueStore` interface boundary to decouple persistence from Unity's `PlayerPrefs`, enabling mocking in automated test fixtures and seamless cloud save migration.

### 2. Comprehensive Test-Driven Quality Assurance
- Authored **62 automated EditMode unit tests** in `RocketBoost.Tests.EditMode` that execute in milliseconds without launching scenes or playmode:
  - **Time Formatting:** Verifies truncation vs. rounding, hundredths precision stability, culture-invariant dot decimal separators, and 99:59.99 overflow clamps.
  - **Rank Calculations:** Enforces strict boundary rules for S, A, B, and C ranks, factoring in completion time cutoffs (<20s) and attempt penalty formulas.
  - **Persistence Reliability:** Ensures corrupted/NaN storage values do not poison comparisons, record improvements trigger writes, and ties do not falsely mark new records.
  - **Level Progression:** Validates index wrap-arounds, last-scene-to-menu transitions, and out-of-range safety.

### 3. Idempotent Procedural UI Framework
- Replaced hand-crafted scene UI with an automated, code-driven UI builder pipeline (`Rocket Boost → UI → Rebuild UI System`):
  - **Design System (`UITheme.asset`):** ScriptableObject acting as the single source of truth for color tokens, typography (Orbitron & Rajdhani TMP font assets), button states, paddings, and audio feedback.
  - **Procedural Asset Generation (`UISpriteFactory`):** Programmatically generates pixel-perfect 9-slice rounded rectangles, panels, glows, and starfields directly in memory.
  - **Automated Scene Injection (`UIOverhaulBuilder`):** Reconstructs all 8 screen prefabs and ensures a persistent, self-healing `UIRoot` exists across all four game scenes in one click.
  - **Unscaled Motion Tweening (`UITween`):** Custom tween engine executing on `Time.unscaledDeltaTime` so menus, button juice, and debrief sequences animate smoothly while the underlying physics simulation is frozen (`Time.timeScale = 0`).

### 4. Robust Session Finite State Machine & Safety Guards
- Designed an event-driven `GameManager` managing high-level state: `MainMenu`, `Playing`, `Paused`, `LevelComplete`, and `GameOver`.
- Implemented state guards preventing invalid operations:
  - Pausing only functions while `Playing`; crashes or debrief screens can never freeze timeScale unexpectedly.
  - `CollisionHandler` utilizes a coroutine re-entrancy lock to guarantee collision callbacks execute exactly once per run.
  - Singleton lifecycle handles Unity's *Enter Play Mode Options (Domain Reload Disabled)* via `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` to prevent memory leaks across play sessions.

### 5. Zero-Allocation Telemetry & Audio Routing
- HUD mission timer updates avoid string allocations by using TextMeshPro char-buffer overloads, eliminating garbage collection pressure during flight.
- Created a comprehensive Unity `AudioMixer` asset with Master, Music, and SFX buses. Integrated a settings screen with decibel conversion math (`20 * log10(volume)`), giving players seamless, dynamic audio mixing.

---

## 📊 Key Results & Engineering Impact

| Metric / Objective | Before / Prototype | After / Rocket Boost Production |
| :--- | :--- | :--- |
| **Unit Test Coverage** | 0 tests (manual play only) | **62 automated EditMode tests (100% pass)** |
| **Engine Dependency in Core** | 100% coupled to `MonoBehaviour` | **0% (`noEngineReferences: true`)** |
| **HUD GC Allocation / Frame** | Continuous string allocations in `Update` | **0 bytes per frame (Non-allocating TMP buffer)** |
| **UI Reskin / Update Time** | Hours across multiple scenes | **1 click (~3 seconds automated generation)** |
| **Pause Animation Glitches** | UI animations froze with `timeScale=0` | **100% smooth via unscaled UITween engine** |
| **Audio Re-entrancy Bugs** | Double sound triggers & audio pops | **0 duplicate triggers (re-entrancy locks)** |

---

## 💡 Lessons Learned & Technical Retrospective

1. **Assembly Definition Boundaries with Third-Party Plugins:**
   * *Discovery:* DOTween's built-in generator scripts clashed when assembly definitions were placed inside its directory.
   * *Solution:* Maintained gameplay MonoBehaviours in `Assembly-CSharp` while strictly isolating the core domain logic into `RocketBoost.Core.asmdef`, preserving compile-time architectural integrity while retaining DOTween extension method resolution.
2. **Fast Iteration via Domain Reload Disabling:**
   * *Discovery:* Disabling Domain Reload in Unity 6 cuts playmode entry time from seconds to near-zero, but static fields persist between play sessions.
   * *Solution:* Implemented explicit static cleanup routines utilizing `SubsystemRegistration` hooks on all singleton instances (`GameManager`, `UIManager`), ensuring bug-free zero-reload development.
3. **The Power of Idempotent Tooling:**
   * *Discovery:* Manual scene-level UI editing is the leading cause of merge conflicts and UI regression in team environments.
   * *Solution:* Codifying UI hierarchy into an idempotent builder script proved that UI can be treated as code—reproducible, versionable, and consistent.

---

## 💼 Portfolio Ready Highlights

### 📋 Resume / CV Bullet Points
- **Engineered an arcade physics flight game in Unity 6 (C#)** featuring kinetic obstacles, precision thrust mechanics, and Universal Render Pipeline (URP) post-processing.
- **Architected a decoupled, pure C# domain layer** with zero engine dependencies (`noEngineReferences: true`), backed by **62 automated EditMode unit tests** validating mission arithmetic, culture-invariant time formatting, and scoring algorithms.
- **Developed an idempotent procedural UI generation pipeline** driven by a ScriptableObject design system, automating 9-slice sprite generation, TMP typography, and multi-scene screen integration in 1 click.
- **Built an unscaled tweening engine and event-driven finite state machine** supporting pause states, stacked overlays, and zero-allocation HUD performance without GC stutters.

### 🌐 30-Second Elevator Pitch (For Interviews / Website Cards)
> *"Rocket Boost is a precision physics flight game engineered in Unity 6 that bridges classic arcade game mechanics with clean enterprise architecture. By isolating core business logic into a pure C# engine-agnostic domain layer tested by 62 automated unit tests, and pairing it with an idempotent procedural UI design system, the project delivers rock-solid determinism, zero garbage collection stutters, and frictionless iteration across desktop and WebGL platforms."*
