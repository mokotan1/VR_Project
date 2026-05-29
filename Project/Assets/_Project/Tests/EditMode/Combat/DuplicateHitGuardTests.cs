using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class DuplicateHitGuardTests
    {
        [Test]
        public void TryRegisterHit_BlocksDuplicateSessionTargetZone()
        {
            var guard = new DuplicateHitGuard();
            Assert.IsTrue(guard.TryRegisterHit(1, 10, 100, 1f, 0.2f));
            Assert.IsFalse(guard.TryRegisterHit(1, 10, 100, 1.05f, 0.2f));
        }

        [Test]
        public void TryRegisterHit_BlocksTargetDuringCooldown()
        {
            var guard = new DuplicateHitGuard();
            Assert.IsTrue(guard.TryRegisterHit(1, 10, 100, 1f, 0.2f));
            Assert.IsFalse(guard.TryRegisterHit(2, 10, 101, 1.05f, 0.2f));
            Assert.IsTrue(guard.TryRegisterHit(2, 10, 101, 1.25f, 0.2f));
        }
    }
}
