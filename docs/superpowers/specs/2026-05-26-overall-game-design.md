# Overall Game Design

## One-Line Concept

A crystal-defense action game where the player uses Superhot-style movement-driven time control to defend a central crystal from enemy waves, playable in either mobile/tablet mode or VR mode.

## Current Prototype Meaning

The current scene with a blue sphere and colored cubes should be treated as a test arena, not the final game presentation.

- The blue sphere becomes `Crystal_Core`, the main objective.
- The cubes become temporary placeholders for grabbable props, combat objects, or debug markers.
- The flat gray floor becomes the first combat arena.
- The scene still needs UI, wave spawning, enemy behavior, mode selection, and platform-specific input before it communicates the intended game.

## Full Game Flow

```text
App Launch
  ↓
Startup Scene
  - Check device/platform
  - Check XR headset availability
  - Show Mobile Play / VR Play choices
  ↓
Mode Selected
  - Save selected mode in session
  ↓
Crystal Defense Gameplay Scene
  - Activate mobile rig or XR rig
  - Start crystal-defense encounter
  ↓
Wave Combat
  - Enemies spawn
  - Enemies attack player or crystal
  - Player defends using weapons, throwing, movement, and time control
  ↓
Boss / Elite Wave
  ↓
Victory if crystal survives all waves
Defeat if crystal health reaches zero
```

## Startup Scene Design

The app should not enter gameplay immediately. It should first show a device connection and play mode selection screen.

Required UI:

```text
[Device Connection]

Device: Android Device / Unity Editor / VR Headset
Mobile Play: Ready / Unavailable
VR Headset: Connected / Not Connected

[Refresh]
[Mobile Play]
[VR Play]
```

Rules:

- `Mobile Play` is enabled on Android tablet and editor/desktop playtest.
- `VR Play` is enabled only when an XR device is active.
- `Refresh` re-checks device and XR status.
- The selected mode is saved before loading gameplay.

## Gameplay Scene Layout

Basic arena structure:

```text
              EnemySpawn_03

                  Enemy
                    ↓

 EnemySpawn_02 → [ Crystal_Core ] ← EnemySpawn_01

                 Player Start
```

Required scene objects:

- `Systems`
  - `GameBootstrapper`
  - `SuperhotGameplayDriver`
  - `SuperhotPlaytestRigSelector`
  - `CrystalDefenseWaveDirector`
  - platform performance mode/limiter components
- `Crystal_Core`
  - visual crystal mesh
  - collider
  - `CrystalCoreHealth`
- `EnemySpawns`
  - at least three spawn point transforms
- `XR Origin`
  - VR mode rig
- `Mobile/Flat Rig`
  - mobile/tablet and editor play rig
- `Grabbable Props`
  - throwable combat objects
- `Arena`
  - floor, cover, spawn entrances, lighting

## Core Gameplay Loop

Player objective:

```text
Keep Crystal_Core alive until all waves are cleared.
```

Enemy objective:

```text
Attack the player when the player is visible, close, noisy, or threatening.
Otherwise move toward and attack Crystal_Core.
```

Victory condition:

```text
All normal waves and the final boss/elite wave are cleared while the crystal survives.
```

Defeat condition:

```text
Crystal_Core health reaches zero.
```

## Main Systems

### `GameBootstrapper`

Initializes shared infrastructure services. It should remain persistent where needed and register core services such as the gameplay clock and event bus.

### `PlayModeSession`

Stores the selected mode from the startup scene.

Modes:

- `Mobile`
- `Vr`

### `SuperhotPlaytestRigSelector`

Reads the selected mode and enables only one gameplay rig:

- Mobile mode: enable flat/mobile rig, disable XR Origin.
- VR mode: enable XR Origin, disable flat/mobile rig.

Fallback behavior:

- If no mode was selected and XR is active, use VR.
- If no mode was selected and XR is not active, use Mobile.

### `SuperhotGameplayDriver`

Controls time scale.

VR behavior:

- Samples HMD and controller movement.
- More movement means time flows faster.
- Less movement means time slows down.

Mobile/flat behavior:

- Samples movement and look input.
- Movement/look activity drives time scale.

### `CrystalCoreHealth`

Owns the crystal's health and destruction state.

Responsibilities:

- Store max/current health.
- Receive damage.
- Raise damage events.
- Raise destroyed event once.
- Trigger defeat when destroyed.

### `CrystalDefenseWaveDirector`

Owns encounter progression.

Responsibilities:

- Start waves.
- Spawn enemies from configured spawn points.
- Limit max alive enemies.
- Track wave clear.
- Start final boss/elite wave.
- Trigger victory.

### `CrystalDefenseEnemyObjective`

Decides what an enemy should currently attack.

Possible targets:

- Player
- Crystal
- None

Targeting rule:

- Prefer player when visible, close, or threatening.
- Prefer crystal when player is not a strong immediate target.

### `SuperhotEnemyBrain`

Existing enemy AI brain. It should be extended rather than replaced.

Existing behavior:

- Hearing
- Line of sight
- Flanking
- Engagement
- Close-range takedown

New behavior:

- Route toward `Crystal_Core` when the objective system selects the crystal.
- Attack the crystal when in range.

### `CrystalDefenseEnemyAttack`

Applies enemy damage to player or crystal.

Responsibilities:

- Range check.
- Attack cooldown.
- Damage crystal.
- Damage player where applicable.

### `OsFpsInspiredWeapon`

Existing player weapon system.

Responsibilities:

- Equipped state.
- Ammo.
- Reload.
- Hitscan fire.
- Bullet tracer visual.
- Throw gun when empty.
- Apply damage to enemies.

### `CrystalDefenseGrabbableDamage`

Lets thrown objects damage enemies.

Responsibilities:

- Read collision velocity.
- Apply damage only above minimum speed.
- Prevent repeated rapid damage to the same target.

## Mobile Mode Design

Target device:

- Samsung Galaxy Tab S7 FE.

Primary target:

- Snapdragon 778G model.
- 60 FPS.

Conservative target:

- Snapdragon 750G / 4 GB RAM model.
- 45-60 FPS.

Input:

- Touch movement.
- Touch look.
- Fire/reload/throw buttons.
- Optional virtual joystick.

Required mobile UI:

```text
Crystal HP
Wave indicator
Ammo indicator
Virtual movement control
Look area
Fire button
Reload button
Throw button
Pause/settings button
```

Mobile constraints:

- No VR hand interaction.
- Use screen-space UI.
- Cap enemy count.
- Cap projectiles/tracers.
- Cap physics props.
- Cap glass/particle effects.
- Render below native resolution through URP render scale.

## VR Mode Design

Target device:

- Meta Quest 3 / Quest 3S first.
- Quest 2 as lower-spec compatibility path.

Primary target:

- 72 FPS stable.

Input:

- XR headset movement.
- XR controller movement.
- Grab.
- Throw.
- Trigger fire.
- Controller haptics.

VR UI:

- Avoid heavy screen-space overlays.
- Prefer spatial HUD near crystal, wrist, or controller.
- Show crystal status clearly without blocking view.

VR constraints:

- Reduce camera shake.
- Use haptics for feedback.
- Limit particles and transparent effects.
- Keep one XR Origin active.
- Maintain comfort and stable frame timing.

## Time-Control Design

The game's identity comes from movement-driven time.

Rule:

```text
When the player is still, the world slows.
When the player moves, the world speeds up.
```

This applies to:

- Enemy movement
- Projectiles
- Animations
- Combat pacing

Design implication:

- Fewer enemies can still feel threatening.
- The player can plan shots and throws while still.
- Moving carelessly makes the battlefield more dangerous.

## Wave Structure

Initial target structure:

```text
Wave 1: 3 enemies
Wave 2: 4 enemies
Wave 3: 5 enemies
Final Wave: 1 boss/elite enemy
```

Each wave has:

- enemy prefab
- enemy count
- spawn interval
- max alive count
- start delay
- boss-wave flag

The director should support tuning these values in the Inspector.

## Visual Direction

The current prototype needs stronger game readability.

Crystal:

- Replace plain sphere with a glowing crystal/core form.
- Show cracks or flashing light when damaged.
- Make it the clearest object in the arena.

Enemies:

- Use red or hostile silhouettes.
- Make enemy intent readable from a distance.
- Boss/elite should be visually larger or distinct.

Arena:

- Simple but intentional combat space.
- Spawn entrances should be visible.
- Cover and throwable objects should be placed around the player.

Props:

- Cubes should become objects with gameplay meaning:
  - crates
  - shards
  - tools
  - debris
  - throwable blocks

Feedback:

- Wave start warning.
- Crystal damage effect.
- Enemy death effect.
- Player hit feedback.
- Victory/defeat result.

## Performance Design

Performance should be designed into the game, not patched later.

Shared limits:

- Cap simultaneous enemies.
- Cap active tracers.
- Cap active physics props.
- Cap glass shard bursts.
- Cap particle counts.
- Avoid recurring garbage allocations in hot paths.

Mobile tablet budget:

- 60 FPS target.
- Render scale around `0.6-0.75`.
- HDR off.
- Post-processing off by default.
- Additional realtime lights off.

Quest budget:

- 72 FPS target.
- Render scale around `0.7-0.8`.
- HDR off.
- Post-processing minimal.
- Foveated rendering where available.
- Stable frame pacing over visual complexity.

## Code Organization

Recommended structure:

```text
Application
  Startup
    PlayModeSelection.cs
  Gameplay
    performance budgets
    pure gameplay helpers

Presentation
  Startup
    DeviceConnectionProbe.cs
    DeviceConnectionView.cs
    PlayModeSession.cs

  Gameplay
    CrystalCoreHealth.cs
    CrystalDefenseWaveDirector.cs
    CrystalDefenseEnemyObjective.cs
    CrystalDefenseEnemyAttack.cs
    CrystalDefenseGrabbableDamage.cs
    SuperhotGameplayDriver.cs
    SuperhotEnemyBrain.cs
    SuperhotPlaytestRigSelector.cs

  Common
    UI
    Managers

Editor
  StartupSceneMenu.cs
  SuperhotPrototypeSceneMenu.cs
  optimization menus
```

## Related Implementation Plans

- `docs/superpowers/plans/2026-05-26-vr-crystal-defense-and-interaction.md`
- `docs/superpowers/plans/2026-05-26-device-connection-and-play-mode-selection.md`
- `docs/superpowers/plans/2026-05-26-galaxy-tab-s7-fe-mobile-optimization.md`
- `docs/superpowers/plans/2026-05-26-meta-quest-mobile-optimization.md`

## First Build Milestone

The first meaningful milestone is not visual polish. It is a playable loop.

Milestone requirements:

1. Startup scene appears.
2. Player chooses Mobile Play or VR Play.
3. Gameplay scene loads.
4. Correct rig activates.
5. `Crystal_Core` has health.
6. Wave 1 spawns enemies.
7. Enemies can damage the crystal.
8. Player can kill enemies.
9. Wave can clear.
10. Crystal destruction causes defeat.

After this milestone, visual design and combat feel can be improved with confidence.

## Current Gap

The current scene does not yet match this design visually or mechanically.

It is currently closer to:

```text
test floor + placeholder sphere + placeholder cubes
```

It needs to become:

```text
startup mode selection + crystal-defense arena + wave combat + platform-specific controls
```

That gap is expected. The purpose of this design is to define exactly what must be added to turn the placeholder scene into the intended game.
