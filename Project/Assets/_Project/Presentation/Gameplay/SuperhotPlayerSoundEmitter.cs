using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SuperhotPlayerSoundEmitter : MonoBehaviour
    {
        [SerializeField] float _soundRadius = 12f;
        [SerializeField] float _moveSpeedThreshold = 0.15f;
        [SerializeField] float _emitInterval = 0.15f;

        SuperhotFlatFpsController _flatController;
        float _emitTimer;
        Vector3 _lastHmdPos;
        Transform _hmd;
        Vector3 _lastGenericPos;

        void Awake()
        {
            _flatController = GetComponent<SuperhotFlatFpsController>();
        }

        void OnEnable()
        {
            _lastGenericPos = transform.position;

            if (XRSettings.isDeviceActive)
            {
                var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.Camera != null)
                {
                    _hmd = origin.Camera.transform;
                    _lastHmdPos = _hmd.position;
                }
            }
        }

        void Update()
        {
            // ComputeSpeed must run every frame to keep _lastHmdPos current
            var speed = ComputeSpeed();

            _emitTimer -= Time.unscaledDeltaTime;
            if (_emitTimer > 0f)
                return;

            if (speed < _moveSpeedThreshold)
                return;

            _emitTimer = _emitInterval;
            SuperhotSoundChannel.Emit(new SuperhotSoundEvent(transform.position, _soundRadius));
        }

        float ComputeSpeed()
        {
            if (_flatController != null)
                return _flatController.LastPlanarSpeedMetersPerSecond;

            // Lazy re-lookup in case XR initialized after OnEnable
            if (_hmd == null && XRSettings.isDeviceActive)
            {
                var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.Camera != null)
                {
                    _hmd = origin.Camera.transform;
                    _lastHmdPos = _hmd.position;
                }
            }

            if (_hmd != null)
            {
                var udt = Mathf.Max(Time.unscaledDeltaTime, 1e-6f);
                var speed = (_hmd.position - _lastHmdPos).magnitude / udt;
                _lastHmdPos = _hmd.position;
                return speed;
            }

            // Generic fallback: track this transform's world position delta
            {
                var udt = Mathf.Max(Time.unscaledDeltaTime, 1e-6f);
                var speed = (transform.position - _lastGenericPos).magnitude / udt;
                _lastGenericPos = transform.position;
                return speed;
            }
        }
    }
}
