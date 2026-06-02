using NUnit.Framework;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class EnemyPoseDemolishOnDeathTests
    {
        [Test]
        public void ResolveMeleeFragmentImpulse_UsesConfiguredAndWeaponValues()
        {
            Assert.AreEqual(12f, EnemyPoseDemolishOnDeath.ResolveMeleeFragmentImpulse(12f, 4f));
            Assert.AreEqual(15f, EnemyPoseDemolishOnDeath.ResolveMeleeFragmentImpulse(7.5f, 15f));
        }

        [Test]
        public void TryDemolishFromMeleeHit_UsesLastHitPoint()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var effect = go.AddComponent<EnemyPoseDemolishOnDeath>();
                var hitPoint = new Vector3(0.3f, 0.8f, -0.2f);
                var captured = Vector3.negativeInfinity;

                effect.SetFragmentFactoryForTests((_, point) =>
                {
                    captured = point;
                    return true;
                });

                Assert.IsTrue(effect.TryDemolishFromMeleeHit(hitPoint, Vector3.forward, 10f));
                Assert.AreEqual(hitPoint, captured);
            }
            finally
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

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
                InitializeDamageableForEditMode(damageable);

                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
                damageable.ApplyDamage(1000f, hitPoint);

                Assert.AreEqual(hitPoint, captured);
            }
            finally
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        static void InitializeDamageableForEditMode(OsFpsInspiredDamageable damageable)
        {
            var healthField = typeof(OsFpsInspiredDamageable).GetField(
                "_health",
                BindingFlags.Instance | BindingFlags.NonPublic);
            healthField?.SetValue(damageable, damageable.MaxHealth);
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
        public void LowPolyShardBurst_SpawnsMobileSafeFragments()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            GameObject burst = null;
            try
            {
                var settings = EnemyLowPolyShardBurst.Settings.MobileDefault;
                burst = EnemyLowPolyShardBurst.Spawn(
                    "Enemy_Agent",
                    new Bounds(Vector3.zero, Vector3.one * 2f),
                    Vector3.zero,
                    material,
                    settings);

                Assert.IsNotNull(burst);
                Assert.LessOrEqual(burst.transform.childCount, settings.ShardCount);
                Assert.Greater(burst.transform.childCount, 0);
                Assert.GreaterOrEqual(settings.ShardScale, 0.7f);
                Assert.GreaterOrEqual(settings.Impulse, 7f);
                Assert.GreaterOrEqual(settings.SpreadMultiplier, 1.5f);

                var farthestSqrDistance = 0f;
                foreach (Transform shard in burst.transform)
                {
                    Assert.IsNotNull(shard.GetComponent<MeshFilter>());
                    Assert.IsNotNull(shard.GetComponent<MeshRenderer>());
                    Assert.IsNull(shard.GetComponent<MeshCollider>());

                    var rb = shard.GetComponent<Rigidbody>();
                    Assert.IsNotNull(rb);
                    Assert.AreEqual(CollisionDetectionMode.Discrete, rb.collisionDetectionMode);
                    farthestSqrDistance = Mathf.Max(farthestSqrDistance, shard.position.sqrMagnitude);
                }

                Assert.Greater(Mathf.Sqrt(farthestSqrDistance), 0.8f);
            }
            finally
            {
                if (burst != null)
                    Object.DestroyImmediate(burst);
                if (material != null)
                    Object.DestroyImmediate(material);
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

        [Test]
        public void WeaponMuzzleDefaults_CreatesPersistentFirePointWhenGunHasNoRenderer()
        {
            var gun = new GameObject("HandGun_HK416");
            try
            {
                var firePoint = UnityChanPrototypeWeaponMuzzleDefaults.EnsureFirePoint(gun);

                Assert.IsNotNull(firePoint);
                Assert.AreEqual(UnityChanPrototypeWeaponMuzzleDefaults.FirePointName, firePoint.name);
                Assert.AreSame(gun.transform, firePoint.parent);
                Assert.AreEqual(UnityChanPrototypeWeaponMuzzleDefaults.FallbackFirePointLocalPosition, firePoint.localPosition);
            }
            finally
            {
                if (gun != null)
                    Object.DestroyImmediate(gun);
            }
        }

        [Test]
        public void WeaponMuzzleDefaults_ReusesAuthoredFirePoint()
        {
            var gun = new GameObject("HandGun_HK416");
            var authored = new GameObject(UnityChanPrototypeWeaponMuzzleDefaults.FirePointName).transform;
            try
            {
                authored.SetParent(gun.transform, false);
                authored.localPosition = new Vector3(0.1f, 0.2f, 0.3f);

                var firePoint = UnityChanPrototypeWeaponMuzzleDefaults.EnsureFirePoint(gun);

                Assert.AreSame(authored, firePoint);
                Assert.AreEqual(new Vector3(0.1f, 0.2f, 0.3f), firePoint.localPosition);
            }
            finally
            {
                if (gun != null)
                    Object.DestroyImmediate(gun);
            }
        }
    }
}
