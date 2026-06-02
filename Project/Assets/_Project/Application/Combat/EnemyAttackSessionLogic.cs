namespace VRProject.Application.Combat
{
    public readonly struct EnemyAttackState
    {
        public EnemyAttackState(EnemyAttackPhase phase, float phaseElapsedSeconds)
        {
            Phase = phase;
            PhaseElapsedSeconds = phaseElapsedSeconds;
        }

        public EnemyAttackPhase Phase { get; }
        public float PhaseElapsedSeconds { get; }

        public static EnemyAttackState Idle => new EnemyAttackState(EnemyAttackPhase.Idle, 0f);
    }

    public readonly struct EnemyAttackAdvanceResult
    {
        public EnemyAttackAdvanceResult(
            EnemyAttackState nextState,
            bool enteredActive,
            bool attackCompleted)
        {
            NextState = nextState;
            EnteredActive = enteredActive;
            AttackCompleted = attackCompleted;
        }

        public EnemyAttackState NextState { get; }
        public bool EnteredActive { get; }
        public bool AttackCompleted { get; }
    }

    public static class EnemyAttackSessionLogic
    {
        public static bool CanBeginAttack(EnemyAttackState state, float distanceToTarget, float attackRange, bool hasTarget) =>
            state.Phase == EnemyAttackPhase.Idle
            && hasTarget
            && distanceToTarget <= attackRange;

        public static EnemyAttackState BeginAttack(EnemyAttackState state)
        {
            if (state.Phase != EnemyAttackPhase.Idle)
                return state;

            return new EnemyAttackState(EnemyAttackPhase.WindUp, 0f);
        }

        public static bool IsHitboxActive(EnemyAttackPhase phase) => phase == EnemyAttackPhase.Active;

        public static bool IsAttacking(EnemyAttackPhase phase) => phase != EnemyAttackPhase.Idle;

        public static EnemyAttackAdvanceResult Advance(
            EnemyAttackState state,
            EnemyMeleeAttackTimings timings,
            float deltaTimeSeconds)
        {
            if (state.Phase == EnemyAttackPhase.Idle)
                return new EnemyAttackAdvanceResult(state, false, false);

            var elapsed = state.PhaseElapsedSeconds + deltaTimeSeconds;

            switch (state.Phase)
            {
                case EnemyAttackPhase.WindUp:
                    if (elapsed >= timings.WindUpSeconds)
                    {
                        return new EnemyAttackAdvanceResult(
                            new EnemyAttackState(EnemyAttackPhase.Active, 0f),
                            enteredActive: true,
                            attackCompleted: false);
                    }

                    return new EnemyAttackAdvanceResult(
                        new EnemyAttackState(EnemyAttackPhase.WindUp, elapsed),
                        false,
                        false);

                case EnemyAttackPhase.Active:
                    if (elapsed >= timings.ActiveSeconds)
                    {
                        return new EnemyAttackAdvanceResult(
                            new EnemyAttackState(EnemyAttackPhase.Recovery, 0f),
                            false,
                            false);
                    }

                    return new EnemyAttackAdvanceResult(
                        new EnemyAttackState(EnemyAttackPhase.Active, elapsed),
                        false,
                        false);

                case EnemyAttackPhase.Recovery:
                    if (elapsed >= timings.RecoverySeconds)
                    {
                        return new EnemyAttackAdvanceResult(
                            EnemyAttackState.Idle,
                            false,
                            attackCompleted: true);
                    }

                    return new EnemyAttackAdvanceResult(
                        new EnemyAttackState(EnemyAttackPhase.Recovery, elapsed),
                        false,
                        false);

                default:
                    return new EnemyAttackAdvanceResult(state, false, false);
            }
        }
    }
}
