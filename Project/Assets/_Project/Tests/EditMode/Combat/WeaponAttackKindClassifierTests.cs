using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class WeaponAttackKindClassifierTests
    {
        [Test]
        public void Classify_Stab_WhenForwardDotHigh()
        {
            var kind = WeaponAttackKindClassifier.Classify(
                new CombatVector3(0f, 0f, 3f),
                weaponForward: new CombatVector3(0f, 0f, 1f),
                weaponRight: new CombatVector3(1f, 0f, 0f),
                WeaponFamily.Hybrid,
                linearSpeed: 2f,
                angularSpeed: 20f,
                stabForwardDotMin: 0.6f,
                slashSideDotMin: 0.5f,
                bluntMaxAngularSpeed: 90f,
                bluntMinLinearSpeed: 1.5f);

            Assert.AreEqual(WeaponAttackKind.Stab, kind);
        }

        [Test]
        public void Classify_Slash_WhenSideDotHigh()
        {
            var kind = WeaponAttackKindClassifier.Classify(
                new CombatVector3(3f, 0f, 0f),
                weaponForward: new CombatVector3(0f, 0f, 1f),
                weaponRight: new CombatVector3(1f, 0f, 0f),
                WeaponFamily.Hybrid,
                linearSpeed: 2f,
                angularSpeed: 120f,
                stabForwardDotMin: 0.85f,
                slashSideDotMin: 0.5f,
                bluntMaxAngularSpeed: 90f,
                bluntMinLinearSpeed: 1.5f);

            Assert.AreEqual(WeaponAttackKind.Slash, kind);
        }

        [Test]
        public void IsKindAllowed_RespectsFamily()
        {
            Assert.IsFalse(WeaponAttackKindClassifier.IsKindAllowed(WeaponAttackKind.Stab, WeaponFamily.Slash));
            Assert.IsTrue(WeaponAttackKindClassifier.IsKindAllowed(WeaponAttackKind.Slash, WeaponFamily.Slash));
        }
    }
}
