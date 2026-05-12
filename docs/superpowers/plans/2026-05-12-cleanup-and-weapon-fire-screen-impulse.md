# Cleanup and Weapon Fire Screen Impulse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove out-of-scope AI voice/document automation code and add a hybrid weapon-fire screen impulse effect for Unity firing feedback.

**Architecture:** Delete the Python/HWP support folders and remove Unity AI voice connection surfaces that only exist to call the removed server. Add a small pure calculation helper plus a `WeaponFireScreenImpulse` MonoBehaviour that handles camera kick and URP Volume pulses. Existing weapon scripts only trigger the impulse when a shot successfully fires.

**Tech Stack:** Unity 6000.4.0f1, C#, URP Volume framework, Unity EditMode tests, PowerShell, git.

---

## File Structure

- Create `project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulseProfile.cs`: pure calculation helpers for pulse decay and XR/flat kick scaling.
- Create `project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulse.cs`: MonoBehaviour that applies local camera kick and URP post-processing pulses.
- Create `project/Assets/_Project/Tests/EditMode/WeaponFireScreenImpulseProfileTests.cs`: EditMode tests for decay/retrigger/XR scaling.
- Modify `project/Assets/_Project/Presentation/OsFpsInspired/OsFpsInspiredWeapon.cs`: trigger impulse after a successful bullet shot.
- Modify `project/Assets/_Project/Presentation/Gameplay/SuperhotFlatHitscanWeapon.cs`: trigger impulse after desktop test shot input.
- Delete `ai_server/`: removed Python AI voice conversation server.
- Delete `tools/hwp-mcp/`: removed HWP document automation tool.
- Delete Unity AI voice bridge files if they remain unreferenced and depend only on the removed server: `AIServerConnection.cs`, `WhisperWebSocketAdapter.cs`, `OllamaWebSocketAdapter.cs`, `MicrophoneCaptureAdapter.cs`, `VoiceInputController.cs`, `NPCDialogueController.cs`, `DialogueView.cs`, and AI dialogue/voice use-case/domain files.

---

## Task 1: Add Screen Impulse Calculation Tests

**Files:**
- Create: `project/Assets/_Project/Tests/EditMode/WeaponFireScreenImpulseProfileTests.cs`
- Create later: `project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulseProfile.cs`

- [ ] **Step 1: Write the failing test**

Create `WeaponFireScreenImpulseProfileTests.cs`:

```csharp
using NUnit.Framework;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class WeaponFireScreenImpulseProfileTests
    {
        [Test]
        public void PulseWeight_AtStart_IsOne()
        {
            Assert.That(WeaponFireScreenImpulseProfile.PulseWeight(0f, 0.2f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PulseWeight_AfterDuration_IsZero()
        {
            Assert.That(WeaponFireScreenImpulseProfile.PulseWeight(0.2f, 0.2f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PulseWeight_Halfway_EasesDown()
        {
            var weight = WeaponFireScreenImpulseProfile.PulseWeight(0.1f, 0.2f);
            Assert.That(weight, Is.GreaterThan(0f));
            Assert.That(weight, Is.LessThan(1f));
        }

        [Test]
        public void EffectiveKickStrength_InXr_UsesComfortMultiplier()
        {
            var strength = WeaponFireScreenImpulseProfile.EffectiveKickStrength(2f, 0.25f, true);
            Assert.That(strength, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void EffectiveKickStrength_InFlatMode_UsesFullStrength()
        {
            var strength = WeaponFireScreenImpulseProfile.EffectiveKickStrength(2f, 0.25f, false);
            Assert.That(strength, Is.EqualTo(2f).Within(0.001f));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Unity EditMode tests or compile tests. Expected failure: `WeaponFireScreenImpulseProfile` type does not exist.

- [ ] **Step 3: Implement minimal helper**

Create `WeaponFireScreenImpulseProfile.cs`:

```csharp
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class WeaponFireScreenImpulseProfile
    {
        public static float PulseWeight(float elapsedSeconds, float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return 0f;

            var t = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            var inv = 1f - t;
            return inv * inv;
        }

        public static float EffectiveKickStrength(float baseStrength, float xrComfortMultiplier, bool xrActive)
        {
            var multiplier = xrActive ? Mathf.Clamp01(xrComfortMultiplier) : 1f;
            return Mathf.Max(0f, baseStrength) * multiplier;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the same EditMode test target. Expected: all `WeaponFireScreenImpulseProfileTests` pass.

- [ ] **Step 5: Commit**

```bash
git add project/Assets/_Project/Tests/EditMode/WeaponFireScreenImpulseProfileTests.cs project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulseProfile.cs
git commit -m "test: cover weapon fire screen impulse profile"
```

---

## Task 2: Add WeaponFireScreenImpulse MonoBehaviour

**Files:**
- Create: `project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulse.cs`

- [ ] **Step 1: Create the component**

Add a MonoBehaviour that:

- Finds a target camera transform if none is assigned.
- Captures the original local camera position/rotation.
- On `Trigger()`, resets the pulse timer.
- During `LateUpdate`, applies a short local position/rotation kick.
- If a `VolumeProfile` is assigned or found from a `Volume`, tries to modify `LensDistortion`, `ChromaticAberration`, and `Vignette`.
- Restores original post values when disabled or when the pulse ends.

- [ ] **Step 2: Include this public trigger API**

```csharp
public void Trigger()
{
    _elapsed = 0f;
    _active = true;
}
```

- [ ] **Step 3: Run compile check**

Expected: no missing URP/Volume type errors.

- [ ] **Step 4: Commit**

```bash
git add project/Assets/_Project/Presentation/Gameplay/WeaponFireScreenImpulse.cs
git commit -m "feat: add weapon fire screen impulse component"
```

---

## Task 3: Trigger Impulse From Weapon Scripts

**Files:**
- Modify: `project/Assets/_Project/Presentation/OsFpsInspired/OsFpsInspiredWeapon.cs`
- Modify: `project/Assets/_Project/Presentation/Gameplay/SuperhotFlatHitscanWeapon.cs`

- [ ] **Step 1: Add serialized impulse fields**

In each weapon script, add:

```csharp
[Header("발사 화면 연출")]
[SerializeField] WeaponFireScreenImpulse _screenImpulse;
```

- [ ] **Step 2: Auto-resolve in `Awake`**

Add:

```csharp
if (_screenImpulse == null)
    _screenImpulse = GetComponentInParent<WeaponFireScreenImpulse>();
```

- [ ] **Step 3: Trigger after successful fire input**

For `OsFpsInspiredWeapon`, call:

```csharp
_screenImpulse?.Trigger();
```

after `_lastFireUnscaledTime = Time.unscaledTime;`.

For `SuperhotFlatHitscanWeapon`, call:

```csharp
_screenImpulse?.Trigger();
```

after `_lastShootUnscaledTime = Time.unscaledTime;`.

- [ ] **Step 4: Run compile check**

Expected: no namespace or missing type errors.

- [ ] **Step 5: Commit**

```bash
git add project/Assets/_Project/Presentation/OsFpsInspired/OsFpsInspiredWeapon.cs project/Assets/_Project/Presentation/Gameplay/SuperhotFlatHitscanWeapon.cs
git commit -m "feat: trigger screen impulse when weapons fire"
```

---

## Task 4: Remove Out-of-Scope AI and HWP Tooling

**Files:**
- Delete: `ai_server/`
- Delete: `tools/hwp-mcp/`
- Delete: Unity AI voice bridge and dialogue controller/use-case files that only support the removed server.

- [ ] **Step 1: Search references**

Run:

```bash
rg -n "AIServer|Ollama|Whisper|VoiceInput|NPCDialogue|ILanguageModel|ISpeechRecognizer|ai_server|hwp|HWP" project/Assets/_Project project/Assets -g "*.cs" -g "*.asmdef" -g "*.unity" -g "*.prefab" -g "*.asset"
```

- [ ] **Step 2: Delete Python server and HWP tool folders**

Remove `ai_server/` and `tools/hwp-mcp/`.

- [ ] **Step 3: Remove Unity bridge files**

Delete the C# files and matching `.meta` files for removed AI dialogue/server features if they remain unreferenced by scenes/prefabs.

- [ ] **Step 4: Re-run reference search**

Expected: no references to removed server/tool names remain in Unity source.

- [ ] **Step 5: Commit**

```bash
git add -A ai_server tools/hwp-mcp project/Assets/_Project
git commit -m "chore: remove ai voice server and document automation"
```

---

## Task 5: Final Verification

**Files:**
- Read: `compile_log.txt`
- Read: `compile_verify.txt`

- [ ] **Step 1: Run targeted text checks**

Run:

```bash
rg -n "AIServer|Ollama|Whisper|VoiceInput|NPCDialogue|ai_server|hwp|HWP" .
```

Expected: only historical docs/git metadata or no matches.

- [ ] **Step 2: Run Unity EditMode tests**

Run the available Unity test command for `VRProject.Tests.EditMode` if Unity CLI is available. Expected: tests pass.

- [ ] **Step 3: Check git status**

Run:

```bash
git status --short
```

Expected: only intentional changes.

- [ ] **Step 4: Report results**

Summarize changed files, deleted surfaces, and any verification that could not be run.
