# Enemy Attack System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Telegraph-based enemy melee attacks with 1-hit player death, wired into `SuperhotEnemyBrain` CloseRange, with pure logic tests and shield block hook.

**Architecture:** `EnemyAttackSessionLogic` (Application) drives WindUp/Active/Recovery phases. `EnemyMeleeAttackController` + `EnemyMeleeHitbox` (Presentation) enable hitbox only during Active. `SuperhotEnemyBrain` removes crystal branches and takedown debuff; starts attacks in CloseRange. Ranged Shooter deferred to a follow-up plan.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing `ShieldBlocker` / `SuperhotPlaytestPlayerHealth`

**Spec:** `docs/superpowers/specs/2026-06-02-enemy-attack-design.md`

---

## File Map

| File | Responsibility |
|------|----------------|
| `Application/Combat/EnemyAttackPhase.cs` | Phase enum |
| `Application/Combat/EnemyMeleeAttackTimings.cs` | Duration struct |
| `Application/Combat/EnemyAttackSessionLogic.cs` | Pure state machine |
| `Presentation/Combat/EnemyMeleeAttackController.cs` | Enemy attack MonoBehaviour |
| `Presentation/Combat/EnemyMeleeHitbox.cs` | Active-window trigger → player hit / shield block |
| `Presentation/Gameplay/SuperhotEnemyBrain.cs` | CloseRange attack wiring, crystal removal |
| `Presentation/Gameplay/SuperhotPlaytestPlayerHealth.cs` | Default 1 hit |
| `Editor/MeleeCombatSceneMenu.cs` | `MeleeEnemySetup` adds melee attack + hitbox |
| `Tests/EditMode/Combat/EnemyAttackSessionLogicTests.cs` | Phase transition tests |
| `Tests/EditMode/Gameplay/SuperhotPlaytestPlayerHealthTests.cs` | 1HK tests |

---

### Task 1: Pure attack session logic (TDD)

- [x] Tests for Begin, WindUp→Active→Recovery→Idle transitions
- [x] `EnemyAttackSessionLogic` implementation

### Task 2: Player 1HK

- [x] Default `_startingHits = 1`
- [x] EditMode test: single `ApplyHit` triggers defeat

### Task 3: Melee attack presentation

- [x] `EnemyMeleeAttackController` — timings, tick, hitbox enable, block stun
- [x] `EnemyMeleeHitbox` — player hit + `ShieldBlocker` cancel

### Task 4: Brain + editor wiring

- [x] `SuperhotEnemyBrain` CloseRange uses controller; remove crystal + takedown debuff
- [x] `MeleeEnemySetup.Ensure` adds controller + hitbox child

### Task 5: Verification

- [ ] `.\harness.cmd verify VR_Project`

---

## Follow-Up (not this plan)

- `EnemyRangedAttack` + Engaging Shooter integration
- Rusher global active cap
- Archetype ScriptableObject profiles
- VR telegraph VFX/audio
