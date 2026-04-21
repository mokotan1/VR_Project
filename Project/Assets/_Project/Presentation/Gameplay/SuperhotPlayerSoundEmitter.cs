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

        void Awake()
        {
            _flatController = GetComponent<SuperhotFlatFpsController>();
        }

        void OnEnable()
        {
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
            _emitTimer -= Time.unscaledDeltaTime;
            if (_emitTimer > 0f)
                return;

            var speed = ComputeSpeed();
            if (speed < _moveSpeedThreshold)
                return;

            _emitTimer = _emitInterval;
            SuperhotSoundChannel.Emit(new SuperhotSoundEvent(transform.position, _soundRadius));
        }

        float ComputeSpeed()
        {
            if (_flatController != null)
                return _flatController.LastPlanarSpeedMetersPerSecond;

            if (_hmd != null)
            {
                var udt = Mathf.Max(Time.unscaledDeltaTime, 1e-6f);
                var speed = (_hmd.position - _lastHmdPos).magnitude / udt;
                _lastHmdPos = _hmd.position;
                return speed;
            }

            return 0f;
        }
    }
}
