using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Startup;
using VRProject.Presentation.Combat;
using VRProject.Presentation.PrototypeFps;
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
                vrAvailable: (XRSettings.isDeviceActive || UnityEngine.Application.isEditor) && !_forceFlatForTesting);

            var selected = PlayModeSession.GetSelectedModeOrFallback(availability);
            if (_forceFlatForTesting)
                selected = PlayModeKind.Mobile;
            else if (selected == PlayModeKind.None && _autoUseXrWhenActive && XRSettings.isDeviceActive)
                selected = PlayModeKind.Vr;

            ApplyRigSelection(selected == PlayModeKind.Vr);
        }

        void ApplyRigSelection(bool useXr)
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include);
            var flatRig = FindAnyObjectByType<SuperhotFlatPlaytestRig>(FindObjectsInactive.Include);

            if (xrOrigin != null)
            {
                xrOrigin.gameObject.SetActive(useXr);
                if (useXr)
                    PromoteXrOriginToPlayer(xrOrigin);
            }

            if (flatRig != null)
                flatRig.gameObject.SetActive(!useXr);

            ApplyLegacyPlayerVisibility(useXr, xrOrigin, flatRig);
        }

        static void PromoteXrOriginToPlayer(XROrigin xrOrigin)
        {
            var root = xrOrigin.gameObject;
            TrySetTag(root, "Player");

            if (root.GetComponent<SuperhotPlaytestPlayerHealth>() == null)
                root.AddComponent<SuperhotPlaytestPlayerHealth>();
            if (root.GetComponent<PrototypeFpsPlayerHealth>() == null)
                root.AddComponent<PrototypeFpsPlayerHealth>();

            var snapInput = root.GetComponent<VrSceneWeaponSnapInput>();
            if (snapInput == null)
                snapInput = root.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(xrOrigin);
        }

        static void ApplyLegacyPlayerVisibility(bool useXr, XROrigin xrOrigin, SuperhotFlatPlaytestRig flatRig)
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i].gameObject;
                if (!IsLegacyPlayerRoot(candidate, xrOrigin, flatRig))
                    continue;

                candidate.SetActive(!useXr);
            }
        }

        static bool IsLegacyPlayerRoot(GameObject candidate, XROrigin xrOrigin, SuperhotFlatPlaytestRig flatRig)
        {
            if (candidate == null)
                return false;
            if (candidate.transform.root != candidate.transform)
                return false;
            if (xrOrigin != null && candidate == xrOrigin.gameObject)
                return false;
            if (flatRig != null && candidate == flatRig.gameObject)
                return false;

            return candidate.CompareTag("Player") || candidate.name == "UnityChan_Player";
        }

        static void TrySetTag(GameObject target, string tagName)
        {
            try
            {
                target.tag = tagName;
            }
            catch (UnityException)
            {
                Debug.LogWarning("[VR Project] Tag \"" + tagName + "\" is not defined; XR Origin cannot be promoted to tagged Player.", target);
            }
        }
    }
}
