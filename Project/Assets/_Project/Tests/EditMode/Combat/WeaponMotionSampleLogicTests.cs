using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class WeaponMotionSampleLogicTests
    {
        [Test]
        public void LinearSpeed_ComputesFromDelta()
        {
            var prev = new CombatVector3(0f, 0f, 0f);
            var current = new CombatVector3(1f, 0f, 0f);
            var speed = WeaponMotionSampleLogic.LinearSpeedMetersPerSecond(prev, current, 0.5f);
            Assert.AreEqual(2f, speed, 1e-4f);
        }

        [Test]
        public void SwingDirection_NormalizesDelta()
        {
            var dir = WeaponMotionSampleLogic.SwingDirection(
                new CombatVector3(0f, 0f, 0f),
                new CombatVector3(0f, 2f, 0f));
            Assert.AreEqual(0f, dir.X, 1e-4f);
            Assert.AreEqual(1f, dir.Y, 1e-4f);
        }
    }
}
