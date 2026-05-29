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
                WeaponAttackKind.Slash,
                WeaponFamily.Hybrid);

            Assert.IsTrue(ok);
        }
    }
}
