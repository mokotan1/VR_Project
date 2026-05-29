# Enemy Pose Demolish On Death Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a death effect for the current UnityChan prototype enemy that bakes the current enemy pose and shatters it around the last hit point.

**Architecture:** Put the runtime effect in `VRProject.Presentation.Gameplay` beside the existing damage feedback components. Call the MeshDemolisher public API through a small reflection bridge so the existing third-party folder does not need an asmdef reshuffle, then attach the new component to `CrystalDefenseEnemy.prefab`.

**Tech Stack:** Unity C#, Unity Test Framework EditMode tests, `OsFpsInspiredDamageable`, `SkinnedMeshRenderer.BakeMesh`, `Hanzzz.MeshDemolisher.MeshDemolisher`.

---

### Task 1: Test the death effect surface

**Files:**
- Create: `Project/Assets/_Project/Tests/EditMode/EnemyPoseDemolishOnDeathTests.cs`
- Create: `Project/Assets/_Project/Presentation/Gameplay/EnemyPoseDemolishOnDeath.cs`

- [x] **Step 1: Write the failing tests**

Write tests that verify the component records the last damage point, triggers only after lethal damage, and exposes deterministic break point placement.

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode tests and confirm the failure is because `EnemyPoseDemolishOnDeath` does not exist yet.

- [x] **Step 3: Write minimal implementation**

Implement the MonoBehaviour with a test seam for fragment creation, plus runtime mesh baking and MeshDemolisher execution.

- [ ] **Step 4: Run tests to verify they pass**

Run the same EditMode tests and then compile the Unity project.

Status: Unity batch-mode EditMode test execution was blocked because the project is already open in another Unity Editor instance. `dotnet build project/VRProject.Tests.EditMode.csproj --no-restore` completed with 0 warnings and 0 errors after cleanup.

### Task 2: Wire the asset and prefab

**Files:**
- Modify: `Project/Assets/_Project/Presentation/Gameplay/Prefabs/CrystalDefenseEnemy.prefab`
- Modify: `Project/Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity`
- Create: `Project/Assets/Hanzzz/MeshDemolisher/Scripts/**`

- [x] **Step 1: Keep MeshDemolisher decoupled**

Use reflection to avoid moving editor-only MeshDemolisher files or creating a runtime asmdef that accidentally includes `UnityEditor` scripts.

- [x] **Step 2: Attach the component to the enemy prefab**

Add `EnemyPoseDemolishOnDeath` to `CrystalDefenseEnemy.prefab` with conservative default shard settings and the existing enemy material fallback.

- [ ] **Step 3: Verify**

Run EditMode tests and check that Unity can compile the new script without errors.

Status: C# compilation via generated Unity csproj passes with 0 warnings and 0 errors. Full Unity Test Runner verification still needs the currently open Unity Editor instance to be closed or used directly.
