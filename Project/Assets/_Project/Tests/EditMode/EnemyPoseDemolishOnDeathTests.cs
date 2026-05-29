using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class EnemyPoseDemolishOnDeathTests
    {
        [Test]
        public void BuildBreakPointPositions_UsesHitPointAsFirstPoint()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            var hitPoint = new Vector3(0.25f, 0.5f, -0.25f);

            var points = EnemyPoseDemolishOnDeath.BuildBreakPointPositions(bounds, hitPoint, 8, 0.35f);

            Assert.AreEqual(8, points.Length);
            Assert.AreEqual(hitPoint, points[0]);
        }

        [Test]
        public void DamageableLethalHit_TriggersDemolishWithLastHitPoint()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var damageable = go.AddComponent<OsFpsInspiredDamageable>();
                var effect = go.AddComponent<EnemyPoseDemolishOnDeath>();
                var hitPoint = new Vector3(0.2f, 0.4f, 0.1f);
                var captured = Vector3.negativeInfinity;

                effect.SetFragmentFactoryForTests((_, point) =>
                {
                    captured = point;
                    return true;
                });

                damageable.ApplyDamage(1000f, hitPoint);

                Assert.AreEqual(hitPoint, captured);
            }
            finally
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DamageableNonLethalHit_DoesNotTriggerDemolish()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var damageable = go.AddComponent<OsFpsInspiredDamageable>();
                var effect = go.AddComponent<EnemyPoseDemolishOnDeath>();
                var triggerCount = 0;

                effect.SetFragmentFactoryForTests((_, _) =>
                {
                    triggerCount++;
                    return true;
                });

                damageable.ApplyDamage(1f, Vector3.one);

                Assert.AreEqual(0, triggerCount);
            }
            finally
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SetupUnityChanPrototypeEnemy_AddsOnlyGenericDeathDemolishComponents()
        {
            var enemy = new GameObject("Enemy_Agent");
            try
            {
                enemy.AddComponent<NavMeshAgent>();

                var configured = UnityChanPrototypeEnemyDemolishSetup.Ensure(enemy);

                Assert.IsTrue(configured);
                Assert.IsNotNull(enemy.GetComponent<OsFpsInspiredDamageable>());
                Assert.IsNotNull(enemy.GetComponent<EnemyPoseDemolishOnDeath>());
                Assert.IsNull(enemy.GetComponent<CrystalDefenseEnemyObjective>());
                Assert.IsNull(enemy.GetComponent<CrystalDefenseEnemyAttack>());
            }
            finally
            {
                if (enemy != null)
                    Object.DestroyImmediate(enemy);
            }
        }
    }
}
