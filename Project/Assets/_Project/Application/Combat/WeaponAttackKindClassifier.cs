namespace VRProject.Application.Combat
{
    public static class WeaponAttackKindClassifier
    {
        public static WeaponAttackKind Classify(
            CombatVector3 tipVelocity,
            CombatVector3 weaponForward,
            CombatVector3 weaponRight,
            WeaponFamily family,
            float linearSpeed,
            float angularSpeed,
            float stabForwardDotMin,
            float slashSideDotMin,
            float bluntMaxAngularSpeed,
            float bluntMinLinearSpeed)
        {
            if (family == WeaponFamily.Blunt)
                return linearSpeed >= bluntMinLinearSpeed ? WeaponAttackKind.Blunt : WeaponAttackKind.None;

            var forwardDot = CombatVector3.Dot(tipVelocity.Normalized, weaponForward.Normalized);
            var sideDot = CombatVector3.Dot(tipVelocity.Normalized, weaponRight.Normalized);

            var stab = forwardDot >= stabForwardDotMin && angularSpeed <= bluntMaxAngularSpeed;
            var slash = sideDot >= slashSideDotMin && linearSpeed > 0f;
            var blunt = linearSpeed >= bluntMinLinearSpeed && angularSpeed <= bluntMaxAngularSpeed && forwardDot < stabForwardDotMin;

            if (family == WeaponFamily.Stab)
                return stab ? WeaponAttackKind.Stab : WeaponAttackKind.None;

            if (family == WeaponFamily.Slash)
                return slash ? WeaponAttackKind.Slash : WeaponAttackKind.None;

            if (stab)
                return WeaponAttackKind.Stab;
            if (slash)
                return WeaponAttackKind.Slash;
            if (blunt)
                return WeaponAttackKind.Blunt;

            return WeaponAttackKind.None;
        }

        public static bool IsKindAllowed(WeaponAttackKind kind, WeaponFamily family)
        {
            if (kind == WeaponAttackKind.None)
                return false;

            switch (family)
            {
                case WeaponFamily.Slash:
                    return kind == WeaponAttackKind.Slash;
                case WeaponFamily.Stab:
                    return kind == WeaponAttackKind.Stab;
                case WeaponFamily.Blunt:
                    return kind == WeaponAttackKind.Blunt;
                default:
                    return true;
            }
        }
    }
}
