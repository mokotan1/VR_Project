using NUnit.Framework;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class OsFpsInspiredWeaponThrowGateTests
    {
        [Test]
        public void ShouldThrow_When_AmmoZero_And_NotReloading()
        {
            var ok = OsFpsInspiredWeaponThrowGate.ShouldThrowOnFire(0, false);
            Assert.That(ok, Is.True);
        }

        [Test]
        public void ShouldThrow_When_AmmoNegative_And_NotReloading()
        {
            var ok = OsFpsInspiredWeaponThrowGate.ShouldThrowOnFire(-1, false);
            Assert.That(ok, Is.True);
        }

        [Test]
        public void ShouldNotThrow_When_AmmoPositive()
        {
            var ok = OsFpsInspiredWeaponThrowGate.ShouldThrowOnFire(1, false);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void ShouldNotThrow_When_Reloading_EvenIfAmmoZero()
        {
            var ok = OsFpsInspiredWeaponThrowGate.ShouldThrowOnFire(0, true);
            Assert.That(ok, Is.False);
        }
    }
}
