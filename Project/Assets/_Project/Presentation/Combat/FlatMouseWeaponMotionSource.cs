using UnityEngine;
using UnityEngine.InputSystem;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Flat/desktop playtest: mouse delta drives a virtual weapon tip around the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlatMouseWeaponMotionSource : MonoBehaviour, IWeaponMotionSource
    {
        const float MouseDeltaToMeters = 0.004f;
        const float ReachMeters = 0.85f;

        [SerializeField] Transform _camera;
        [SerializeField] Transform _tip;
        [SerializeField] Transform _handle;
        [SerializeField] Transform _forwardReference;

        Vector3 _virtualTipOffset = new Vector3(0.25f, -0.15f, 0.55f);

        public bool IsActive => enabled && _camera != null;

        void Reset()
        {
            _forwardReference = transform;
            _handle = transform;
            _tip = transform;
        }

        void Update()
        {
            if (_camera == null || Mouse.current == null)
                return;

            var delta = Mouse.current.delta.ReadValue();
            _virtualTipOffset += new Vector3(delta.x, delta.y, 0f) * MouseDeltaToMeters;
            _virtualTipOffset = Vector3.ClampMagnitude(_virtualTipOffset, ReachMeters);

            var worldTip = _camera.TransformPoint(_virtualTipOffset);
            var worldHandle = _camera.TransformPoint(_virtualTipOffset * 0.35f);

            ApplyWeaponRootPose(worldHandle, worldTip);

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

        void ApplyWeaponRootPose(Vector3 worldHandle, Vector3 worldTip)
        {
            if (_handle == null)
                return;

            var forward = worldTip - worldHandle;
            if (forward.sqrMagnitude < 1e-6f)
                forward = _camera.forward;

            var rotation = Quaternion.LookRotation(forward.normalized, _camera.up);
            transform.rotation = rotation;
            transform.position = worldHandle - rotation * _handle.localPosition;
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
