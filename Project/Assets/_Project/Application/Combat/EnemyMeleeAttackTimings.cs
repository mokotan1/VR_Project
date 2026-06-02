namespace VRProject.Application.Combat
{
    public readonly struct EnemyMeleeAttackTimings
    {
        public EnemyMeleeAttackTimings(float windUpSeconds, float activeSeconds, float recoverySeconds)
        {
            WindUpSeconds = windUpSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = recoverySeconds;
        }

        public float WindUpSeconds { get; }
        public float ActiveSeconds { get; }
        public float RecoverySeconds { get; }

        public static EnemyMeleeAttackTimings StalkerDefault =>
            new EnemyMeleeAttackTimings(0.6f, 0.15f, 0.8f);
    }
}
