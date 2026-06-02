using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class MeleeHitValidatorTests
    {
        [Test]
        public void IsQualifyingHit_False_WhenSessionInactive()
        {
            var ok = MeleeHitValidator.IsQualifyingHit(
                sessionActive: false,
                qualifyingScore: 1f,
                minQualifyingScore: 0.3f,
                linearSpeedMetersPerSecond: 2f,
                angularSpeedDegreesPerSecond: 120f,
                minHitLinearSpeed: 1.5f,
                minHitAngularSpeed: 110f,
                WeaponAttackKind.Slash,
                WeaponFamily.Hybrid);
            Assert.IsFalse(ok);
        }

        [Test]
        public void IsQualifyingHit_True_WhenScoreAndKindValid()
        {
            var score = MeleeQualifyingHitCalculator.QualifyingScore(
                linearSpeedMetersPerSecond: 2f,
                minLinearSpeed: 1f,
                referenceLinearSpeed: 3f,
                WeaponAttackKind.Slash,
                zoneFeedbackMultiplier: 1f);

            var ok = MeleeHitValidator.IsQualifyingHit(
                sessionActive: true,
                qualifyingScore: score,
                minQualifyingScore: 0.2f,
                linearSpeedMetersPerSecond: 2f,
                angularSpeedDegreesPerSecond: 120f,
                minHitLinearSpeed: 1.5f,
                minHitAngularSpeed: 110f,
                WeaponAttackKind.Slash,
                WeaponFamily.Hybrid);

            Assert.IsTrue(ok);
        }

        [Test]
        public void IsQualifyingHit_False_WhenSessionActiveButHitSpeedTooLow()
        {
            var ok = MeleeHitValidator.IsQualifyingHit(
                sessionActive: true,
                qualifyingScore: 0.9f,
                minQualifyingScore: 0.35f,
                linearSpeedMetersPerSecond: 0.2f,
                angularSpeedDegreesPerSecond: 10f,
                minHitLinearSpeed: 1.6f,
                minHitAngularSpeed: 110f,
                WeaponAttackKind.Slash,
                WeaponFamily.Hybrid);

            Assert.IsFalse(ok);
        }
    }
}
