using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class WeaponAttackSessionLogicTests
    {
        [Test]
        public void Tick_StartsSession_WhenSpeedExceedsEnterThreshold()
        {
            var state = new WeaponAttackSessionState(false, 0, 0, 0f);
            var result = WeaponAttackSessionLogic.Tick(
                state,
                linearSpeed: 2f,
                angularSpeed: 0f,
                enterLinear: 1.5f,
                enterAngular: 120f,
                exitLinear: 0.8f,
                exitAngular: 60f,
                exitIdleFramesRequired: 2,
                maxSessionDurationSeconds: 0.5f,
                deltaTimeSeconds: 0.02f);

            Assert.IsTrue(result.SessionStarted);
            Assert.IsTrue(result.NextState.IsActive);
            Assert.AreEqual(1, result.NextState.SessionId);
        }

        [Test]
        public void Tick_EndsSession_AfterIdleFramesBelowExit()
        {
            var active = new WeaponAttackSessionState(true, 3, 0, 0.1f);
            var first = WeaponAttackSessionLogic.Tick(
                active,
                linearSpeed: 0.1f,
                angularSpeed: 10f,
                enterLinear: 1.5f,
                enterAngular: 120f,
                exitLinear: 0.8f,
                exitAngular: 60f,
                exitIdleFramesRequired: 2,
                maxSessionDurationSeconds: 1f,
                deltaTimeSeconds: 0.02f);

            Assert.IsFalse(first.SessionEnded);
            Assert.AreEqual(1, first.NextState.IdleFrameCount);

            var second = WeaponAttackSessionLogic.Tick(
                first.NextState,
                linearSpeed: 0.1f,
                angularSpeed: 10f,
                enterLinear: 1.5f,
                enterAngular: 120f,
                exitLinear: 0.8f,
                exitAngular: 60f,
                exitIdleFramesRequired: 2,
                maxSessionDurationSeconds: 1f,
                deltaTimeSeconds: 0.02f);

            Assert.IsTrue(second.SessionEnded);
            Assert.IsFalse(second.NextState.IsActive);
        }
    }
}
