using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Tracks the grabbed weapon root / attach transform for VR motion sampling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VrGrabbedWeaponMotionSource : MonoBehaviour, IWeaponMotionSource
    {
        [SerializeField] Transform _tip;
        [SerializeField] Transform _handle;
        [SerializeField] Transform _forwardReference;

        XRGrabInteractable _grab;
        bool _isSelected;

        public bool IsActive => _isSelected && enabled;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            if (_grab != null)
            {
                _grab.selectEntered.AddListener(_ =>
                {
                    _isSelected = true;
                    var binder = GetComponent<MeleeWeaponRuntimeBinder>();
                    if (binder != null)
                        binder.PickUpForFlatOrMobile();
                });
                _grab.selectExited.AddListener(_ => _isSelected = false);
            }
        }

        void OnDestroy()
        {
            if (_grab == null)
                return;
            _grab.selectEntered.RemoveAllListeners();
            _grab.selectExited.RemoveAllListeners();
        }

        public WeaponMotionPose SamplePose()
        {
            var tip = _tip != null ? _tip.position : transform.position;
            var handle = _handle != null ? _handle.position : transform.position;
            var forwardRef = _forwardReference != null ? _forwardReference : transform;
            return new WeaponMotionPose(tip, handle, forwardRef.forward, forwardRef.right);
        }
    }
}
