using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using VRProject.Presentation.Common.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Mobile/tablet: melee swing drag from the dedicated touch band via <see cref="MobileTouchInputBus"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileTouchWeaponMotionSource : MonoBehaviour, IWeaponMotionSource
    {
        const float TouchDeltaToMeters = 0.0035f;
        const float ReachMeters = 0.8f;

        [SerializeField] Transform _camera;
        [SerializeField] Transform _tip;
        [SerializeField] Transform _handle;
        [SerializeField] Transform _forwardReference;

        Vector3 _virtualTipOffset = new Vector3(0.2f, -0.1f, 0.5f);

        public bool IsActive => enabled && _camera != null;

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            if (_camera == null)
                return;

            var bus = MobileTouchInputBus.Instance;
            if (bus != null && bus.IsMobileModeActive)
            {
                var snapshot = bus.Snapshot;
                if (snapshot.MeleeSwingActive)
                {
                    _virtualTipOffset += new Vector3(snapshot.MeleeSwingDeltaX, snapshot.MeleeSwingDeltaY, 0f) * TouchDeltaToMeters;
                    _virtualTipOffset = Vector3.ClampMagnitude(_virtualTipOffset, ReachMeters);
                }
            }
            else
            {
                ApplyLegacyFirstTouchFallback();
            }

            ApplyPose();
        }

        void ApplyLegacyFirstTouchFallback()
        {
            if (Touch.activeTouches.Count == 0)
                return;

            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                var delta = touch.delta;
                _virtualTipOffset += new Vector3(delta.x, delta.y, 0f) * TouchDeltaToMeters;
                _virtualTipOffset = Vector3.ClampMagnitude(_virtualTipOffset, ReachMeters);
            }
        }

        void ApplyPose()
        {
            var worldTip = _camera.TransformPoint(_virtualTipOffset);
            var worldHandle = _camera.TransformPoint(_virtualTipOffset * 0.35f);

            if (_tip != null)
                _tip.position = worldTip;
            if (_handle != null)
                _handle.position = worldHandle;
            if (_forwardReference != null)
            {
                _forwardReference.position = worldHandle;
                _forwardReference.rotation = _camera.rotation;
            }
        }

        public WeaponMotionPose SamplePose()
        {
            var tip = _tip != null ? _tip.position : transform.position;
            var handle = _handle != null ? _handle.position : transform.position;
            var forwardRef = _forwardReference != null ? _forwardReference : transform;
            return new WeaponMotionPose(tip, handle, forwardRef.forward, forwardRef.right);
        }

        public void BindCamera(Transform camera)
        {
            _camera = camera;
        }
    }
}
