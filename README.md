# Marble Sort

## Running

- The game must be played in **iOS build mode** at **iPhone 11 Pro Portrait** resolution.

## How to Play

1. Tap the **magnet icon** to start playing.
2. On the screen that opens, tap the **blue box** and activate the **magnet booster**.
3. To play again, tap the **magnet button** once more to reset the game.

---

## What Changed from the Base Project

### ScriptableObject-Driven Game Feel

Every game feel parameter is now managed through ScriptableObject profiles. Decoupled from code, editable directly from the Inspector, swappable at runtime — no code changes needed to try a new feel. Designers can iterate independently, and different profiles can be A/B tested live.

### Mid-Core → Casual VFX Pivot

The visual language was deliberately shifted to casual: mid-core VFX complexity was replaced with juicy feedback loops built for a broad audience. Every interaction is now a mini reward — squash, pop, shake. Players intuitively want the next move.

### Directional Impact Particles

On ball landing, impact particles are positioned and oriented to point outward from the hit point — reinforcing the physical direction of the impact. This is not decorative VFX; it's feedback design. The particle burst tells the eye exactly where the force came from, making the hit feel real and grounded.

### Screen Shake as Player Feedback

Screen shake was added as a first-class feedback mechanism. Every ball landing triggers a camera shake tuned for impact weight — strength, vibrato, and randomness are all profile-controlled. The result: effects feel alive, every hit registers physically, and the overall juiciness of the game increases measurably. This is the difference between an action that happens on screen and one the player *feels*.

### Magnet Collect System — Full Overhaul

The base project had a simple kickback → arc pull → settle flow. The upgraded system introduces a full multi-phase animation pipeline:

- **Anticipation** — Target slots pulse before launch; balls lift, reach, and stretch toward the magnet
- **Air Float** — Optional suspension phase building charge tension before release
- **Snap Pull** — Speed-based travel with directional stretch scale
- **Impact Punch** — On arrival: squash → overshoot → settle, giving each ball a satisfying landing hit
- **Screen Shake** — Camera shake on impact, tunable per profile (strength, vibrato, randomness)
- **Group Complete** — Coordinated slot pop and vanish sequence once all balls land

### Data-Driven Feel Profiles

All the above parameters live in `MagnetCollectSettings` ScriptableObject assets (`Create > Marble Sort > Magnet Collect Feel Profile`). One asset controls the entire feel of a collect sequence. Multiple profiles can coexist and be swapped at runtime.

### Project Structure Reorganization

Moved from a flat layout to a feature-based architecture:

| Before | After |
|---|---|
| `Assets/_Project/Scripts/Magnet/` | `Assets/_Project/Features/Magnet/Scripts/` |
| `Assets/_Project/Editor/` | `Assets/_Project/Core/Editor/` |
| `Assets/_Project/Settings/` | `Assets/_Project/Core/Settings/` |
| `Assets/_Project/Prefabs/` | `Assets/_Project/Features/Magnet/Prefabs/` |

### New Art Assets

- **3D Model:** `BoxMesh.fbx` for gameplay objects
- **Shaders:** Always-on-top particle shaders, VFX shockwave ring shader
- **Textures:** Full VFX texture library (glows, rings, flares, trails)
- **Materials:** Organized into `Gameplay/`, `UI/`, and `VFX/` subfolders
