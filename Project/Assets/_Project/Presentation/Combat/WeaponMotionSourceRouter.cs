using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using VRProject.Application.Startup;
using VRProject.Presentation.Common.UI;
using VRProject.Presentation.Startup;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Enables the correct <see cref="IWeaponMotionSource"/> for VR, mobile touch, or flat mouse playtest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponMotionSourceRouter : MonoBehaviour
    {
        [SerializeField] VrGrabbedWeaponMotionSource _vrSource;
        [SerializeField] FlatMouseWeaponMotionSource _flatSource;
        [SerializeField] MobileTouchWeaponMotionSource _mobileSource;

        IWeaponMotionSource _activeSource;

        public IWeaponMotionSource ActiveSource => _activeSource;

        void Awake()
        {
            if (_vrSource == null)
                _vrSource = GetComponent<VrGrabbedWeaponMotionSource>();
            if (_flatSource == null)
                _flatSource = GetComponent<FlatMouseWeaponMotionSource>();
            if (_mobileSource == null)
                _mobileSource = GetComponent<MobileTouchWeaponMotionSource>();

            SelectSource();
        }

        void SelectSource()
        {
            var availability = new PlayModeAvailability(mobileAvailable: true, vrAvailable: XRSettings.isDeviceActive);
            var mode = PlayModeSession.GetSelectedModeOrFallback(availability);

            SetActive(_vrSource, false);
            SetActive(_flatSource, false);
            SetActive(_mobileSource, false);

            if (mode == PlayModeKind.Vr)
            {
                SetActive(_vrSource, true);
                _activeSource = _vrSource;
                return;
            }

            if (MobileTouchInputBus.ShouldUseMobileControls())
            {
                SetActive(_mobileSource, true);
                _activeSource = _mobileSource;
                return;
            }

            SetActive(_flatSource, true);
            _activeSource = _flatSource;
        }

        static void SetActive(MonoBehaviour source, bool active)
        {
            if (source != null)
                source.enabled = active;
        }
    }
}
