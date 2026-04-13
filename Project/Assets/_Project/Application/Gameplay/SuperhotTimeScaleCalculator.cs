using System;

namespace VRProject.Application.Gameplay
{
    /// <summary>
    /// 세 트래커(HMD, 좌·우 컨트롤) 합성 방식.
    /// </summary>
    public enum SuperhotCombineThreeTrackersMode
    {
        /// <summary>머리/손 채널을 각각 0..1로 만든 뒤 가중 평균.</summary>
        WeightedBlend01,

        /// <summary>트래커별 intensity를 RSS로 합친 뒤 하나의 속도 구간으로 0..1 매핑.</summary>
        RootSumSquareMagnitude,
    }

    /// <summary>양손 intensity를 하나의 "손" 채널로 묶는 방식.</summary>
    public enum SuperhotHandPairAggregation
    {
        Max,
        Average,
    }

    /// <summary>
    /// Pure SUPERHOT-style blending: weighted HMD vs hands, then map to time factor. No Unity dependencies.
    /// </summary>
    public static class SuperhotTimeScaleCalculator
    {
        /// <summary>
        /// 프레임 사이 각변화(도)와 dt로 각속도(도/초)를 구합니다. 드라이버에서 <c>Quaternion.Angle(prev, current)</c>를 넘깁니다.
        /// </summary>
        public static float AngularSpeedDegreesPerSecond(float deltaAngleDegrees, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f || deltaAngleDegrees < 0f)
                return 0f;

            return deltaAngleDegrees / deltaTimeSeconds;
        }

        /// <summary>
        /// 선속도(m/s)와 각속도(도/s)를 하나의 "움직임 강도" 스칼라로 합칩니다. angularWeight는 도/s를 m/s에 대응시키는 튜닝 계수입니다.
        /// </summary>
        public static float TrackerIntensity(
            float linearSpeedMetersPerSecond,
            float angularDegreesPerSecond,
            float angularWeightDegreesToMeters)
        {
            return linearSpeedMetersPerSecond + angularDegreesPerSecond * angularWeightDegreesToMeters;
        }

        /// <summary>세 트래커 intensity의 제곱합 제곱근 (유클리드 결합).</summary>
        public static float RootSumSquareThree(float a, float b, float c)
        {
            return (float)Math.Sqrt(a * a + b * b + c * c);
        }

        /// <summary>
        /// 목표 시간 배율까지 초당 변화량을 넘지 않도록 이동합니다 (Unity <c>Mathf.MoveTowards</c>와 동일 개념, 순수 수학).
        /// </summary>
        public static float SmoothTimeScaleMoveTowards(
            float current,
            float target,
            float maxDeltaPerSecond,
            float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f)
                return current;

            var maxDelta = maxDeltaPerSecond * unscaledDeltaTime;
            if (current < target)
                return Math.Min(current + maxDelta, target);
            return (float)Math.Max(current - maxDelta, target);
        }

        /// <summary>선형 보간으로 현재 값을 목표에 가깝게 (<c>t</c>는 보통 <c>unscaledDeltaTime * rate</c>).</summary>
        public static float SmoothTimeScaleLerp(float current, float target, float t)
        {
            if (t <= 0f)
                return current;
            if (t >= 1f)
                return target;
            return current + (target - current) * t;
        }

        public static float BlendWeightedMotion(
            float headMotion01,
            float handMotion01,
            float headWeight,
            float handWeight)
        {
            var denom = headWeight + handWeight;
            if (denom <= 1e-6f)
                return 0f;

            var blended = (headMotion01 * headWeight + handMotion01 * handWeight) / denom;
            return Clamp01(blended);
        }

        public static float Motion01FromSpeed(
            float speedMetersPerSecond,
            float deadZoneMetersPerSecond,
            float referenceSpeedMetersPerSecond)
        {
            if (speedMetersPerSecond <= deadZoneMetersPerSecond || referenceSpeedMetersPerSecond <= deadZoneMetersPerSecond)
                return 0f;

            var span = referenceSpeedMetersPerSecond - deadZoneMetersPerSecond;
            var t = (speedMetersPerSecond - deadZoneMetersPerSecond) / span;
            return Clamp01(t);
        }

        public static float ToTimeFactor(float blendedMotion01, float minTimeFactor, float maxTimeFactor)
        {
            var t = Clamp01(blendedMotion01);
            return minTimeFactor + (maxTimeFactor - minTimeFactor) * t;
        }

        /// <summary>
        /// PC/패드: 정규화된 이동(0~1)·시선(0~1)에 가중치를 곱해 더한 뒤 [minFactor, maxFactor]로 매핑합니다.
        /// 가중 합이 1을 넘을 수 있어 합성 계수는 [0,1]로 클램프합니다.
        /// </summary>
        public static float DesktopAdditiveTargetTimeScale(
            float minFactor,
            float maxFactor,
            float move01,
            float look01,
            float moveWeight,
            float lookWeight)
        {
            move01 = Clamp01(move01);
            look01 = Clamp01(look01);
            if (maxFactor < minFactor)
            {
                var tmp = maxFactor;
                maxFactor = minFactor;
                minFactor = tmp;
            }

            var span = maxFactor - minFactor;
            if (span <= 1e-6f)
                return minFactor;

            var combined = move01 * moveWeight + look01 * lookWeight;
            if (combined < 0f)
                combined = 0f;
            if (combined > 1f)
                combined = 1f;

            return minFactor + span * combined;
        }

        /// <summary>
        /// 이동만·시선만·가산 합 중 목표 배율이 가장 큰 값을 씁니다(WASD가 마우스 약한 입력에 덮이지 않게).
        /// </summary>
        public static float DesktopMaxBlendedTargetTimeScale(
            float minFactor,
            float maxFactor,
            float move01,
            float look01,
            float moveWeight,
            float lookWeight)
        {
            var moveOnly = DesktopAdditiveTargetTimeScale(minFactor, maxFactor, move01, 0f, moveWeight, lookWeight);
            var lookOnly = DesktopAdditiveTargetTimeScale(minFactor, maxFactor, 0f, look01, moveWeight, lookWeight);
            var additive = DesktopAdditiveTargetTimeScale(minFactor, maxFactor, move01, look01, moveWeight, lookWeight);
            var m = Math.Max(moveOnly, lookOnly);
            return Math.Max(m, additive);
        }

        /// <summary>
        /// 시뮬레이션 dt가 0일 때도 키 입력으로 시간 진행을 풀 수 있도록, 속도와 의도(×이동속도) 중 큰 값을 씁니다.
        /// </summary>
        public static float EffectivePlanarSpeedForTime(
            float planarVelocityMetersPerSecond,
            float moveIntent01,
            float moveSpeedMetersPerSecond)
        {
            var fromIntent = moveIntent01 * moveSpeedMetersPerSecond;
            return planarVelocityMetersPerSecond >= fromIntent
                ? planarVelocityMetersPerSecond
                : fromIntent;
        }

        /// <summary>
        /// Desktop playtest: blend keyboard locomotion vs mouse-look intensity into one motion 0..1.
        /// </summary>
        public static float FlatBlendedMotion01(
            float planarSpeedMetersPerSecond,
            float lookIntensityPerSecond,
            float planarDeadZoneMps,
            float planarReferenceMps,
            float lookDeadZonePerSec,
            float lookReferencePerSec,
            float planarWeight,
            float lookWeight)
        {
            var planar01 = Motion01FromSpeed(planarSpeedMetersPerSecond, planarDeadZoneMps, planarReferenceMps);
            var look01 = Motion01FromSpeed(lookIntensityPerSecond, lookDeadZonePerSec, lookReferencePerSec);
            return BlendWeightedMotion(planar01, look01, planarWeight, lookWeight);
        }

        public static float SmoothTowards(
            float current,
            float target,
            ref float velocity,
            float smoothTimeSeconds,
            float unscaledDeltaTime)
        {
            if (smoothTimeSeconds <= 1e-6f || unscaledDeltaTime <= 0f)
                return target;

            var omega = 2f / smoothTimeSeconds;
            var x = omega * unscaledDeltaTime;
            var exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            var change = current - target;
            var temp = (velocity + omega * change) * unscaledDeltaTime;
            velocity = (velocity - omega * temp) * exp;
            return target + (change + temp) * exp;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;
            return v > 1f ? 1f : v;
        }
    }
}
