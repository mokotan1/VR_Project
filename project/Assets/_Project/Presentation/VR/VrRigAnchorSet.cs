using UnityEngine;

namespace VRProject.Presentation.VR
{
    [DisallowMultipleComponent]
    public sealed class VrRigAnchorSet : MonoBehaviour
    {
        [SerializeField] Transform _headAnchor;
        [SerializeField] Transform _rightHandAnchor;
        [SerializeField] Transform _leftHandAnchor;

        public Transform HeadAnchor => _headAnchor;
        public Transform RightHandAnchor => _rightHandAnchor;
        public Transform LeftHandAnchor => _leftHandAnchor;

        public bool HasRequiredAnchors =>
            _headAnchor != null && _rightHandAnchor != null && _leftHandAnchor != null;

        public void AutoBindMissingReferences()
        {
            var root = transform;
            if (_headAnchor == null)
                _headAnchor = VrWeaponGripPoseResolver.FindByName(root, VrRigAnchorNames.HeadAnchor);
            if (_rightHandAnchor == null)
                _rightHandAnchor = VrWeaponGripPoseResolver.FindByName(root, VrRigAnchorNames.RightHandAnchor);
            if (_leftHandAnchor == null)
                _leftHandAnchor = VrWeaponGripPoseResolver.FindByName(root, VrRigAnchorNames.LeftHandAnchor);
        }

        void Reset() => AutoBindMissingReferences();

        void OnValidate() => AutoBindMissingReferences();
    }
}
