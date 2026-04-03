using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    /// <summary>
    /// Pure rules for whether LMB may be processed when ADS is required on the motor.
    /// </summary>
    public static class OsFpsInspiredAimFireGate
    {
        public static bool PassesFireAimGate(
            bool requireRightMouseAimToFire,
            bool allowHipFireWhileLocomoting,
            float locomotionAxesSqrThreshold,
            bool motorIsNull,
            bool motorReportsAiming,
            Vector2 locomotionAxes)
        {
            if (!requireRightMouseAimToFire || motorIsNull)
                return true;
            if (motorReportsAiming)
                return true;
            if (allowHipFireWhileLocomoting && locomotionAxes.sqrMagnitude > locomotionAxesSqrThreshold)
                return true;
            return false;
        }
    }
}
