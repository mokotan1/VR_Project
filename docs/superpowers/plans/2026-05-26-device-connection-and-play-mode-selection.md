# Device Connection and Play Mode Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a startup flow that shows device connection status, confirms whether a mobile/tablet or VR headset is available, and lets the player choose Mobile Play or VR Play before entering the game.

**Architecture:** Add a lightweight launch scene and runtime selection service before the crystal-defense scene. The launch UI runs on normal screen-space canvas for Android/tablet and editor, with XR availability shown as a status card rather than assuming XR is active. The selected play mode is stored in a persistent session object, then the gameplay scene activates the mobile/flat rig or XR rig through a revised rig selector.

**Tech Stack:** Unity 6000.x, C#, Unity UI, Unity SceneManagement, Unity XR Management / `XRSettings`, XR Interaction Toolkit, Android tablet build, existing `GameBootstrapper`, existing `SuperhotPlaytestRigSelector`, existing `SuperhotPrototypeSceneMenu`.

---

## User Flow

Startup scene:

1. Show a connection-check screen immediately after app launch.
2. Detect and display:
   - current platform: Android tablet, desktop editor, or other;
   - XR device active or not;
   - XR device name if available;
   - whether the current device can use Mobile Play;
   - whether the current device can use VR Play.
3. Provide a refresh button for connection status.
4. Provide two explicit choices:
   - `Mobile Play`: screen/touch/flat rig.
   - `VR Play`: XR rig/headset mode.
5. Disable or warn on choices that are not currently available.
6. When a mode is selected, save the mode in a persistent session and load the gameplay scene.

Gameplay scene:

1. Read the saved play mode.
2. Enable only the matching rig:
   - Mobile Play: flat/mobile rig and normal camera.
   - VR Play: XR Origin and XR interaction rig.
3. Keep one active MainCamera and one AudioListener.
4. If no saved mode exists, fall back safely:
   - XR active: VR Play.
   - Android/mobile: Mobile Play.
   - Editor/no XR: Mobile Play.

## Current Project Context

Relevant existing files:

- `project/Assets/_Project/Presentation/Common/Managers/GameBootstrapper.cs`
  - Persistent bootstrapper that registers shared services.
- `project/Assets/_Project/Presentation/Common/UI/ViewBase.cs`
  - Simple base class for UI views.
- `project/Assets/_Project/Presentation/Gameplay/SuperhotPlaytestRigSelector.cs`
  - Currently auto-selects XR rig if `XRSettings.isDeviceActive`, otherwise flat rig.
- `project/Assets/_Project/Editor/SuperhotPrototypeSceneMenu.cs`
  - Creates Superhot, interaction, and crystal-defense prototype scenes.
- `project/Assets/_Project/Presentation/Gameplay/SuperhotFlatPlaytestRig.cs`
  - Existing flat playtest rig marker.
- `docs/superpowers/plans/2026-05-26-vr-crystal-defense-and-interaction.md`
  - Gameplay feature plan.
- `docs/superpowers/plans/2026-05-26-galaxy-tab-s7-fe-mobile-optimization.md`
  - Android tablet optimization plan.
- `docs/superpowers/plans/2026-05-26-meta-quest-mobile-optimization.md`
  - Quest standalone optimization plan.

## Design Decisions

- Use an explicit launch scene rather than burying the selection panel in gameplay. This keeps startup state clean and avoids enabling both mobile and XR rigs for a frame.
- Store play mode in a `DontDestroyOnLoad` session object so the gameplay scene can stay reusable.
- Use pure decision helpers for tests. Unity device/XR APIs are wrapped by small components because real device connection state cannot be reliably unit-tested in edit mode.
- Mobile Play means Android tablet or desktop-style flat play. It is not Quest mobile VR.
- VR Play means XR rig/headset mode. It can be Meta Quest standalone or a PC-connected headset if the project supports that path.

## File Structure

Create these files:

- `project/Assets/_Project/Application/Startup/PlayModeSelection.cs`
  - Pure enum and decision helpers for selected mode, availability, and fallback.
- `project/Assets/_Project/Presentation/Startup/DeviceConnectionStatus.cs`
  - Runtime snapshot of platform/XR/device connection state.
- `project/Assets/_Project/Presentation/Startup/PlayModeSession.cs`
  - Persistent selected-mode holder.
- `project/Assets/_Project/Presentation/Startup/DeviceConnectionProbe.cs`
  - Unity-facing component that samples platform and XR state.
- `project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs`
  - UI view/controller for status cards and play-mode buttons.
- `project/Assets/_Project/Editor/StartupSceneMenu.cs`
  - Editor menu that creates `Assets/Scenes/Startup.unity` with the connection UI.
- `project/Assets/_Project/Tests/EditMode/PlayModeSelectionTests.cs`
  - Tests for availability and fallback logic.

Modify these files:

- `project/Assets/_Project/Presentation/Gameplay/SuperhotPlaytestRigSelector.cs`
  - Read `PlayModeSession` before falling back to automatic XR detection.
- `project/Assets/_Project/Editor/SuperhotPrototypeSceneMenu.cs`
  - Ensure generated gameplay scenes keep both XR and flat/mobile rigs available for selector use.
- `project/ProjectSettings/EditorBuildSettings.asset`
  - Add Startup scene before gameplay scenes.

Optional future files:

- `project/Assets/_Project/Presentation/Startup/TabletConnectionHelpView.cs`
  - Only create if the team wants a separate instruction panel for USB/debug/device setup.

## Implementation Tasks

### Task 1: Pure Play Mode Selection Logic

**Files:**
- Create: `project/Assets/_Project/Application/Startup/PlayModeSelection.cs`
- Create: `project/Assets/_Project/Tests/EditMode/PlayModeSelectionTests.cs`

- [ ] **Step 1: Write failing tests**

Create `project/Assets/_Project/Tests/EditMode/PlayModeSelectionTests.cs`:

```csharp
using NUnit.Framework;
using VRProject.Application.Startup;

namespace VRProject.Tests.EditMode
{
    public sealed class PlayModeSelectionTests
    {
        [Test]
        public void CanSelectMobile_WhenMobileAvailable_ReturnsTrue()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.IsTrue(PlayModeSelection.CanSelect(PlayModeKind.Mobile, availability));
        }

        [Test]
        public void CanSelectVr_WhenVrUnavailable_ReturnsFalse()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.IsFalse(PlayModeSelection.CanSelect(PlayModeKind.Vr, availability));
        }

        [Test]
        public void ChooseFallback_PrefersVrWhenXrActive()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: true);

            Assert.AreEqual(PlayModeKind.Vr, PlayModeSelection.ChooseFallback(availability));
        }

        [Test]
        public void ChooseFallback_UsesMobileWhenVrUnavailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.AreEqual(PlayModeKind.Mobile, PlayModeSelection.ChooseFallback(availability));
        }

        [Test]
        public void ResolveSelectedMode_UsesRequestedModeWhenAvailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: true);

            Assert.AreEqual(
                PlayModeKind.Mobile,
                PlayModeSelection.ResolveSelectedMode(PlayModeKind.Mobile, availability));
        }

        [Test]
        public void ResolveSelectedMode_FallsBackWhenRequestedModeUnavailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.AreEqual(
                PlayModeKind.Mobile,
                PlayModeSelection.ResolveSelectedMode(PlayModeKind.Vr, availability));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run from `D:\VR_Project\project`:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ..\play-mode-selection-tests.xml -quit
```

Expected: compile fails because `VRProject.Application.Startup` types do not exist.

- [ ] **Step 3: Implement pure selection logic**

Create `project/Assets/_Project/Application/Startup/PlayModeSelection.cs`:

```csharp
namespace VRProject.Application.Startup
{
    public enum PlayModeKind
    {
        None,
        Mobile,
        Vr
    }

    public readonly struct PlayModeAvailability
    {
        public PlayModeAvailability(bool mobileAvailable, bool vrAvailable)
        {
            MobileAvailable = mobileAvailable;
            VrAvailable = vrAvailable;
        }

        public bool MobileAvailable { get; }
        public bool VrAvailable { get; }
    }

    public static class PlayModeSelection
    {
        public static bool CanSelect(PlayModeKind mode, PlayModeAvailability availability)
        {
            switch (mode)
            {
                case PlayModeKind.Mobile:
                    return availability.MobileAvailable;
                case PlayModeKind.Vr:
                    return availability.VrAvailable;
                default:
                    return false;
            }
        }

        public static PlayModeKind ChooseFallback(PlayModeAvailability availability)
        {
            if (availability.VrAvailable)
                return PlayModeKind.Vr;
            if (availability.MobileAvailable)
                return PlayModeKind.Mobile;
            return PlayModeKind.None;
        }

        public static PlayModeKind ResolveSelectedMode(PlayModeKind requested, PlayModeAvailability availability)
        {
            return CanSelect(requested, availability)
                ? requested
                : ChooseFallback(availability);
        }
    }
}
```

- [ ] **Step 4: Run tests**

Expected: all `PlayModeSelectionTests` pass.

- [ ] **Step 5: Commit**

```powershell
git add project/Assets/_Project/Application/Startup/PlayModeSelection.cs project/Assets/_Project/Tests/EditMode/PlayModeSelectionTests.cs
git commit -m "feat: add play mode selection logic"
```

### Task 2: Device Connection Status and Probe

**Files:**
- Create: `project/Assets/_Project/Presentation/Startup/DeviceConnectionStatus.cs`
- Create: `project/Assets/_Project/Presentation/Startup/DeviceConnectionProbe.cs`

- [ ] **Step 1: Implement status value**

Create `project/Assets/_Project/Presentation/Startup/DeviceConnectionStatus.cs`:

```csharp
using VRProject.Application.Startup;

namespace VRProject.Presentation.Startup
{
    public readonly struct DeviceConnectionStatus
    {
        public DeviceConnectionStatus(
            string platformLabel,
            string xrDeviceName,
            bool isAndroid,
            bool isEditor,
            bool xrDeviceActive,
            bool mobileAvailable,
            bool vrAvailable)
        {
            PlatformLabel = platformLabel ?? string.Empty;
            XrDeviceName = xrDeviceName ?? string.Empty;
            IsAndroid = isAndroid;
            IsEditor = isEditor;
            XrDeviceActive = xrDeviceActive;
            Availability = new PlayModeAvailability(mobileAvailable, vrAvailable);
        }

        public string PlatformLabel { get; }
        public string XrDeviceName { get; }
        public bool IsAndroid { get; }
        public bool IsEditor { get; }
        public bool XrDeviceActive { get; }
        public PlayModeAvailability Availability { get; }

        public string MobileStatusText =>
            Availability.MobileAvailable ? "Mobile Play Ready" : "Mobile Play Unavailable";

        public string VrStatusText =>
            Availability.VrAvailable ? "VR Headset Ready" : "VR Headset Not Connected";
    }
}
```

- [ ] **Step 2: Implement Unity device probe**

Create `project/Assets/_Project/Presentation/Startup/DeviceConnectionProbe.cs`:

```csharp
using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Startup
{
    [DisallowMultipleComponent]
    public sealed class DeviceConnectionProbe : MonoBehaviour
    {
        public DeviceConnectionStatus CurrentStatus { get; private set; }

        void Awake()
        {
            Refresh();
        }

        public DeviceConnectionStatus Refresh()
        {
            var isAndroid = Application.platform == RuntimePlatform.Android;
            var isEditor = Application.isEditor;
            var xrActive = XRSettings.isDeviceActive;
            var xrName = xrActive ? XRSettings.loadedDeviceName : string.Empty;
            var platformLabel = ResolvePlatformLabel(isAndroid, isEditor);

            var mobileAvailable = isAndroid || isEditor ||
                                  Application.platform == RuntimePlatform.WindowsPlayer ||
                                  Application.platform == RuntimePlatform.OSXPlayer ||
                                  Application.platform == RuntimePlatform.LinuxPlayer;

            var vrAvailable = xrActive;

            CurrentStatus = new DeviceConnectionStatus(
                platformLabel,
                xrName,
                isAndroid,
                isEditor,
                xrActive,
                mobileAvailable,
                vrAvailable);

            return CurrentStatus;
        }

        static string ResolvePlatformLabel(bool isAndroid, bool isEditor)
        {
            if (isEditor)
                return "Unity Editor";
            if (isAndroid)
                return "Android Device";
            return Application.platform.ToString();
        }
    }
}
```

- [ ] **Step 3: Compile**

Run edit-mode tests.

Expected: compile passes.

- [ ] **Step 4: Commit**

```powershell
git add project/Assets/_Project/Presentation/Startup/DeviceConnectionStatus.cs project/Assets/_Project/Presentation/Startup/DeviceConnectionProbe.cs
git commit -m "feat: add device connection probe"
```

### Task 3: Persistent Play Mode Session

**Files:**
- Create: `project/Assets/_Project/Presentation/Startup/PlayModeSession.cs`

- [ ] **Step 1: Implement persistent session**

Create `project/Assets/_Project/Presentation/Startup/PlayModeSession.cs`:

```csharp
using UnityEngine;
using VRProject.Application.Startup;

namespace VRProject.Presentation.Startup
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class PlayModeSession : MonoBehaviour
    {
        static PlayModeSession s_instance;

        [SerializeField] PlayModeKind _selectedMode = PlayModeKind.None;

        public static PlayModeSession Instance => s_instance;
        public PlayModeKind SelectedMode => _selectedMode;
        public bool HasSelection => _selectedMode != PlayModeKind.None;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        public void SetSelectedMode(PlayModeKind mode)
        {
            _selectedMode = mode;
        }

        public static PlayModeKind GetSelectedModeOrFallback(PlayModeAvailability availability)
        {
            if (s_instance != null && s_instance.HasSelection)
                return PlayModeSelection.ResolveSelectedMode(s_instance.SelectedMode, availability);

            return PlayModeSelection.ChooseFallback(availability);
        }
    }
}
```

- [ ] **Step 2: Compile**

Run edit-mode tests.

Expected: compile passes.

- [ ] **Step 3: Commit**

```powershell
git add project/Assets/_Project/Presentation/Startup/PlayModeSession.cs
git commit -m "feat: persist selected play mode"
```

### Task 4: Device Connection UI View

**Files:**
- Create: `project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs`

- [ ] **Step 1: Implement UI controller**

Create `project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Application.Startup;
using VRProject.Presentation.Common.UI;

namespace VRProject.Presentation.Startup
{
    [DisallowMultipleComponent]
    public sealed class DeviceConnectionView : ViewBase
    {
        [Header("Scene")]
        [SerializeField] string _gameplaySceneName = "CrystalDefensePrototype";

        [Header("Dependencies")]
        [SerializeField] DeviceConnectionProbe _probe;
        [SerializeField] PlayModeSession _session;

        [Header("Status Text")]
        [SerializeField] Text _platformText;
        [SerializeField] Text _xrStatusText;
        [SerializeField] Text _mobileStatusText;
        [SerializeField] Text _messageText;

        [Header("Buttons")]
        [SerializeField] Button _refreshButton;
        [SerializeField] Button _mobilePlayButton;
        [SerializeField] Button _vrPlayButton;

        DeviceConnectionStatus _status;

        protected override void OnInitialize()
        {
            if (_probe == null)
                _probe = FindFirstObjectByType<DeviceConnectionProbe>();
            if (_session == null)
                _session = FindFirstObjectByType<PlayModeSession>();

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(Refresh);
            if (_mobilePlayButton != null)
                _mobilePlayButton.onClick.AddListener(() => SelectMode(PlayModeKind.Mobile));
            if (_vrPlayButton != null)
                _vrPlayButton.onClick.AddListener(() => SelectMode(PlayModeKind.Vr));
        }

        protected override void OnShow()
        {
            Refresh();
        }

        void Refresh()
        {
            if (_probe == null)
                return;

            _status = _probe.Refresh();
            Render(_status);
        }

        void Render(DeviceConnectionStatus status)
        {
            if (_platformText != null)
                _platformText.text = "Device: " + status.PlatformLabel;

            if (_xrStatusText != null)
            {
                var xrName = string.IsNullOrEmpty(status.XrDeviceName) ? "None" : status.XrDeviceName;
                _xrStatusText.text = status.VrStatusText + " (" + xrName + ")";
            }

            if (_mobileStatusText != null)
                _mobileStatusText.text = status.MobileStatusText;

            if (_mobilePlayButton != null)
                _mobilePlayButton.interactable = PlayModeSelection.CanSelect(PlayModeKind.Mobile, status.Availability);

            if (_vrPlayButton != null)
                _vrPlayButton.interactable = PlayModeSelection.CanSelect(PlayModeKind.Vr, status.Availability);

            if (_messageText != null)
            {
                _messageText.text = status.Availability.VrAvailable
                    ? "VR device is connected. Choose a play mode."
                    : "No VR headset detected. Mobile Play is available on this device.";
            }
        }

        void SelectMode(PlayModeKind requestedMode)
        {
            var resolved = PlayModeSelection.ResolveSelectedMode(requestedMode, _status.Availability);
            if (resolved == PlayModeKind.None)
            {
                if (_messageText != null)
                    _messageText.text = "No playable mode is available. Check the connected device and refresh.";
                return;
            }

            if (_session != null)
                _session.SetSelectedMode(resolved);

            SceneManager.LoadScene(_gameplaySceneName);
        }
    }
}
```

- [ ] **Step 2: Compile**

Run edit-mode tests.

Expected: compile passes.

- [ ] **Step 3: Commit**

```powershell
git add project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs
git commit -m "feat: add device connection selection ui"
```

### Task 5: Update Rig Selector to Respect User Choice

**Files:**
- Modify: `project/Assets/_Project/Presentation/Gameplay/SuperhotPlaytestRigSelector.cs`

- [ ] **Step 1: Modify selector**

Replace the body of `SuperhotPlaytestRigSelector` with this behavior while preserving the namespace and attributes:

```csharp
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Startup;
using VRProject.Presentation.Startup;

namespace VRProject.Presentation.Gameplay
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class SuperhotPlaytestRigSelector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("When true, always use the flat rig even if an XR device is active (editor convenience).")]
        bool _forceFlatForTesting;

        [SerializeField]
        [Tooltip("When no startup selection exists, prefer XR if a headset is active.")]
        bool _autoUseXrWhenActive = true;

        void Awake()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: XRSettings.isDeviceActive && !_forceFlatForTesting);

            var selected = PlayModeSession.GetSelectedModeOrFallback(availability);
            if (_forceFlatForTesting)
                selected = PlayModeKind.Mobile;
            else if (selected == PlayModeKind.None && _autoUseXrWhenActive && XRSettings.isDeviceActive)
                selected = PlayModeKind.Vr;

            ApplyRigSelection(selected == PlayModeKind.Vr);
        }

        void ApplyRigSelection(bool useXr)
        {
            var xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var flatRig = FindFirstObjectByType<SuperhotFlatPlaytestRig>(FindObjectsInactive.Include);

            if (xrOrigin != null)
                xrOrigin.gameObject.SetActive(useXr);

            if (flatRig != null)
                flatRig.gameObject.SetActive(!useXr);
        }
    }
}
```

- [ ] **Step 2: Compile**

Run edit-mode tests.

Expected: compile passes.

- [ ] **Step 3: Manual check in editor**

Open a gameplay scene with both rigs. Add a temporary `PlayModeSession` in the scene, set mode to `Mobile`, and enter play mode.

Expected:

- XR Origin is inactive.
- Flat/mobile rig is active.
- Only one MainCamera is active.

Set mode to `Vr` and test with XR available.

Expected:

- XR Origin is active.
- Flat/mobile rig is inactive.

- [ ] **Step 4: Commit**

```powershell
git add project/Assets/_Project/Presentation/Gameplay/SuperhotPlaytestRigSelector.cs
git commit -m "feat: route gameplay rig from selected play mode"
```

### Task 6: Startup Scene Generator

**Files:**
- Create: `project/Assets/_Project/Editor/StartupSceneMenu.cs`
- Create through Unity menu: `project/Assets/Scenes/Startup.unity`

- [ ] **Step 1: Implement editor scene menu**

Create `project/Assets/_Project/Editor/StartupSceneMenu.cs`:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Startup;

namespace VRProject.EditorTools
{
    public static class StartupSceneMenu
    {
        const string StartupScenePath = "Assets/Scenes/Startup.unity";
        const string GameplaySceneName = "CrystalDefensePrototype";

        [MenuItem("VR Project/Scenes/Create Startup Device Selection Scene")]
        public static void CreateStartupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("StartupSystems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<PlayModeSession>();
            systems.AddComponent<DeviceConnectionProbe>();

            var canvasGo = new GameObject("StartupCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = CreatePanel(canvasGo.transform, "Root", new Vector2(0.5f, 0.5f), new Vector2(780f, 520f));
            var title = CreateText(root.transform, "Title", "Device Connection", 32, new Vector2(0f, 190f), new Vector2(700f, 60f));
            title.alignment = TextAnchor.MiddleCenter;

            var platformText = CreateText(root.transform, "PlatformText", "Device: Checking...", 22, new Vector2(0f, 120f), new Vector2(700f, 40f));
            var xrText = CreateText(root.transform, "XrStatusText", "VR Headset: Checking...", 22, new Vector2(0f, 70f), new Vector2(700f, 40f));
            var mobileText = CreateText(root.transform, "MobileStatusText", "Mobile Play: Checking...", 22, new Vector2(0f, 20f), new Vector2(700f, 40f));
            var messageText = CreateText(root.transform, "MessageText", "Choose a play mode after device status is checked.", 20, new Vector2(0f, -45f), new Vector2(700f, 60f));
            messageText.alignment = TextAnchor.MiddleCenter;

            var refreshButton = CreateButton(root.transform, "RefreshButton", "Refresh", new Vector2(-240f, -145f), new Vector2(160f, 54f));
            var mobileButton = CreateButton(root.transform, "MobilePlayButton", "Mobile Play", new Vector2(0f, -145f), new Vector2(180f, 54f));
            var vrButton = CreateButton(root.transform, "VrPlayButton", "VR Play", new Vector2(240f, -145f), new Vector2(180f, 54f));

            var view = root.gameObject.AddComponent<DeviceConnectionView>();
            var so = new SerializedObject(view);
            so.FindProperty("_gameplaySceneName").stringValue = GameplaySceneName;
            so.FindProperty("_platformText").objectReferenceValue = platformText;
            so.FindProperty("_xrStatusText").objectReferenceValue = xrText;
            so.FindProperty("_mobileStatusText").objectReferenceValue = mobileText;
            so.FindProperty("_messageText").objectReferenceValue = messageText;
            so.FindProperty("_refreshButton").objectReferenceValue = refreshButton;
            so.FindProperty("_mobilePlayButton").objectReferenceValue = mobileButton;
            so.FindProperty("_vrPlayButton").objectReferenceValue = vrButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StartupScenePath);
            AddSceneToBuildSettingsIfNeeded(StartupScenePath, first: true);
            AssetDatabase.Refresh();

            Debug.Log("[VR Project] Saved startup device selection scene: " + StartupScenePath);
        }

        static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.92f);
            return rect;
        }

        static Text CreateText(Transform parent, string name, string text, int size, Vector2 position, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            return label;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.42f, 0.75f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go.transform, "Text", label, 20, Vector2.zero, rectSize);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static void AddSceneToBuildSettingsIfNeeded(string scenePath, bool first)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (first)
                scenes.Insert(0, entry);
            else
                scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
```

- [ ] **Step 2: Run the scene menu**

In Unity, run:

```text
VR Project/Scenes/Create Startup Device Selection Scene
```

Expected:

- `Assets/Scenes/Startup.unity` is created.
- Startup scene is first in Build Settings.
- Canvas shows connection status labels and three buttons.

- [ ] **Step 3: Commit**

```powershell
git add project/Assets/_Project/Editor/StartupSceneMenu.cs project/Assets/Scenes/Startup.unity project/Assets/Scenes/Startup.unity.meta project/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: create startup device selection scene"
```

### Task 7: Gameplay Scene Integration

**Files:**
- Modify: `project/Assets/_Project/Editor/SuperhotPrototypeSceneMenu.cs`
- Modify generated scene if needed: `project/Assets/Scenes/CrystalDefensePrototype.unity`

- [ ] **Step 1: Ensure gameplay scene has both play rigs**

Inspect `SuperhotPrototypeSceneMenu.cs`. Existing scene generation already calls:

```csharp
InstantiateXrRigAndWireSystems(scene, systems, rigPrefabPath);
BuildFlatPlaytestRig(XrRigSpawnPosition);
```

Keep both calls in the crystal-defense scene generator. Do not delete either rig.

- [ ] **Step 2: Ensure selector is present**

In the crystal-defense scene generator, keep:

```csharp
systems.AddComponent<SuperhotPlaytestRigSelector>();
```

If missing in any gameplay scene generator used by the build, add it to `Systems`.

- [ ] **Step 3: Add gameplay scene after startup scene in Build Settings**

Make sure Build Settings order is:

1. `Assets/Scenes/Startup.unity`
2. `Assets/Scenes/CrystalDefensePrototype.unity`

If using a different final scene name, update `_gameplaySceneName` in `DeviceConnectionView` scene wiring.

- [ ] **Step 4: Manual play mode checks**

Editor, no headset:

- Start from `Startup.unity`.
- Mobile Play button is enabled.
- VR Play button is disabled.
- Click Mobile Play.
- `CrystalDefensePrototype` loads.
- Flat/mobile rig is active.

VR headset connected:

- Start from `Startup.unity`.
- VR Play button is enabled.
- Click VR Play.
- `CrystalDefensePrototype` loads.
- XR Origin is active.

Android tablet:

- Build to the connected device.
- Startup screen appears before gameplay.
- Mobile Play is enabled.
- VR Play is disabled unless XR runtime reports active.
- Mobile Play loads gameplay.

- [ ] **Step 5: Commit scene integration**

```powershell
git add project/Assets/_Project/Editor/SuperhotPrototypeSceneMenu.cs project/Assets/Scenes/CrystalDefensePrototype.unity project/Assets/Scenes/CrystalDefensePrototype.unity.meta project/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: integrate startup mode selection with gameplay scene"
```

### Task 8: Connection Help and User-Facing States

**Files:**
- Modify: `project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs`
- Modify: `project/Assets/_Project/Editor/StartupSceneMenu.cs`

- [ ] **Step 1: Add clear status messages**

Update `DeviceConnectionView.Render` message behavior:

```csharp
if (_messageText != null)
{
    if (status.Availability.VrAvailable && status.Availability.MobileAvailable)
        _messageText.text = "Both play modes are available. Choose how you want to play.";
    else if (status.Availability.MobileAvailable)
        _messageText.text = "Mobile Play is available. Connect a VR headset and press Refresh to enable VR Play.";
    else if (status.Availability.VrAvailable)
        _messageText.text = "VR Play is available. Mobile Play is unavailable on this platform.";
    else
        _messageText.text = "No playable mode is available. Check the connected device and press Refresh.";
}
```

- [ ] **Step 2: Add short connection instructions to startup scene**

In `StartupSceneMenu.cs`, add one more text line under the buttons:

```csharp
CreateText(
    root.transform,
    "HelpText",
    "Android tablet: use Mobile Play. Headset: connect/enable XR, then press Refresh.",
    16,
    new Vector2(0f, -210f),
    new Vector2(720f, 40f));
```

- [ ] **Step 3: Manual visual check**

Open `Startup.unity`.

Expected:

- Text fits inside the panel at desktop and tablet landscape aspect ratios.
- Buttons do not overlap.
- Disabled VR button is visibly disabled when no headset is active.

- [ ] **Step 4: Commit**

```powershell
git add project/Assets/_Project/Presentation/Startup/DeviceConnectionView.cs project/Assets/_Project/Editor/StartupSceneMenu.cs project/Assets/Scenes/Startup.unity
git commit -m "feat: clarify device connection status messages"
```

### Task 9: Build and Device Verification

**Files:**
- Create: `docs/performance/device-connection-play-mode-test-log.md`

- [ ] **Step 1: Create verification log**

Create `docs/performance/device-connection-play-mode-test-log.md`:

```markdown
# Device Connection and Play Mode Test Log

## Build

- Date:
- Unity version:
- Build target:
- Startup scene:
- Gameplay scene:

## Test Matrix

| Date | Device | Platform | XR Connected | Expected Buttons | Selected Mode | Result | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |

## Acceptance Checklist

- [ ] Startup scene appears before gameplay.
- [ ] Refresh updates platform and XR status.
- [ ] Mobile Play is available on Android tablet.
- [ ] VR Play is disabled when no XR device is active.
- [ ] VR Play is enabled when XR device is active.
- [ ] Mobile Play loads gameplay with flat/mobile rig active.
- [ ] VR Play loads gameplay with XR Origin active.
- [ ] Only one MainCamera is active after selection.
- [ ] Only one AudioListener is active after selection.
- [ ] Returning to startup and choosing another mode works after app restart.
```

- [ ] **Step 2: Test in editor without headset**

Expected:

- Mobile Play enabled.
- VR Play disabled.
- Mobile Play loads gameplay and flat/mobile rig.

- [ ] **Step 3: Test Android tablet connection**

Build and run on the connected mobile device.

Expected:

- Startup scene appears.
- Device label shows Android Device.
- Mobile Play enabled.
- VR Play disabled.
- Mobile Play enters gameplay.

- [ ] **Step 4: Test VR headset**

Run with an XR device active.

Expected:

- Startup scene reports XR device connected.
- VR Play enabled.
- VR Play enters gameplay with XR Origin active.

- [ ] **Step 5: Commit test log**

```powershell
git add docs/performance/device-connection-play-mode-test-log.md
git commit -m "test: record device connection play mode verification"
```

## Execution Order

Recommended order:

1. Task 1, because every decision depends on pure mode-selection logic.
2. Task 2 and Task 3, because UI needs device status and persistent selection.
3. Task 4, because it creates runtime UI behavior.
4. Task 5, because gameplay must respect the selected mode.
5. Task 6, because it creates the startup scene.
6. Task 7 and Task 8, because they wire and polish the flow.
7. Task 9 last, on real devices.

## Acceptance Criteria

This feature is complete when:

- `PlayModeSelectionTests` pass.
- `Assets/Scenes/Startup.unity` exists and is first in Build Settings.
- Startup screen shows device/platform status and XR connection status.
- Refresh button updates the connection status.
- Mobile Play and VR Play buttons reflect availability.
- Selected mode persists into the gameplay scene.
- Gameplay scene activates only the selected rig.
- Android tablet build can enter Mobile Play.
- XR headset run can enter VR Play.
- Verification log contains results for editor, Android tablet, and VR headset where available.

## Handoff Notes for the Next Agent

- Read this plan together with the crystal-defense and mobile optimization plans.
- Do not remove the existing automatic fallback behavior entirely; it protects direct scene play in editor.
- Do not assume a VR headset is connected just because the build target is Android. Galaxy Tab S7 FE should use Mobile Play.
- Keep startup UI simple and reliable. This is a connection and mode-selection screen, not a settings menu.
- If TextMeshPro is already standard in the branch, it is acceptable to replace `UnityEngine.UI.Text` with TMP types, but update the scene generator and code consistently.
- Make small commits after each task.

## Self-Review

- Spec coverage: The plan covers connection screen, device status screen, refresh behavior, Mobile vs VR choice, persistent selection, gameplay rig activation, startup scene generation, build settings, and device verification.
- Placeholder scan: No unresolved placeholder markers are left in implementation steps. The verification log intentionally has blank fields for real test data.
- Type consistency: New code consistently uses `PlayModeKind`, `PlayModeAvailability`, `PlayModeSelection`, `DeviceConnectionProbe`, `PlayModeSession`, and `DeviceConnectionView`.
- Scope check: This plan only creates the startup/device-selection flow. It does not implement the crystal-defense gameplay itself or the deeper performance optimizations.
