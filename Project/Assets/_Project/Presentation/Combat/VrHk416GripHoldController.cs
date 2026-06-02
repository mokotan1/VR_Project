using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// While grip is held (simulator G), snaps the nearest floor HK416 to the right hand.
    /// Releasing grip drops the weapon back to the scene.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class VrHk416GripHoldController : MonoBehaviour
    {
        [SerializeField] XROrigin _xrOrigin;
        [SerializeField] VrSceneWeaponSnapInput _snapInput;
        [SerializeField] XRNode _hand = XRNode.RightHand;
        [SerializeField, Range(0.01f, 1f)] float _analogGripThreshold = 0.55f;
        [SerializeField, Min(0.5f)] float _snapPickupMaxDistance = 2.75f;

        bool _wasGripHeld;

        void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponent<XROrigin>();
            if (_snapInput == null)
                _snapInput = GetComponent<VrSceneWeaponSnapInput>();
        }

        void Update()
        {
            if (_snapInput == null)
                return;

            if (!VrPlaytestControllerInput.TryReadGripHeld(_hand, _analogGripThreshold, out var gripHeld))
                return;

            if (gripHeld && !_wasGripHeld)
                _snapInput.TrySnapNearestHk416PickupToRightHand(_snapPickupMaxDistance);

            if (!gripHeld && _wasGripHeld)
                _snapInput.TryReleaseHk416FromRightHand();

            _wasGripHeld = gripHeld;
        }
    }
}
