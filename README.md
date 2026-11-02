# Rocket Boost 🚀

An arcade precision physics flight game built in **Unity 6 (6000.0.34f1)** with the Universal Render Pipeline (URP). Pilot a high-thrust rocket through challenging sectors filled with kinetic obstacles, manage inertia and rotational momentum, stick precision pad landings, and beat sector time records.

---

## 🎮 Features

### Gameplay & Physics
- **Momentum-Based Flight:** Dual-thruster rotational torque and primary vector propulsion with tuned inertial damping.
- **Kinetic Hazards:** Dynamic oscillating barriers, rotating hazard gates, and precision navigation gauntlets.
- **Precision Landing Detection:** Contact angle and descent velocity validation on launch and landing pads.
- **Time Attack & Ranking:** High-precision sector timer with dynamic **S / A / B / C** rank evaluations based on clear time and attempt count. Best times are automatically saved locally.

### UI & Audio Polish
- **Sci-Fi Theme:** Cohesive UI design system with dynamic HUD, sector selection, settings, and mission debrief screens.
- **Independent UI Animations:** Custom tween system running on unscaled time, ensuring silky-smooth menus and animations while the game physics is paused.
- **Audio Architecture:** Dedicated audio mixer with separate Master, Music, and SFX channels, featuring custom menu ambience and responsive sound effects.

### Engineering Highlights
- **Decoupled Core Logic:** Game scoring, rank calculations, timer formatting, and progression logic live in `RocketBoost.Core`—a pure C# assembly with zero engine dependencies (`noEngineReferences: true`).
- **62 Automated Unit Tests:** Comprehensive EditMode test coverage in `RocketBoost.Tests.EditMode` verifying timer truncation, culture invariance, ranking edge cases, and high-score persistence.
- **Zero-Allocation HUD:** Change-driven TextMeshPro formatting avoids per-frame string allocations during flight.
- **Fast Playmode Ready:** Safe for Unity's *Enter Play Mode Options* with explicit static lifecycle resets across domain reloads.

---

## 🕹️ Controls

| Action | Primary Key | Alternate Key | Description |
| :--- | :--- | :--- | :--- |
| **Main Boost** | `Space` | - | Engages main thrusters & particle plume |
| **Steer Port (Left)** | `A` | `Left Arrow` | Rotates rocket counter-clockwise |
| **Steer Starboard (Right)** | `D` | `Right Arrow` | Rotates rocket clockwise |
| **Pause / Resume** | `Escape` | - | Pauses physics and opens mission menu |
| **Quick Restart** | `R` (in menus) | UI Button | Respawns rocket at the launch pad |

*Debug keys available in Editor / Development builds: `L` (advance level), `C` (toggle invulnerability).*

---

## 🚀 Getting Started

### Prerequisites
- **Unity Hub**
- **Unity 6 (6000.0.34f1)** with Universal Render Pipeline (URP)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/GitRohanM/Rocket-Boost.git
   ```
2. Open **Unity Hub**, click **Add → Add project from disk**, and select the `Rocket-Boost` directory.
3. Open the project with **Unity 6000.0.34f1**.
4. In the Project window, navigate to `Assets/Scenes/MainMenu.unity` and press **Play**.

### Running the Unit Tests
1. In the top menu, go to **Window → General → Test Runner**.
2. Select the **EditMode** tab.
3. Click **Run All** to execute all 62 unit tests.

---

## 📁 Project Structure

```
Rocket-Boost/
├── Assets/
│   ├── _Project/               # Custom UI system, fonts, theme data, and audio
│   │   ├── Audio/              # GameAudioMixer & sound assets
│   │   └── UI/                 # Screen prefabs, widgets, and UI management scripts
│   ├── Audio/                  # Core gameplay SFX (thruster, explosion, success)
│   ├── Materials/              # Shaders, skybox, and glow materials
│   ├── Prefabs/                # Player rocket, pads, hazards, cameras, volume profiles
│   ├── Scenes/                 # MainMenu, Scene 1, Scene 2, Scene 3
│   ├── Scripts/
│   │   ├── Core/               # Pure C# domain logic (RocketBoost.Core)
│   │   └── *.cs                # Movement, CollisionHandler, GameManager, Oscillator
│   └── Tests/EditMode/         # Automated NUnit test suite (RocketBoost.Tests.EditMode)
└── Packages/                   # Package manifest & dependencies
```

---

## 📜 Credits & Licenses

- **Development:** Rohan M.
- **Base Assets:** GameDev.tv 3D models and textures.
- **Audio:** Kenney UI Audio & Interface Packs (CC0 1.0).
- **Fonts:** Orbitron (Matt McInerney) & Rajdhani (Indian Type Foundry) under SIL Open Font License 1.1.
- Detailed licensing breakdown available in [`Assets/CREDITS.md`](Assets/CREDITS.md).
