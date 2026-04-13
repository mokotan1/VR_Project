using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Gameplay;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Samples motion (XR: HMD + hands; flat: WASD 평면 이동 — 옵션으로 마우스 시점 포함) 후 <see cref="IGameplayClock"/>을 갱신합니다.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class SuperhotGameplayDriver : MonoBehaviour
    {
        [Header("VR 연결 (헤드셋 쓸 때만 채우면 됨)")]
        [Tooltip("XR Origin. 비우면 씬에서 자동으로 찾습니다.")]
        [SerializeField] Unity.XR.CoreUtils.XROrigin _xrOrigin;

        [Tooltip("HMD(머리) 카메라 트랜스폼. 비우면 XR Origin의 메인 카메라를 씁니다.")]
        [SerializeField] Transform _hmd;

        [Tooltip("왼쪽 컨트롤러. 비우면 이름으로 ‘Left Controller’를 찾습니다.")]
        [SerializeField] Transform _leftController;

        [Tooltip("오른쪽 컨트롤러. 비우면 이름으로 ‘Right Controller’를 찾습니다.")]
        [SerializeField] Transform _rightController;

        [Header("VR — 움직임 민감도 (m/s)")]
        [Tooltip("머리가 이 정도 속도면 ‘완전히 움직임’으로 간주합니다.")]
        [Range(0.05f, 5f)]
        [SerializeField] float _headReferenceSpeed = 1.2f;

        [Tooltip("이보다 느린 머리 움직임은 무시합니다(미세 떨림 제거).")]
        [Range(0f, 1f)]
        [SerializeField] float _headDeadZoneSpeed = 0.02f;

        [Tooltip("손이 이 정도 속도면 ‘완전히 움직임’으로 간주합니다.")]
        [Range(0.05f, 8f)]
        [SerializeField] float _handReferenceSpeed = 2.0f;

        [Tooltip("이보다 느린 손 움직임은 무시합니다.")]
        [Range(0f, 2f)]
        [SerializeField] float _handDeadZoneSpeed = 0.35f;

        [Header("VR — 머리 vs 손 비중")]
        [Tooltip("시간 진행에 머리 움직임이 차지하는 비중. 높을수록 손만 흔들어서 시간을 잘 못 풀게 됩니다.")]
        [Range(0f, 1f)]
        [SerializeField] float _headWeight = 0.85f;

        [Tooltip("손 움직임 비중. 머리 비중과 합이 클수록 둘 다 반영됩니다.")]
        [Range(0f, 1f)]
        [SerializeField] float _handWeight = 0.15f;

        [Header("모니터 플레이 — WASD/마우스 (VR 안 켰을 때)")]
        [Tooltip("키보드로 얼마나 움직여야 ‘시간이 풀리기 시작’하는지(작을수록 살짝만 움직여도 반응).")]
        [Range(0f, 2f)]
        [SerializeField] float _flatPlanarDeadZone = 0.12f;

        [Tooltip("발 이동 속도 기준값(m/s 근처). 이보다 빠르면 시간이 거의 풀 속도에 가깝게 갑니다.")]
        [Range(0.5f, 15f)]
        [SerializeField] float _flatPlanarReference = 4.5f;

        [Tooltip("‘마우스 시점도 시간에 넣기’를 켰을 때만 씁니다. 시점 변화가 이 값보다 작으면 무시.")]
        [Range(0f, 90f)]
        [SerializeField] float _flatLookDeadZone = 10f;

        [Tooltip("마우스 시점 포함 시, 이 정도로 빨리 돌리면 시간이 풀 속도에 가깝습니다.")]
        [Range(10f, 600f)]
        [SerializeField] float _flatLookReference = 220f;

        [Tooltip("마우스 시점 포함 시, 이동 vs 시점 중 이동 쪽 가중치.")]
        [Range(0f, 1f)]
        [SerializeField] float _flatPlanarWeight = 0.55f;

        [Tooltip("마우스 시점 포함 시, 시점 쪽 가중치.")]
        [Range(0f, 1f)]
        [SerializeField] float _flatLookWeight = 0.45f;

        [Tooltip("끄면(권장) WASD 발 이동만 시간에 반영합니다. 켜면 마우스로 화면만 돌려도 시간이 조금 진행됩니다.")]
        [SerializeField] bool _flatTimeIncludesMouseLook = false;

        [Header("시간 느리게/빠르게 (기획에서 자주 조정)")]
        [Tooltip("가만히 있을 때 시간 배율. 0에 가깝게 = 거의 멈춤, 0.1 = 아주 살짝 흐름.")]
        [Range(0f, 1f)]
        [SerializeField] float _minTimeFactor = 0f;

        [Tooltip("많이 움직일 때 최대 시간 배율. 보통 1(정상 속도)로 둡니다.")]
        [Range(0.1f, 1f)]
        [SerializeField] float _maxTimeFactor = 1f;

        [Tooltip("느려짐/빨라짐이 얼마나 부드럽게 바뀌는지(초). 작을수록 반응이 즉각적입니다.")]
        [Range(0.02f, 1f)]
        [SerializeField] float _smoothTimeSeconds = 0.08f;

        IGameplayClock _clock;
        Vector3 _prevHmd;
        Vector3 _prevLeft;
        Vector3 _prevRight;
        bool _hasPrev;
        float _smoothedTimeFactor;
        float _smoothVelocity;
        SuperhotFlatFpsController _flatFps;
        OsFpsInspiredPlayerMotor _osFpsMotor;

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
            _flatFps = null;
            _osFpsMotor = null;
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
                DriveFlatPlaytest(unscaledDt);
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
                _clock.BeginFrame(unscaledDt, _maxTimeFactor);
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

        void DriveFlatPlaytest(float unscaledDt)
        {
            if (_flatFps == null || !_flatFps.isActiveAndEnabled)
                _flatFps = FindFirstObjectByType<SuperhotFlatFpsController>();

            float motion01;

            if (_flatFps != null)
            {
                if (_flatTimeIncludesMouseLook)
                {
                    motion01 = SuperhotTimeScaleCalculator.FlatBlendedMotion01(
                        _flatFps.LastPlanarSpeedMetersPerSecond,
                        _flatFps.LastLookIntensityPerSecond,
                        _flatPlanarDeadZone,
                        _flatPlanarReference,
                        _flatLookDeadZone,
                        _flatLookReference,
                        _flatPlanarWeight,
                        _flatLookWeight);
                }
                else
                {
                    var effective = SuperhotTimeScaleCalculator.EffectivePlanarSpeedForTime(
                        _flatFps.LastPlanarSpeedMetersPerSecond,
                        _flatFps.LastPlanarMoveIntent01,
                        _flatFps.MoveSpeed);
                    var reference = Mathf.Max(_flatPlanarReference, _flatFps.MoveSpeed);
                    motion01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                        effective,
                        _flatPlanarDeadZone,
                        reference);
                }
            }
            else
            {
                if (_osFpsMotor == null || !_osFpsMotor.isActiveAndEnabled)
                    _osFpsMotor = FindFirstObjectByType<OsFpsInspiredPlayerMotor>();

                if (_osFpsMotor == null)
                {
                    _clock.BeginFrame(unscaledDt, _maxTimeFactor);
                    return;
                }

                var effective = SuperhotTimeScaleCalculator.EffectivePlanarSpeedForTime(
                    _osFpsMotor.LastPlanarSpeedMetersPerSecond,
                    _osFpsMotor.LastPlanarMoveIntent01,
                    _osFpsMotor.MoveSpeed);
                var reference = Mathf.Max(_flatPlanarReference, _osFpsMotor.MoveSpeed);
                motion01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                    effective,
                    _flatPlanarDeadZone,
                    reference);
            }

            var targetFactor = SuperhotTimeScaleCalculator.ToTimeFactor(motion01, _minTimeFactor, _maxTimeFactor);
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
            _flatPlanarReference = Mathf.Max(0.01f, _flatPlanarReference);
            _flatLookReference = Mathf.Max(0.01f, _flatLookReference);
            _flatPlanarDeadZone = Mathf.Max(0f, _flatPlanarDeadZone);
            _flatLookDeadZone = Mathf.Max(0f, _flatLookDeadZone);
            _flatPlanarWeight = Mathf.Max(0f, _flatPlanarWeight);
            _flatLookWeight = Mathf.Max(0f, _flatLookWeight);
            _minTimeFactor = Mathf.Clamp(_minTimeFactor, 0f, 1f);
            _maxTimeFactor = Mathf.Clamp(_maxTimeFactor, _minTimeFactor, 1f);
        }
#endif
    }
}
