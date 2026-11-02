# Credits & Asset Licenses

Rocket Boost ships only assets that are CC0, OFL, MIT, or generated in-project. Every entry
below records the author, source, license and where the asset is used.

## Fonts

| Asset | Author | License | Source | Used in |
| --- | --- | --- | --- | --- |
| Orbitron (Bold) | Matt McInerney | SIL Open Font License 1.1 | https://fonts.google.com/specimen/Orbitron | Display font: logo, screen titles, timer, large numbers. `Assets/_Project/UI/Fonts/Orbitron-Bold.ttf` → `TMP_Orbitron_Display.asset` |
| Rajdhani (Medium) | Indian Type Foundry | SIL Open Font License 1.1 | https://fonts.google.com/specimen/Rajdhani | Body font: buttons, labels, descriptions, settings. `Assets/_Project/UI/Fonts/Rajdhani-Medium.ttf` → `TMP_Rajdhani_Body.asset` |

OFL permits embedding and commercial use; the fonts are not sold on their own.

## Audio

| Asset | Author | License | Source | Used in |
| --- | --- | --- | --- | --- |
| UI Audio pack (rollover2, click1) | Kenney | CC0 1.0 | https://kenney.nl/assets/ui-audio | `SFX/ui_hover.ogg`, `SFX/ui_click.ogg` |
| Interface Sounds pack (back_001, confirmation_001) | Kenney | CC0 1.0 | https://kenney.nl/assets/interface-sounds | `SFX/ui_back.ogg`, `SFX/ui_confirm.ogg` |
| Sci-Fi Sounds pack (explosionCrunch_001) | Kenney | CC0 1.0 | https://kenney.nl/assets/sci-fi-sounds | `SFX/ui_crash.ogg` |
| Music Jingles pack (jingles_PIZZI10) | Kenney | CC0 1.0 | https://kenney.nl/assets/music-jingles | `SFX/ui_level_complete.ogg` |
| Menu Ambience Loop | Rohan M. | Original Project Asset | Custom synthesized audio | `Music/MenuAmbience_Loop.wav` — looping main-menu music |

Kenney packs are released under CC0 1.0 (public domain). No attribution is required; it is
recorded here anyway.

## Sprites, icons and effects

All UI sprites, icons, panels, stars, starfield tiles, nebula, vignette, scanlines and
confetti shapes are generated procedurally by `Assets/_Project/UI/Scripts/Editor/UISpriteFactory.cs`.
They are original project assets, free to use and modify.

## Third-party code

| Asset | Author | License | Used in |
| --- | --- | --- | --- |
| DOTween (free) | Demigiant | DOTween free license (Asset Store) | Pre-existing project dependency; kept for scene presentation (`MenuShowcase`). The new UI uses the in-house `UITween` helper. |
| Unity Input System, URP, uGUI/TextMeshPro, Cinemachine | Unity Technologies | Unity Companion License | Engine packages |

## Music provenance

`MenuAmbience_Loop.wav` is an original 48-second seamless loop composed from sine partials,
filtered noise, a circular delay and a synthetic reverb impulse. No samples or third-party
recordings are used, so no external license applies.
