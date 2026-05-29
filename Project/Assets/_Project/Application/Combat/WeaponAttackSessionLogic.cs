namespace VRProject.Application.Combat
{
    public readonly struct WeaponAttackSessionState
    {
        public WeaponAttackSessionState(bool isActive, int sessionId, int idleFrameCount, float activeDurationSeconds)
        {
            IsActive = isActive;
            SessionId = sessionId;
            IdleFrameCount = idleFrameCount;
            ActiveDurationSeconds = activeDurationSeconds;
        }

        public bool IsActive { get; }
        public int SessionId { get; }
        public int IdleFrameCount { get; }
        public float ActiveDurationSeconds { get; }
    }

    public readonly struct WeaponAttackSessionTickResult
    {
        public WeaponAttackSessionTickResult(
            WeaponAttackSessionState nextState,
            bool sessionStarted,
            bool sessionEnded)
        {
            NextState = nextState;
            SessionStarted = sessionStarted;
            SessionEnded = sessionEnded;
        }

        public WeaponAttackSessionState NextState { get; }
        public bool SessionStarted { get; }
        public bool SessionEnded { get; }
    }

    public static class WeaponAttackSessionLogic
    {
        public static bool ShouldEnterSession(float linearSpeed, float angularSpeed, float enterLinear, float enterAngular) =>
            linearSpeed >= enterLinear || angularSpeed >= enterAngular;

        public static bool ShouldExitSession(
            float linearSpeed,
            float angularSpeed,
            float exitLinear,
            float exitAngular,
            int idleFrameCount,
            int exitIdleFramesRequired,
            float activeDurationSeconds,
            float maxSessionDurationSeconds)
        {
            if (activeDurationSeconds >= maxSessionDurationSeconds)
                return true;

            if (linearSpeed <= exitLinear && angularSpeed <= exitAngular)
                return idleFrameCount + 1 >= exitIdleFramesRequired;

            return false;
        }

        public static WeaponAttackSessionTickResult Tick(
            WeaponAttackSessionState state,
            float linearSpeed,
            float angularSpeed,
            float enterLinear,
            float enterAngular,
            float exitLinear,
            float exitAngular,
            int exitIdleFramesRequired,
            float maxSessionDurationSeconds,
            float deltaTimeSeconds)
        {
            if (!state.IsActive)
            {
                if (!ShouldEnterSession(linearSpeed, angularSpeed, enterLinear, enterAngular))
                    return new WeaponAttackSessionTickResult(state, false, false);

                var nextSessionId = state.SessionId + 1;
                var active = new WeaponAttackSessionState(true, nextSessionId, 0, deltaTimeSeconds);
                return new WeaponAttackSessionTickResult(active, true, false);
            }

            var nextDuration = state.ActiveDurationSeconds + deltaTimeSeconds;
            var belowExit = linearSpeed <= exitLinear && angularSpeed <= exitAngular;
            var idleFrames = belowExit ? state.IdleFrameCount + 1 : 0;

            if (!ShouldExitSession(
                    linearSpeed,
                    angularSpeed,
                    exitLinear,
                    exitAngular,
                    idleFrames,
                    exitIdleFramesRequired,
                    nextDuration,
                    maxSessionDurationSeconds))
            {
                var continuing = new WeaponAttackSessionState(true, state.SessionId, idleFrames, nextDuration);
                return new WeaponAttackSessionTickResult(continuing, false, false);
            }

            var idle = new WeaponAttackSessionState(false, state.SessionId, 0, 0f);
            return new WeaponAttackSessionTickResult(idle, false, true);
        }
    }
}
