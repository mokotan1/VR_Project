using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class WeaponFireScreenImpulseProfile
    {
        public static float PulseWeight(float elapsedSeconds, float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return 0f;

            var t = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            var inv = 1f - t;
            return inv * inv;
        }

        public static float EffectiveKickStrength(float baseStrength, float xrComfortMultiplier, bool xrActive)
        {
            var multiplier = xrActive ? Mathf.Clamp01(xrComfortMultiplier) : 1f;
            return Mathf.Max(0f, baseStrength) * multiplier;
        }
    }
}
