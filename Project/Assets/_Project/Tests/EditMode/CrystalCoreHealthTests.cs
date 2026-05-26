using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class CrystalCoreHealthTests
    {
        [Test]
        public void ApplyDamage_ReducesHealthAndRaisesDamaged()
        {
            var go = new GameObject("Crystal");
            var crystal = go.AddComponent<CrystalCoreHealth>();
            crystal.ResetHealth(100f);

            var damagedCount = 0;
            float lastHealth = -1f;
            crystal.Damaged += (_, remaining, _) =>
            {
                damagedCount++;
                lastHealth = remaining;
            };

            crystal.ApplyDamage(35f, Vector3.one);

            Assert.AreEqual(65f, crystal.Health, 0.001f);
            Assert.AreEqual(1, damagedCount);
            Assert.AreEqual(65f, lastHealth, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyDamage_WhenHealthReachesZero_RaisesDestroyedOnce()
        {
            var go = new GameObject("Crystal");
            var crystal = go.AddComponent<CrystalCoreHealth>();
            crystal.ResetHealth(50f);

            var destroyedCount = 0;
            crystal.Destroyed += _ => destroyedCount++;

            crystal.ApplyDamage(60f, Vector3.zero);
            crystal.ApplyDamage(60f, Vector3.zero);

            Assert.AreEqual(0f, crystal.Health, 0.001f);
            Assert.IsTrue(crystal.IsDestroyed);
            Assert.AreEqual(1, destroyedCount);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetHealth_RestoresHealthAndAllowsDestroyedEventAgain()
        {
            var go = new GameObject("Crystal");
            var crystal = go.AddComponent<CrystalCoreHealth>();
            crystal.ResetHealth(25f);

            var destroyedCount = 0;
            crystal.Destroyed += _ => destroyedCount++;

            crystal.ApplyDamage(25f, Vector3.zero);
            crystal.ResetHealth(10f);
            crystal.ApplyDamage(10f, Vector3.zero);

            Assert.AreEqual(0f, crystal.Health, 0.001f);
            Assert.AreEqual(2, destroyedCount);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyDamage_NonPositiveAmount_DoesNothing()
        {
            var go = new GameObject("Crystal");
            var crystal = go.AddComponent<CrystalCoreHealth>();
            crystal.ResetHealth(50f);

            var damagedCount = 0;
            crystal.Damaged += (_, __, ___) => damagedCount++;

            crystal.ApplyDamage(0f, Vector3.zero);
            crystal.ApplyDamage(-10f, Vector3.zero);

            Assert.AreEqual(50f, crystal.Health, 0.001f);
            Assert.AreEqual(0, damagedCount);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetHealth_ClampsToMinimumOne()
        {
            var go = new GameObject("Crystal");
            var crystal = go.AddComponent<CrystalCoreHealth>();

            crystal.ResetHealth(-5f);

            Assert.AreEqual(1f, crystal.MaxHealth, 0.001f);
            Assert.AreEqual(1f, crystal.Health, 0.001f);
            Assert.IsFalse(crystal.IsDestroyed);

            Object.DestroyImmediate(go);
        }
    }
}
