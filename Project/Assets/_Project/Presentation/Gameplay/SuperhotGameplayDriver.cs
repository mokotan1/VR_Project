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
    /// <see cref="_applyUnityTimeScale"/>를 켜 두면 Animator·ParticleSystem·NavMesh 등
    /// <c>Time.deltaTime</c> 기반 내장 동작이 시간 배율과 한꺼번에 맞춰집니다.
    /// 애니만 별도 속도가 필요하면 해당 Animator의 <c>speed</c>를 추가로 튜닝합니다.
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

        [Header("VR — 미세 떨림 억제 (정지인데 시간이 안 느려질 때)")]
        [Tooltip("이 각속도(°/s) 이하는 추적 노이즈로 보고 회전 기여를 0으로 둡니다.")]
        [Range(0f, 180f)]
        [SerializeField] float _angularDeadZoneDegreesPerSecond = 12f;

        [Tooltip("선속도(m/s)에서 이 값만큼 빼 노이즈를 줄입니다. 0이면 미적용.")]
        [Range(0f, 0.5f)]
        [SerializeField] float _linearNoiseFloorMps = 0.02f;

        [Tooltip("합성 motion 0..1이 이 값 미만이면 완전 정지(0)로 간주합니다. 0이면 끔.")]
        [Range(0f, 0.2f)]
        [SerializeField] float _idleBlend01Clamp = 0.035f;

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

        [Tooltip("플랫 모드에서 이동 소스(SuperhotFlatFps / OsFpsMotor)를 찾지 못할 때 시간을 1로 둡니다. 끄면 _minTimeFactor(거의 정지)로 둡니다.")]
        [SerializeField] bool _flatUseFullTimeWhenNoMotor;

        [Tooltip("PC/패드: 입력·상태 기반 가산 목표 배율(스펙). 끄면 기존 속도 기반 플랫 모델을 씁니다.")]
        [SerializeField] bool _flatUseDesktopInputModel;

        [Tooltip("데스크톱 가산식: 이동 의도(0~1)에 곱하는 가중치.")]
        [SerializeField] float _flatDesktopMoveWeight = 1f;

        [Tooltip("데스크톱 가산식: 마우스 시선(0~1)에 곱하는 가중치.")]
        [SerializeField] float _flatDesktopLookWeight = 0.25f;

        [Tooltip("데스크톱: 공중일 때 목표 배율이 이보다 내려가지 않게 합니다.")]
        [Range(0.01f, 1f)]
        [SerializeField] float _flatAirborneMinTimeFactor = 0.3f;

        [Tooltip("비어 있으면 씬에서 찾습니다.")]
        [SerializeField] SuperhotFlatHitscanWeapon _flatHitscanWeapon;

        [Header("완전 정지 (움직임 없을 때 화면·시간 동결)")]
        [Tooltip("켜면 motion이 0일 때 Time.timeScale 목표를 _minTimeFactor 대신 0으로 둡니다. 애니·피직스·Time.deltaTime 기반 업데이트가 멈춥니다(드라이버는 unscaled로만 감지).")]
        [SerializeField] bool _completeFreezeWhenIdle = true;

        [Header("시간 느리게/빠르게 (기획에서 자주 조정)")]
        [Tooltip("가만히 있을 때 시간 배율(Complete Freeze 끌 때만 사용). 완전 동결이면 motion=0일 때 0이 목표가 됩니다.")]
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

        [Header("진단 · 스무딩 보정 (선택)")]
        [Tooltip("켜면 콘솔에 timeScale / 시계 / Unity 이론 delta 비교를 출력합니다(에디터·Development 빌드).")]
        [SerializeField] bool _debugLogTimeScale;

        [Tooltip("디버그 로그 최소 간격(실시간 초). 0이면 매 프레임.")]
        [SerializeField] float _debugLogIntervalSeconds = 0.5f;

        [Tooltip("목표 배율이 최소(_minTimeFactor)에 붙으면 스무딩을 건너뛰고 즉시 최소로 맞춥니다(정지 직후 슬로모 잔상 완화).")]
        [SerializeField] bool _snapSmoothedWhenTargetAtMin = true;

        [Tooltip("목표가 최소로 간주되는 여유(이하면 스냅).")]
        [SerializeField] float _snapToMinEpsilon = 0.002f;

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
        float _debugLogAccumulatorUnscaled;
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
            _debugLogAccumulatorUnscaled = 0f;
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

            var headAngSpeed = ApplyAngularDeadZone(
                SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(headAngDelta, unscaledDt));
            var leftAngSpeed = ApplyAngularDeadZone(
                SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(leftAngDelta, unscaledDt));
            var rightAngSpeed = ApplyAngularDeadZone(
                SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(rightAngDelta, unscaledDt));

            headLin = ApplyLinearNoiseFloor(headLin);
            leftLin = ApplyLinearNoiseFloor(leftLin);
            rightLin = ApplyLinearNoiseFloor(rightLin);

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

            blendedMotion01 = ApplyIdleBlend01Clamp(blendedMotion01);

            var targetFactor = SuperhotTimeScaleCalculator.ToTimeFactor(
                blendedMotion01,
                EffectiveMinTimeFactor(blendedMotion01),
                _maxTimeFactor);
            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
        }

        void DriveFlatPlaytest(float unscaledDt)
        {
            if (_flatFps == null || !_flatFps.isActiveAndEnabled)
                _flatFps = FindFirstObjectByType<SuperhotFlatFpsController>(FindObjectsInactive.Include);

            if (_flatHitscanWeapon == null || !_flatHitscanWeapon.isActiveAndEnabled)
                _flatHitscanWeapon = FindFirstObjectByType<SuperhotFlatHitscanWeapon>(FindObjectsInactive.Include);

            if (_flatUseDesktopInputModel)
            {
                if (_flatFps != null)
                {
                    DriveFlatDesktopAdditiveFromFlatFps(unscaledDt);
                    return;
                }

                if (_osFpsMotor == null || !_osFpsMotor.isActiveAndEnabled)
                    _osFpsMotor = FindFirstObjectByType<OsFpsInspiredPlayerMotor>(FindObjectsInactive.Include);

                if (_osFpsMotor == null)
                {
                    if (_flatUseFullTimeWhenNoMotor)
                        ApplyMaxTimeAndClock(unscaledDt);
                    else
                        FinishFrameWithSmoothedTarget(unscaledDt, _minTimeFactor);
                    return;
                }

                DriveFlatDesktopAdditiveFromOsMotor(unscaledDt);
                return;
            }

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
                    _osFpsMotor = FindFirstObjectByType<OsFpsInspiredPlayerMotor>(FindObjectsInactive.Include);

                if (_osFpsMotor == null)
                {
                    if (_flatUseFullTimeWhenNoMotor)
                        ApplyMaxTimeAndClock(unscaledDt);
                    else
                        FinishFrameWithSmoothedTarget(unscaledDt, EffectiveMinTimeFactor(0f));
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

            motion01 = ApplyIdleBlend01Clamp(motion01);

            var targetFactor = SuperhotTimeScaleCalculator.ToTimeFactor(
                motion01,
                EffectiveMinTimeFactor(motion01),
                _maxTimeFactor);
            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
        }

        void DriveFlatDesktopAdditiveFromFlatFps(float unscaledDt)
        {
            var move01 = _flatFps.LastPlanarMoveIntent01;
            var look01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                _flatFps.LastLookIntensityPerSecond,
                _flatLookDeadZone,
                _flatLookReference);

            var baseTarget = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(
                _minTimeFactor,
                _maxTimeFactor,
                move01,
                look01,
                _flatDesktopMoveWeight,
                _flatDesktopLookWeight);

            var targetFactor = baseTarget;
            if (_flatHitscanWeapon != null && _flatHitscanWeapon.IsInShootTimeScaleHold)
                targetFactor = _maxTimeFactor;
            else if (_flatFps.IsAirborne)
                targetFactor = Mathf.Max(baseTarget, _flatAirborneMinTimeFactor);

            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
        }

        void DriveFlatDesktopAdditiveFromOsMotor(float unscaledDt)
        {
            var move01 = _osFpsMotor.LastPlanarMoveIntent01;
            const float look01 = 0f;

            var baseTarget = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(
                _minTimeFactor,
                _maxTimeFactor,
                move01,
                look01,
                _flatDesktopMoveWeight,
                _flatDesktopLookWeight);

            var targetFactor = baseTarget;
            if (_flatHitscanWeapon != null && _flatHitscanWeapon.IsInShootTimeScaleHold)
                targetFactor = _maxTimeFactor;
            else if (_osFpsMotor.IsAirborne)
                targetFactor = Mathf.Max(baseTarget, _flatAirborneMinTimeFactor);

            FinishFrameWithSmoothedTarget(unscaledDt, targetFactor);
        }

        /// <summary>motion 0일 때 완전 동결(최소 0) vs 슬로만(_minTimeFactor) 선택.</summary>
        float EffectiveMinTimeFactor(float blendedMotion01)
        {
            if (_completeFreezeWhenIdle && blendedMotion01 <= 1e-5f)
                return 0f;
            return _minTimeFactor;
        }

        float ApplyAngularDeadZone(float degreesPerSecond)
        {
            if (_angularDeadZoneDegreesPerSecond <= 0f)
                return degreesPerSecond;
            return degreesPerSecond > _angularDeadZoneDegreesPerSecond ? degreesPerSecond : 0f;
        }

        float ApplyLinearNoiseFloor(float linearMetersPerSecond)
        {
            if (_linearNoiseFloorMps <= 0f)
                return linearMetersPerSecond;
            return Mathf.Max(0f, linearMetersPerSecond - _linearNoiseFloorMps);
        }

        float ApplyIdleBlend01Clamp(float blended01)
        {
            if (_idleBlend01Clamp <= 0f || blended01 >= _idleBlend01Clamp)
                return blended01;
            return 0f;
        }

        /// <summary>목표 배율까지 스무딩한 뒤 Unity 시간과 게임 시계에 반영합니다.</summary>
        void FinishFrameWithSmoothedTarget(float unscaledDt, float targetFactor)
        {
            var snapThreshold = _completeFreezeWhenIdle ? _snapToMinEpsilon : _minTimeFactor + _snapToMinEpsilon;
            if (_snapSmoothedWhenTargetAtMin && targetFactor <= snapThreshold)
            {
                _smoothedTimeFactor = targetFactor;
                _smoothVelocity = 0f;
            }
            else if (_timeSmoothingMode == SuperhotTimeSmoothingMode.MoveTowards)
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

            ApplySmoothedTimeToUnityAndClock(unscaledDt, targetFactor);
        }

        void ApplyMaxTimeAndClock(float unscaledDt)
        {
            _smoothedTimeFactor = _maxTimeFactor;
            _smoothVelocity = 0f;
            ApplySmoothedTimeToUnityAndClock(unscaledDt, _maxTimeFactor);
        }

        void ApplySmoothedTimeToUnityAndClock(float unscaledDt, float targetFactorForDebug)
        {
            var factor = _smoothedTimeFactor;
            if (_applyUnityTimeScale)
            {
                Time.timeScale = factor;
                // timeScale==0이면 물리 스텝도 0에 가깝게(완전 정지). 일부 플랫폼은 0 고정스텝에 민감해 최소값을 사용할 수 있음.
                var scaledFixed = _baseFixedDeltaTime * Time.timeScale;
                Time.fixedDeltaTime = scaledFixed > 1e-8f ? scaledFixed : 0f;
            }

            // 시계: unscaledDeltaTime × timeFactor — Unity 문서상 Time.deltaTime과 같은 프레임에서의 이론값과 일치시키기 위함
            var clockFactor = _applyUnityTimeScale ? Time.timeScale : factor;
            _clock.BeginFrame(unscaledDt, clockFactor);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogTimeScaleDiagnosticsIfNeeded(unscaledDt, targetFactorForDebug, factor, clockFactor);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void LogTimeScaleDiagnosticsIfNeeded(
            float unscaledDt,
            float motionTargetFactor,
            float smoothedFactor,
            float clockFactor)
        {
            if (!_debugLogTimeScale)
                return;

            var interval = Mathf.Max(0f, _debugLogIntervalSeconds);
            if (interval > 0f)
            {
                _debugLogAccumulatorUnscaled += unscaledDt;
                if (_debugLogAccumulatorUnscaled < interval)
                    return;
                _debugLogAccumulatorUnscaled = 0f;
            }

            var simDt = _clock.SimulationDeltaTime;
            var theoryFromUnscaled = unscaledDt * clockFactor;
            var unityDt = Time.deltaTime;
            var mismatchVsTheory = Mathf.Abs(simDt - theoryFromUnscaled);
            var mismatchVsUnityDt = Mathf.Abs(simDt - unityDt);

            Debug.Log(
                "[SuperhotTime] targetMotion=" + motionTargetFactor.ToString("F4") +
                " smoothed=" + smoothedFactor.ToString("F4") +
                " timeScale=" + Time.timeScale.ToString("F4") +
                " clockLastFactor=" + _clock.LastTimeFactor.ToString("F4") +
                " simDt=" + simDt.ToString("F5") +
                " theory(unscaled×factor)=" + theoryFromUnscaled.ToString("F5") +
                " |Δ(theory)|=" + mismatchVsTheory.ToString("F6") +
                " Time.deltaTime=" + unityDt.ToString("F5") +
                " |Δ(Unity.dt)|=" + mismatchVsUnityDt.ToString("F6") +
                " applyUnityTS=" + _applyUnityTimeScale,
                this);
        }
#endif

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
            _debugLogIntervalSeconds = Mathf.Max(0f, _debugLogIntervalSeconds);
            _snapToMinEpsilon = Mathf.Max(0f, _snapToMinEpsilon);
            _angularDeadZoneDegreesPerSecond = Mathf.Max(0f, _angularDeadZoneDegreesPerSecond);
            _linearNoiseFloorMps = Mathf.Max(0f, _linearNoiseFloorMps);
            _idleBlend01Clamp = Mathf.Max(0f, _idleBlend01Clamp);
            _flatDesktopMoveWeight = Mathf.Max(0f, _flatDesktopMoveWeight);
            _flatDesktopLookWeight = Mathf.Max(0f, _flatDesktopLookWeight);
            _flatAirborneMinTimeFactor = Mathf.Clamp(_flatAirborneMinTimeFactor, _minTimeFactor, _maxTimeFactor);
        }
#endif
    }
}
