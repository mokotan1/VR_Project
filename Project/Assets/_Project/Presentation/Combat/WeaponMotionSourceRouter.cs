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
        Rigidbody _body;

        public IWeaponMotionSource ActiveSource => _activeSource;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            EnsureSolidColliderForPhysicsBody();
            if (_vrSource == null)
                _vrSource = GetComponent<VrGrabbedWeaponMotionSource>();
            if (_flatSource == null)
                _flatSource = GetComponent<FlatMouseWeaponMotionSource>();
            if (_mobileSource == null)
                _mobileSource = GetComponent<MobileTouchWeaponMotionSource>();

            SelectSource();
        }

        void EnsureSolidColliderForPhysicsBody()
        {
            if (_body == null)
                return;

            foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && !collider.isTrigger)
                    return;
            }

            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.size = new Vector3(0.26f, 0.14f, 0.92f);
            box.center = new Vector3(0f, 0f, 0.32f);
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
                ConfigureBodyForHeldFlatOrMobile(false);
                return;
            }

            if (MobileTouchInputBus.ShouldUseMobileControls())
            {
                SetActive(_mobileSource, true);
                _activeSource = _mobileSource;
                ConfigureBodyForHeldFlatOrMobile(true);
                return;
            }

            SetActive(_flatSource, true);
            _activeSource = _flatSource;
            ConfigureBodyForHeldFlatOrMobile(true);
        }

        static void SetActive(MonoBehaviour source, bool active)
        {
            if (source != null)
                source.enabled = active;
        }

        void ConfigureBodyForHeldFlatOrMobile(bool held)
        {
            if (_body == null)
                return;

            _body.isKinematic = held;
            _body.useGravity = !held;
            if (held)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
        }
    }
}
