namespace VRProject.Application.Combat
{
    public static class MeleeHitValidator
    {
        public static bool IsQualifyingHit(
            bool sessionActive,
            float qualifyingScore,
            float minQualifyingScore,
            WeaponAttackKind kind,
            WeaponFamily family)
        {
            if (!sessionActive)
                return false;

            if (qualifyingScore < minQualifyingScore)
                return false;

            return WeaponAttackKindClassifier.IsKindAllowed(kind, family);
        }
    }
}
