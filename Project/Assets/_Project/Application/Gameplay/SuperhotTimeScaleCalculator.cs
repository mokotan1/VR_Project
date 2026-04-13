namespace VRProject.Application.Gameplay
{
    /// <summary>
    /// Pure SUPERHOT-style blending: weighted HMD vs hands, then map to time factor. No Unity dependencies.
    /// </summary>
    public static class SuperhotTimeScaleCalculator
    {
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
