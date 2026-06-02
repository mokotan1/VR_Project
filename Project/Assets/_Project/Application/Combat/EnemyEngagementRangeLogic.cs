namespace VRProject.Application.Combat
{
    public enum EnemyEngagementMode
    {
        Chase,
        Melee,
        Ranged
    }

    public static class EnemyEngagementRangeLogic
    {
        public static EnemyEngagementMode Resolve(float distance, float meleeRange, float rangedMinDistance)
        {
            if (distance <= meleeRange)
                return EnemyEngagementMode.Melee;

            if (distance > rangedMinDistance)
                return EnemyEngagementMode.Ranged;

            return EnemyEngagementMode.Chase;
        }
    }
}
