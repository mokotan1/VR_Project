using NUnit.Framework;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class WeaponFireScreenImpulseProfileTests
    {
        [Test]
        public void PulseWeight_AtStart_IsOne()
        {
            Assert.That(WeaponFireScreenImpulseProfile.PulseWeight(0f, 0.2f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PulseWeight_AfterDuration_IsZero()
        {
            Assert.That(WeaponFireScreenImpulseProfile.PulseWeight(0.2f, 0.2f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PulseWeight_Halfway_EasesDown()
        {
            var weight = WeaponFireScreenImpulseProfile.PulseWeight(0.1f, 0.2f);
            Assert.That(weight, Is.GreaterThan(0f));
            Assert.That(weight, Is.LessThan(1f));
        }

        [Test]
        public void EffectiveKickStrength_InXr_UsesComfortMultiplier()
        {
            var strength = WeaponFireScreenImpulseProfile.EffectiveKickStrength(2f, 0.25f, true);
            Assert.That(strength, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void EffectiveKickStrength_InFlatMode_UsesFullStrength()
        {
            var strength = WeaponFireScreenImpulseProfile.EffectiveKickStrength(2f, 0.25f, false);
            Assert.That(strength, Is.EqualTo(2f).Within(0.001f));
        }
    }
}
