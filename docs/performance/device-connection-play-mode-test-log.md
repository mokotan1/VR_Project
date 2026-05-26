# Device Connection and Play Mode Test Log

This log records device verification runs for the startup device-connection flow introduced by `docs/superpowers/plans/2026-05-26-device-connection-and-play-mode-selection.md`. Append a new row to the test matrix for every fresh build.

## Build

- Date:
- Unity version: 6000.x
- Build target:
- Startup scene: `Assets/Scenes/Startup.unity`
- Gameplay scene: `Assets/Scenes/CrystalDefensePrototype.unity`

## Test Matrix

| Date | Device | Platform | XR Connected | Expected Buttons | Selected Mode | Result | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | Unity Editor (Windows) | Editor | No | Mobile=enabled, VR=disabled | Mobile | | |
| | Unity Editor (Windows) + Quest Link | Editor | Yes | Mobile=enabled, VR=enabled | VR | | |
| | Galaxy Tab S7 FE | Android | No | Mobile=enabled, VR=disabled | Mobile | | |
| | Meta Quest 3 standalone | Android (XR) | Yes | Mobile=disabled or enabled, VR=enabled | VR | | |

## Acceptance Checklist

Run through this checklist for every build before claiming this slice is done.

- [ ] Startup scene appears before gameplay.
- [ ] Refresh updates platform and XR status.
- [ ] Mobile Play is available on Android tablet.
- [ ] VR Play is disabled when no XR device is active.
- [ ] VR Play is enabled when an XR device is active.
- [ ] Mobile Play loads gameplay with the flat/mobile rig active.
- [ ] VR Play loads gameplay with the XR Origin active.
- [ ] Only one MainCamera is active after selection.
- [ ] Only one AudioListener is active after selection.
- [ ] Returning to startup and choosing another mode works after app restart.
- [ ] `PlayModeSelectionTests` (8 NUnit cases) pass in EditMode.

## Manual Editor Verification Steps

These are the steps a developer should perform after merging the startup flow before publishing a build.

1. **Generate the scenes**
   - `VR Project → Scenes → Create Startup Device Selection Scene` (saves `Assets/Scenes/Startup.unity`, inserts as Build Settings #0).
   - `VR Project → Scenes → Create Crystal Defense Prototype Scene` (saves `Assets/Scenes/CrystalDefensePrototype.unity`, auto-bakes the NavMesh, appends to Build Settings).
2. **Verify Build Settings order**
   - `File → Build Settings`. The expected order is:
     1. `Assets/Scenes/Startup.unity`
     2. `Assets/Scenes/CrystalDefensePrototype.unity`
3. **Editor playtest, no headset**
   - Open `Assets/Scenes/Startup.unity` and press Play.
   - Confirm Mobile Play is enabled and VR Play is disabled.
   - Click `Mobile Play`. Confirm `CrystalDefensePrototype` loads with the flat rig active and the XR Origin disabled.
4. **Editor playtest with headset linked**
   - Reopen Startup. Confirm VR Play becomes enabled and the XR device name appears in the VR status line.
   - Click `VR Play`. Confirm XR Origin is active and the flat rig is disabled.
5. **Refresh round-trip**
   - On the startup screen, unplug or disable the headset and press `Refresh`. Confirm the VR button disables itself and the message line updates.
6. **EditMode test run**
   - `Window → General → Test Runner → EditMode → Run All`.
   - All `PlayModeSelectionTests` should pass.

## Android Tablet Verification (Galaxy Tab S7 FE)

1. Plug the tablet over USB, enable developer mode.
2. `File → Build Settings → Switch Platform → Android`.
3. Build & Run on the device.
4. Expected: startup scene appears with Mobile Play enabled, VR Play disabled, and `Mobile Play` enters gameplay with touch input on the flat rig.

## VR Headset Verification (Meta Quest)

1. Confirm Quest 3 / 3S / 2 is connected (Quest Link or APK install).
2. Build & Run with XR Plugin → Oculus enabled.
3. Expected: startup scene appears in-headset (or via Link mirror), VR Play is enabled, and selecting `VR Play` loads gameplay with the XR Origin active.

## Known Limitations

- This slice does not implement mobile-specific touch UI for gameplay (deferred to the Galaxy Tab S7 FE optimization plan).
- This slice does not adjust VR HUD layout for in-game UI (deferred to the Quest optimization plan).
- Unity batchmode EditMode runs are blocked while the Editor is open; verification is currently manual through the Test Runner window.
