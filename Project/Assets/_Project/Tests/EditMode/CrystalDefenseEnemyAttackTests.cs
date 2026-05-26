using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class CrystalDefenseEnemyAttackTests
    {
        [Test]
        public void TryAttackCrystal_WhenInRange_DamagesOnceAndConsumesEnemy()
        {
            var crystalGo = new GameObject("Crystal");
            var enemyGo = new GameObject("Enemy");

            try
            {
                var crystal = crystalGo.AddComponent<CrystalCoreHealth>();
                crystal.ResetHealth(100f);

                enemyGo.transform.position = crystalGo.transform.position;
                var attack = enemyGo.AddComponent<CrystalDefenseEnemyAttack>();

                var attacked = attack.TryAttackCrystal(crystal, Vector3.zero);

                Assert.IsTrue(attacked);
                Assert.AreEqual(85f, crystal.Health, 0.001f);
                Assert.IsTrue(enemyGo == null);
            }
            finally
            {
                if (crystalGo != null)
                    UnityEngine.Object.DestroyImmediate(crystalGo);
                if (enemyGo != null)
                    UnityEngine.Object.DestroyImmediate(enemyGo);
            }
        }
    }
}
