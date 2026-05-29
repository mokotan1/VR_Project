using VRProject.Application.Gameplay;

namespace VRProject.Application.Combat
{
    public static class WeaponMotionSampleLogic
    {
        public static float LinearSpeedMetersPerSecond(CombatVector3 previous, CombatVector3 current, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
                return 0f;

            var delta = current - previous;
            return delta.Magnitude / deltaTimeSeconds;
        }

        public static float AngularSpeedDegreesPerSecond(float deltaAngleDegrees, float deltaTimeSeconds) =>
            SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(deltaAngleDegrees, deltaTimeSeconds);

        public static CombatVector3 SwingDirection(CombatVector3 previousTip, CombatVector3 currentTip)
        {
            return (currentTip - previousTip).Normalized;
        }
    }
}
