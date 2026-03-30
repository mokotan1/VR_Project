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
