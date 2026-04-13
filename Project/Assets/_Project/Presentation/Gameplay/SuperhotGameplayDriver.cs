using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Gameplay;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>시간 배율 스무딩: MoveTowards(권장, VR 멀미 완화) 또는 기존 SmoothDamp 유사 곡선.</summary>
    public enum SuperhotTimeSmoothingMode
    {
        MoveTowards,
        SmoothDampLegacy,
    }

    /// <summary>
    /// Samples motion (XR: HMD + hands 위치·회전; flat: WASD 평면 이동 — 옵션으로 마우스 시점 포함) 후
    /// Unity 시간 배율·고정 스텝·<see cref="IGameplayClock"/>을 갱신합니다.
    /// </summary>
    [DefaultExecutionOrder(-80)]
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

        [Header("VR — 회전 반영")]
        [Tooltip("각속도(°/s)에 곱해 선속도와 같은 단위 스케일로 섞습니다. 크면 손/머리 돌리기만으로도 시간이 잘 풀립니다.")]
        [Range(0f, 0.5f)]
        [SerializeField] float _angularWeightDegreesToMeters = 0.012f;

        [Header("VR — 머리 vs 손 비중 · 합성")]
        [Tooltip("WeightedBlend: 시간 진행에 머리 움직임이 차지하는 비중.")]
        [Range(0f, 1f)]
        [SerializeField] float _headWeight = 0.85f;

        [Tooltip("WeightedBlend: 손 움직임 비중.")]
        [Range(0f, 1f)]
        [SerializeField] float _handWeight = 0.15f;

        [Tooltip("세 트래커를 하나의 motion 0..1로 만드는 방식.")]
        [SerializeField] SuperhotCombineThreeTrackersMode _combineMode = SuperhotCombineThreeTrackersMode.WeightedBlend01;

        [Tooltip("양손 intensity를 손 채널 하나로 묶을 때 최대값 vs 평균.")]
        [SerializeField] SuperhotHandPairAggregation _handPairAggregation = SuperhotHandPairAggregation.Max;

        [Tooltip("RSS 모드: 합성 intensity가 이 값 이하면 정지에 가깝게 취급(m/s 근사).")]
        [Range(0f, 5f)]
        [SerializeField] float _rssDeadZoneSpeed = 0.15f;

        [Tooltip("RSS 모드: 합성 intensity가 이 값에 도달하면 motion 1에 가깝게.")]
        [Range(0.1f, 15f)]
        [SerializeField] float _rssReferenceSpeed = 3.5f;

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

        [Tooltip("플랫 플레이에서도 Time.timeScale / fixedDeltaTime을 VR과 동일하게 적용합니다.")]
        [SerializeField] bool _applyUnityTimeScale = true;

        [Header("시간 느리게/빠르게 (기획에서 자주 조정)")]
        [Tooltip("가만히 있을 때 시간 배율. 0에 가깝게 = 거의 멈춤. 물리 깨짐 방지를 위해 너무 낮게 두지 마세요.")]
        [Range(0.001f, 1f)]
        [SerializeField] float _minTimeFactor = 0.05f;

        [Tooltip("많이 움직일 때 최대 시간 배율. 보통 1(정상 속도)로 둡니다.")]
        [Range(0.1f, 1f)]
        [SerializeField] float _maxTimeFactor = 1f;

        [Header("시간 배율 스무딩")]
        [Tooltip("MoveTowards: 초당 시간 배율이 이 값 이상 바뀌지 않습니다(작을수록 더 부드럽고 둔함).")]
        [Range(0.1f, 30f)]
        [SerializeField] float _maxTimeScaleChangePerSecond = 10f;

        [Tooltip("SmoothDampLegacy: 느려짐/빨라짐이 얼마나 부드럽게 바뀌는지(초).")]
        [Range(0.02f, 1f)]
        [SerializeField] float _smoothTimeSeconds = 0.08f;

        [SerializeField] SuperhotTimeSmoothingMode _timeSmoothingMode = SuperhotTimeSmoothingMode.MoveTowards;

        [Header("Unity 물리 스텝 동기화")]
        [Tooltip("Time.timeScale=1일 때의 기준 Fixed Timestep(보통 0.02). fixedDeltaTime = 이 값 × timeScale.")]
        [Range(0.001f, 0.1f)]
        [SerializeField] float _baseFixedDeltaTime = 0.02f;

        IGameplayClock _clock;
        Vector3 _prevHmd;
        Vector3 _prevLeft;
        Vector3 _prevRight;
        Quaternion _prevHmdRot;
        Quaternion _prevLeftRot;
        Quaternion _prevRightRot;
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
            _smoothVelocity = 0f;
            _smoothedTimeFactor = _maxTimeFactor;
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
                ApplyMaxTimeAndClock(unscaledDt);
                return;
            }

            var hmdPos = _hmd.position;
            var leftPos = _leftController != null ? _leftController.position : hmdPos;
            var rightPos = _rightController != null ? _rightController.position : hmdPos;
            var leftRot = _leftController != null ? _leftController.rotation : _hmd.rotation;
            var rightRot = _rightController != null ? _rightController.rotation : _hmd.rotation;

            if (!_hasPrev)
            {
                _prevHmd = hmdPos;
                _prevLeft = leftPos;
                _prevRight = rightPos;
                _prevHmdRot = _hmd.rotation;
                _prevLeftRot = leftRot;
                _prevRightRot = rightRot;
                _hasPrev = true;
                ApplyMaxTimeAndClock(unscaledDt);
                return;
            }

            var headLin = (hmdPos - _prevHmd).magnitude / unscaledDt;
            var leftLin = (leftPos - _prevLeft).magnitude / unscaledDt;
            var rightLin = (rightPos - _prevRight).magnitude / unscaledDt;

            var headAngDelta = Quaternion.Angle(_prevHmdRot, _hmd.rotation);
            var leftAngDelta = Quaternion.Angle(_prevLeftRot, leftRot);
            var rightAngDelta = Quaternion.Angle(_prevRightRot, rightRot);

            var headAngSpeed = SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(headAngDelta, unscaledDt);
            var leftAngSpeed = SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(leftAngDelta, unscaledDt);
            var rightAngSpeed = SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(rightAngDelta, unscaledDt);

            _prevHmd = hmdPos;
            _prevLeft = leftPos;
            _prevRight = rightPos;
            _prevHmdRot = _hmd.rotation;
            _prevLeftRot = leftRot;
            _prevRightRot = rightRot;

            var w = _angularWeightDegreesToMeters;
            var headI = SuperhotTimeScaleCalculator.TrackerIntensity(headLin, headAngSpeed, w);
            var leftI = SuperhotTimeScaleCalculator.TrackerIntensity(leftLin, leftAngSpeed, w);
            var rightI = SuperhotTimeScaleCalculator.TrackerIntensity(rightLin, rightAngSpeed, w);

            float blendedMotion01;
            if (_combineMode == SuperhotCombineThreeTrackersMode.RootSumSquareMagnitude)
            {
                var combined = SuperhotTimeScaleCalculator.RootSumSquareThree(headI, leftI, rightI);
                blendedMotion01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                    combined,
                    _rssDeadZoneSpeed,
                    _rssReferenceSpeed);
            }
            else
            {
                var handI = _handPairAggregation == SuperhotHandPairAggregation.Max
                    ? Mathf.Max(leftI, rightI)
                    : (leftI + rightI) * 0.5f;

                var head01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                    headI,
                    _headDeadZoneSpeed,
                    _headReferenceSpeed);

                var hand01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                    handI,
                    _handDeadZoneSpeed,
                    _handReferenceSpeed);

                blendedMotion01 = SuperhotTimeScaleCalculator.BlendWeightedMotion(
                    head01,
                    hand01,
                    _headWeight,
                    _handWeight);
            }

            var targetFactor = SuperhotTimeScaleCalculator.ToTimeFactor(blendedMotion01, _minTimeFactor, _maxTimeFactor);
            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
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
                    ApplyMaxTimeAndClock(unscaledDt);
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
            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
        }

        /// <summary>목표 배율까지 스무딩한 뒤 Unity 시간과 게임 시계에 반영합니다.</summary>
        void FinishFrameWithSmoothedTarget(float unscaledDt, float targetFactor)
        {
            if (_timeSmoothingMode == SuperhotTimeSmoothingMode.MoveTowards)
            {
                _smoothedTimeFactor = SuperhotTimeScaleCalculator.SmoothTimeScaleMoveTowards(
                    _smoothedTimeFactor,
                    targetFactor,
                    _maxTimeScaleChangePerSecond,
                    unscaledDt);
            }
            else
            {
                _smoothedTimeFactor = SuperhotTimeScaleCalculator.SmoothTowards(
                    _smoothedTimeFactor,
                    targetFactor,
                    ref _smoothVelocity,
                    _smoothTimeSeconds,
                    unscaledDt);
            }

            ApplySmoothedTimeToUnityAndClock(unscaledDt);
        }

        void ApplyMaxTimeAndClock(float unscaledDt)
        {
            _smoothedTimeFactor = _maxTimeFactor;
            _smoothVelocity = 0f;
            ApplySmoothedTimeToUnityAndClock(unscaledDt);
        }

        void ApplySmoothedTimeToUnityAndClock(float unscaledDt)
        {
            var factor = _smoothedTimeFactor;
            if (_applyUnityTimeScale)
            {
                Time.timeScale = factor;
                Time.fixedDeltaTime = _baseFixedDeltaTime * Time.timeScale;
            }

            var clockFactor = _applyUnityTimeScale ? Time.timeScale : factor;
            _clock.BeginFrame(unscaledDt, clockFactor);
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
            _minTimeFactor = Mathf.Max(1e-3f, _minTimeFactor);
            _minTimeFactor = Mathf.Clamp(_minTimeFactor, 1e-3f, 1f);
            _maxTimeFactor = Mathf.Clamp(_maxTimeFactor, _minTimeFactor, 1f);
            _rssReferenceSpeed = Mathf.Max(0.01f, _rssReferenceSpeed);
            _rssDeadZoneSpeed = Mathf.Max(0f, _rssDeadZoneSpeed);
            _maxTimeScaleChangePerSecond = Mathf.Max(0.01f, _maxTimeScaleChangePerSecond);
            _baseFixedDeltaTime = Mathf.Max(1e-5f, _baseFixedDeltaTime);
        }
#endif
    }
}
