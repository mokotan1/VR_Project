using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Gameplay;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Samples HMD vs controller motion with separate weights and dead-zones, then drives <see cref="IGameplayClock"/>.
    /// When no XR device is active (e.g. flat editor), time factor stays at max for usability.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class SuperhotGameplayDriver : MonoBehaviour
    {
        [SerializeField] Unity.XR.CoreUtils.XROrigin _xrOrigin;

        [Tooltip("Usually the HMD / main XR camera transform.")]
        [SerializeField] Transform _hmd;

        [SerializeField] Transform _leftController;

        [SerializeField] Transform _rightController;

        [Header("Motion normalization (m/s)")]
        [SerializeField] float _headReferenceSpeed = 1.2f;

        [SerializeField] float _headDeadZoneSpeed = 0.02f;

        [SerializeField] float _handReferenceSpeed = 2.0f;

        [SerializeField] float _handDeadZoneSpeed = 0.35f;

        [Header("Weights (HMD should dominate to avoid wrist-cheese)")]
        [SerializeField] float _headWeight = 0.85f;

        [SerializeField] float _handWeight = 0.15f;

        [Header("Time curve")]
        [SerializeField] float _minTimeFactor = 0.02f;

        [SerializeField] float _maxTimeFactor = 1f;

        [SerializeField] float _smoothTimeSeconds = 0.08f;

        IGameplayClock _clock;
        Vector3 _prevHmd;
        Vector3 _prevLeft;
        Vector3 _prevRight;
        bool _hasPrev;
        float _smoothedTimeFactor;
        float _smoothVelocity;

        void Awake()
        {
            var locator = ServiceLocator.Instance;
            if (locator.IsRegistered<IGameplayClock>())
                _clock = locator.Resolve<IGameplayClock>();
            else
                Debug.LogError("[SuperhotGameplayDriver] IGameplayClock is not registered. Add GameplayClockService in GameBootstrapper.", this);

            AutoWireIfNeeded();
        }

        void OnEnable()
        {
            _hasPrev = false;
        }

        void Update()
        {
            if (_clock == null)
                return;

            var unscaledDt = Time.unscaledDeltaTime;
            if (unscaledDt <= 0f)
                return;

            if (!XRSettings.isDeviceActive)
            {
                _clock.BeginFrame(unscaledDt, _maxTimeFactor);
                return;
            }

            AutoWireIfNeeded();
            if (_hmd == null)
            {
                _clock.BeginFrame(unscaledDt, _maxTimeFactor);
                return;
            }

            var hmdPos = _hmd.position;
            var leftPos = _leftController != null ? _leftController.position : hmdPos;
            var rightPos = _rightController != null ? _rightController.position : hmdPos;

            if (!_hasPrev)
            {
                _prevHmd = hmdPos;
                _prevLeft = leftPos;
                _prevRight = rightPos;
                _hasPrev = true;
                _clock.BeginFrame(unscaledDt, _minTimeFactor);
                return;
            }

            var headSpeed = (hmdPos - _prevHmd).magnitude / unscaledDt;
            var leftSpeed = (leftPos - _prevLeft).magnitude / unscaledDt;
            var rightSpeed = (rightPos - _prevRight).magnitude / unscaledDt;
            var handSpeed = Mathf.Max(leftSpeed, rightSpeed);

            _prevHmd = hmdPos;
            _prevLeft = leftPos;
            _prevRight = rightPos;

            var head01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                headSpeed,
                _headDeadZoneSpeed,
                _headReferenceSpeed);

            var hand01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                handSpeed,
                _handDeadZoneSpeed,
                _handReferenceSpeed);

            var blended = SuperhotTimeScaleCalculator.BlendWeightedMotion(
                head01,
                hand01,
                _headWeight,
                _handWeight);

            var targetFactor = SuperhotTimeScaleCalculator.ToTimeFactor(blended, _minTimeFactor, _maxTimeFactor);
            _smoothedTimeFactor = SuperhotTimeScaleCalculator.SmoothTowards(
                _smoothedTimeFactor,
                targetFactor,
                ref _smoothVelocity,
                _smoothTimeSeconds,
                unscaledDt);

            _clock.BeginFrame(unscaledDt, _smoothedTimeFactor);
        }

        void AutoWireIfNeeded()
        {
            if (_xrOrigin == null)
                _xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();

            if (_hmd == null && _xrOrigin != null && _xrOrigin.Camera != null)
                _hmd = _xrOrigin.Camera.transform;

            if (_xrOrigin == null)
                return;

            if (_leftController == null)
                _leftController = FindChildTransformByName(_xrOrigin.transform, "Left Controller");
            if (_rightController == null)
                _rightController = FindChildTransformByName(_xrOrigin.transform, "Right Controller");
        }

        static Transform FindChildTransformByName(Transform root, string exactName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == exactName)
                    return t;
            }

            return null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _headReferenceSpeed = Mathf.Max(0.01f, _headReferenceSpeed);
            _handReferenceSpeed = Mathf.Max(0.01f, _handReferenceSpeed);
            _headDeadZoneSpeed = Mathf.Max(0f, _headDeadZoneSpeed);
            _handDeadZoneSpeed = Mathf.Max(0f, _handDeadZoneSpeed);
            _headWeight = Mathf.Max(0f, _headWeight);
            _handWeight = Mathf.Max(0f, _handWeight);
            _minTimeFactor = Mathf.Clamp(_minTimeFactor, 0.0001f, 1f);
            _maxTimeFactor = Mathf.Clamp(_maxTimeFactor, _minTimeFactor, 1f);
        }
#endif
    }
}
