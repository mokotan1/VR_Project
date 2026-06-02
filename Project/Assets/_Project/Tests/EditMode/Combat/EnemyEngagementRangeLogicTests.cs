using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class EnemyEngagementRangeLogicTests
    {
        [Test]
        public void Resolve_WithinMeleeRange_ReturnsMelee()
        {
            Assert.AreEqual(EnemyEngagementMode.Melee, EnemyEngagementRangeLogic.Resolve(1.5f, 2f, 10f));
        }

        [Test]
        public void Resolve_BeyondRangedThreshold_ReturnsRanged()
        {
            Assert.AreEqual(EnemyEngagementMode.Ranged, EnemyEngagementRangeLogic.Resolve(10.1f, 2f, 10f));
        }

        [Test]
        public void Resolve_BetweenMeleeAndRanged_ReturnsChase()
        {
            Assert.AreEqual(EnemyEngagementMode.Chase, EnemyEngagementRangeLogic.Resolve(5f, 2f, 10f));
        }
    }
}
