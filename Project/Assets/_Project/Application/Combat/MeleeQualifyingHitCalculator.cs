using VRProject.Application.Gameplay;

namespace VRProject.Application.Combat
{
    public static class MeleeQualifyingHitCalculator
    {
        public static float KindWeight(WeaponAttackKind kind)
        {
            switch (kind)
            {
                case WeaponAttackKind.Stab:
                    return 1.15f;
                case WeaponAttackKind.Slash:
                    return 1f;
                case WeaponAttackKind.Blunt:
                    return 0.95f;
                default:
                    return 0f;
            }
        }

        public static float QualifyingScore(
            float linearSpeedMetersPerSecond,
            float minLinearSpeed,
            float referenceLinearSpeed,
            WeaponAttackKind kind,
            float zoneFeedbackMultiplier)
        {
            var motion01 = SuperhotTimeScaleCalculator.Motion01FromSpeed(
                linearSpeedMetersPerSecond,
                minLinearSpeed,
                referenceLinearSpeed);

            var kindWeight = KindWeight(kind);
            if (kindWeight <= 0f)
                return 0f;

            return motion01 * kindWeight * zoneFeedbackMultiplier;
        }
    }
}
