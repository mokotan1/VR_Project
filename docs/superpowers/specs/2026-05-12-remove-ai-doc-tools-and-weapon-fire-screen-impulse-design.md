# Remove AI/Document Tools and Add Weapon Fire Screen Impulse Design

## Summary

The project direction is shifting back toward a focused Unity FPS/VR prototype. The AI voice conversation server and HWP document automation tool are out of scope and should be removed. In their place, weapon firing should feel more tactile through a hybrid screen impulse: a small camera kick plus a short URP post-processing pulse.

## Goals

- Remove the Python AI voice conversation server from the repository.
- Remove the HWP document automation MCP/tooling from the repository.
- Keep the Unity project compiling after those removals.
- Add a reusable weapon-fire screen impulse effect.
- Use a hybrid approach: light camera movement plus brief post-processing changes.
- Keep XR/VR comfort in mind by lowering camera motion strength when XR is active.

## Non-Goals

- Replacing the full weapon system.
- Adding new AI dialogue, speech recognition, or document automation features.
- Reworking unrelated third-party asset folders.
- Building a new custom render pipeline effect.

## Current Context

The Unity project lives under `project/` and uses Unity `6000.4.0f1` with URP. The URP settings already reference volume profiles that include post-processing components such as `LensDistortion`, `ChromaticAberration`, and screen-space lens flare support.

Gameplay code is concentrated under `project/Assets/_Project`. Weapon firing currently appears in two main areas:

- `Presentation/OsFpsInspired/OsFpsInspiredWeapon.cs`
- `Presentation/Gameplay/SuperhotFlatHitscanWeapon.cs`

The AI voice server lives under `ai_server/`. The HWP automation tool lives under `tools/hwp-mcp/`.

## Architecture

### Repository Cleanup

The cleanup should delete:

- `ai_server/`
- `tools/hwp-mcp/`

After deletion, the Unity codebase should be scanned for references to removed server concepts. If Unity code still references AI voice connection classes, those references should be removed or disabled in the narrowest way that preserves compilation.

The cleanup should not delete unrelated Unity gameplay code unless it directly depends on the removed Python server.

### Weapon Fire Screen Impulse

Add a dedicated Unity component responsible for visual firing feedback. A likely name is `WeaponFireScreenImpulse`.

The component should live near presentation/gameplay code and be attachable to a player camera or camera rig. It should expose serialized fields for:

- Camera kick strength.
- Kick recovery speed.
- Lens distortion pulse strength.
- Chromatic aberration pulse strength.
- Optional vignette or flash strength if a compatible volume component is available.
- Separate flat-screen and XR comfort multipliers.

Weapon scripts should only notify this component that a shot occurred. They should not own post-processing details.

### Trigger Flow

The intended flow is:

1. Weapon fires successfully.
2. Weapon invokes a screen impulse trigger.
3. The impulse component applies a short camera offset/rotation kick.
4. The impulse component briefly overrides URP Volume component values.
5. Values decay back to their original state over a short unscaled-time window.

The effect should use unscaled time so it still feels responsive during SUPERHOT-style slow time.

## Visual Direction

Use option C: hybrid recoil plus post-processing.

The effect should be noticeable but not disorienting:

- Flat mode: stronger kick and post pulse for immediate shooting feedback.
- XR/VR mode: reduced camera motion, with more of the feedback carried by post-processing.

The target feel is a sharp, short impulse rather than a long shake.

## Testing

Automated tests should cover the calculation logic where practical, especially:

- Pulse intensity decays to zero over duration.
- Re-triggering while active refreshes or stacks in a controlled way.
- XR multiplier reduces camera kick strength.
- Default values restore after the pulse completes.

Manual Unity verification should confirm:

- The project compiles after removing `ai_server` and `tools/hwp-mcp`.
- Firing `OsFpsInspiredWeapon` triggers the effect.
- Firing `SuperhotFlatHitscanWeapon` triggers the effect.
- Effects decay cleanly and do not leave post-processing values stuck.
- XR mode uses the lower comfort strength.

## Risks

- Existing Unity presentation code may still reference AI server classes. These should be handled during cleanup.
- URP Volume profiles may differ by scene, so the impulse component should handle missing components gracefully.
- Strong camera movement in VR can cause discomfort, so XR mode should prefer low motion and short duration.

## Approved Direction

The approved visual direction is C: a hybrid effect combining light camera kick with short post-processing pulses.
