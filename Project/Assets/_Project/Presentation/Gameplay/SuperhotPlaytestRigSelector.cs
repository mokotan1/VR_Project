using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Startup;
using VRProject.Presentation.Startup;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Activates exactly one playtest rig (XR Origin or flat/mobile rig)
    /// based on the user's selection from the startup scene. Falls back to
    /// safe automatic behavior when no <see cref="PlayModeSession"/> exists
    /// so direct-scene playtests in the editor still work.
    /// </summary>
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
