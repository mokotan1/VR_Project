using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Enables either the XR rig or the flat desktop playtest rig so only one MainCamera and AudioListener are active.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class SuperhotPlaytestRigSelector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("When true, always use the flat rig even if an XR device is active (editor convenience).")]
        bool _forceFlatForTesting;

        void Awake()
        {
            var useXr = XRSettings.isDeviceActive && !_forceFlatForTesting;

            var xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var flatRig = FindFirstObjectByType<SuperhotFlatPlaytestRig>(FindObjectsInactive.Include);

            if (xrOrigin != null)
                xrOrigin.gameObject.SetActive(useXr);

            if (flatRig != null)
                flatRig.gameObject.SetActive(!useXr);
        }
    }
}
