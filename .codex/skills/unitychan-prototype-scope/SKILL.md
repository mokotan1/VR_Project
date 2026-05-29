---
name: unitychan-prototype-scope
description: Project-local Unity scene scope rule for D:\VR_Project. Use when Codex works on this VR Unity project, especially for Unity scene edits, gameplay object placement, prefabs instantiated into scenes, build/startup flow changes, or prototype feature work. Enforces that Unity scene work stays inside Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity unless the user explicitly names another scene.
---

# UnityChan Prototype Scope

This is a project-local rule for `D:\VR_Project`. Treat `Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity` as the default and only working gameplay scene.

## Rule

- Make scene-level Unity changes only in `Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity`.
- Do not add, remove, rewire, or tune gameplay objects in `CrystalDefensePrototype`, `SuperhotPrototype`, `OsFpsInspiredDesktop`, sample scenes, package demo scenes, or alternate UnityChan variants unless the user explicitly requests that specific scene.
- Keep `Assets/Scenes/Startup.unity` only as a startup/router scene. It may be touched when the user asks for startup flow, build order, or scene transition work, but its target gameplay scene should remain `UnityChanPrototypeFps` unless the user explicitly changes that.
- Prefer code, prefab, and asset changes that support `UnityChanPrototypeFps`. If a shared script affects multiple scenes, consider the impact on `UnityChanPrototypeFps` first and avoid incidental behavior changes elsewhere.
- If a task is ambiguous, assume the user means `UnityChanPrototypeFps`.

## Current Startup Flow

Expected build/runtime flow:

```text
Assets/Scenes/Startup.unity
-> Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity
```

`CrystalDefensePrototype` is no longer the active prototype target.
