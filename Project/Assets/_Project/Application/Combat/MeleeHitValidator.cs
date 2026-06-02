namespace VRProject.Application.Combat
{
    public static class MeleeHitValidator
    {
        public static bool MeetsHitMomentSpeed(
            float linearSpeedMetersPerSecond,
            float angularSpeedDegreesPerSecond,
            float minHitLinearSpeed,
            float minHitAngularSpeed)
        {
            return linearSpeedMetersPerSecond >= minHitLinearSpeed ||
                   angularSpeedDegreesPerSecond >= minHitAngularSpeed;
        }

        public static bool IsQualifyingHit(
            bool sessionActive,
            float qualifyingScore,
            float minQualifyingScore,
            float linearSpeedMetersPerSecond,
            float angularSpeedDegreesPerSecond,
            float minHitLinearSpeed,
            float minHitAngularSpeed,
            WeaponAttackKind kind,
            WeaponFamily family)
        {
            if (!sessionActive)
                return false;

            if (qualifyingScore < minQualifyingScore)
                return false;

            if (!MeetsHitMomentSpeed(
                    linearSpeedMetersPerSecond,
                    angularSpeedDegreesPerSecond,
                    minHitLinearSpeed,
                    minHitAngularSpeed))
                return false;

            return WeaponAttackKindClassifier.IsKindAllowed(kind, family);
        }
    }
}
