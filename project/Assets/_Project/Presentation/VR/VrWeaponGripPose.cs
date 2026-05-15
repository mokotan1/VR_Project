using UnityEngine;

namespace VRProject.Presentation.VR
{
    [DisallowMultipleComponent]
    public sealed class VrWeaponGripPose : MonoBehaviour
    {
        [SerializeField] Transform _rightHandGrip;
        [SerializeField] Transform _leftHandGrip;
        [SerializeField] Transform _muzzle;
        [SerializeField] Transform _aimReference;

        public Transform RightHandGrip => _rightHandGrip;
        public Transform LeftHandGrip => _leftHandGrip;
        public Transform Muzzle => _muzzle;
        public Transform AimReference => _aimReference;

        public bool HasRequiredMarkers => _rightHandGrip != null && _leftHandGrip != null && _muzzle != null;

        public void AutoBindMissingReferences()
        {
            var root = transform;
            if (_rightHandGrip == null)
                _rightHandGrip = VrWeaponGripPoseResolver.FindByName(root, VrWeaponGripPoseNames.RightHandGrip);
            if (_leftHandGrip == null)
                _leftHandGrip = VrWeaponGripPoseResolver.FindByName(root, VrWeaponGripPoseNames.LeftHandGrip);
            if (_muzzle == null)
            {
                _muzzle = VrWeaponGripPoseResolver.FindByName(
                    root,
                    VrWeaponGripPoseNames.Muzzle,
                    VrWeaponGripPoseNames.WeaponFirePoint);
            }

            if (_aimReference == null)
                _aimReference = VrWeaponGripPoseResolver.FindByName(root, VrWeaponGripPoseNames.AimReference);
            if (_aimReference == null)
                _aimReference = _muzzle;
        }

        void Reset() => AutoBindMissingReferences();

        void OnValidate() => AutoBindMissingReferences();
    }
}
