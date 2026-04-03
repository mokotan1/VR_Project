using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>Deterministic spread for <see cref="BattleRoyaleStyleCrosshair"/> (testable).</summary>
    public static class BattleRoyaleCrosshairSpread
    {
        public const float FireKickDuration = 0.22f;
        public const float FireKickPixels = 22f;
        public const float MoveSpreadScale = 20f;
        public const float AirSpreadPixels = 14f;
        public const float AimSpreadMultiplier = 0.12f;

        public static float FireKickContribution(float unscaledTimeSinceFire)
        {
            if (unscaledTimeSinceFire < 0f || unscaledTimeSinceFire >= FireKickDuration)
                return 0f;
            return FireKickPixels * (1f - unscaledTimeSinceFire / FireKickDuration);
        }

        /// <summary>Extra pixel offset (before aim multiplier and smoothing).</summary>
        public static float ComputeRawSpread(
            float locomotionAxesMagnitude01,
            bool isGrounded,
            bool isAiming,
            float unscaledTimeSinceLastFire)
        {
            var moveSpread = Mathf.Clamp01(locomotionAxesMagnitude01) * MoveSpreadScale;
            var airSpread = isGrounded ? 0f : AirSpreadPixels;
            var fireSpread = FireKickContribution(unscaledTimeSinceLastFire);
            var raw = moveSpread + airSpread + fireSpread;
            return isAiming ? raw * AimSpreadMultiplier : raw;
        }
    }
}
