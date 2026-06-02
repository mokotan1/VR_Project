using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Combat;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode.Gameplay
{
    public sealed class SuperhotVrHk416FireLogicTests
    {
        [Test]
        public void ShouldFireThisFrame_RequiresGripHeldAndTriggerEdge()
        {
            Assert.IsTrue(SuperhotVrHk416FireLogic.ShouldFireThisFrame(true, true));
            Assert.IsFalse(SuperhotVrHk416FireLogic.ShouldFireThisFrame(false, true));
            Assert.IsFalse(SuperhotVrHk416FireLogic.ShouldFireThisFrame(true, false));
        }

        [Test]
        public void TryGetHk416OnHandAnchor_ReturnsFalseWhenEmpty()
        {
            var anchor = new GameObject("Right Controller").transform;
            Assert.IsFalse(SuperhotVrHk416FireLogic.TryGetHk416OnHandAnchor(anchor, out _));
            Object.DestroyImmediate(anchor.gameObject);
        }

        [Test]
        public void ComputeAimDirectionFromViewport_AimsFromMuzzleThroughViewportCenter()
        {
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.6f, 0f);
            cam.transform.rotation = Quaternion.identity;
            var muzzle = new Vector3(0.2f, 1.2f, 0f);
            try
            {
                var dir = UnityChanPrototypeWeaponMuzzleDefaults.ComputeAimDirectionFromViewport(cam, muzzle, 50f);
                Assert.Less(dir.y, 0.05f, "Horizon aim should not dive into the floor from a low muzzle.");
                Assert.Greater(dir.z, 0.9f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void TryGetShotPoseFromFirePoint_UsesTransformPositionAndForward()
        {
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "HandGun_HK416";
            var firePoint = new GameObject("WeaponFirePoint").transform;
            firePoint.SetParent(gun.transform, false);
            firePoint.localPosition = new Vector3(0f, 0.05f, 0.4f);
            firePoint.localRotation = Quaternion.Euler(10f, 0f, 0f);
            try
            {
                Assert.IsTrue(UnityChanPrototypeWeaponMuzzleDefaults.TryGetShotPoseFromFirePoint(
                    firePoint,
                    out var pos,
                    out var dir));
                Assert.Less(Vector3.Distance(pos, firePoint.position), 1e-4f);
                Assert.Less(Vector3.Angle(dir, firePoint.forward), 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(gun);
            }
        }

        [Test]
        public void RotationForFlight_ZeroOffset_PointsLocalZAlongVelocity()
        {
            var dir = new Vector3(0f, 0f, 1f);
            var rot = PrototypeFpsBulletProjectile.RotationForFlight(dir, Vector3.zero);
            Assert.Less(Vector3.Distance(dir, rot * Vector3.forward), 1e-3f);
        }

        [Test]
        public void TryComputeFirePointLocalPosition_Hk416_AddsForwardFromMuzzleTip()
        {
            var hk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hk.name = "HandGun_HK416";
            hk.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            var toy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            toy.name = "ToyGun";
            toy.transform.localScale = hk.transform.localScale;
            try
            {
                Assert.IsTrue(UnityChanPrototypeWeaponMuzzleDefaults.TryComputeFirePointLocalPosition(toy, out var toyTip));
                Assert.IsTrue(UnityChanPrototypeWeaponMuzzleDefaults.TryComputeFirePointLocalPosition(hk, out var hkTip));
                Assert.AreEqual(
                    toyTip + new Vector3(0f, 0f, UnityChanPrototypeWeaponMuzzleDefaults.Hk416FirePointForwardFromMuzzleLocalZ),
                    hkTip);
            }
            finally
            {
                Object.DestroyImmediate(hk);
                Object.DestroyImmediate(toy);
            }
        }

        [Test]
        public void TryGetMuzzleWorldPose_MovesWithGunRotation()
        {
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "HandGun_HK416";
            gun.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            try
            {
                UnityChanPrototypeWeaponMuzzleDefaults.EnsureFirePoint(gun);
                gun.transform.rotation = Quaternion.identity;
                Assert.IsTrue(UnityChanPrototypeWeaponMuzzleDefaults.TryGetMuzzleWorldPose(
                    gun, out var posA, out _));

                gun.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Assert.IsTrue(UnityChanPrototypeWeaponMuzzleDefaults.TryGetMuzzleWorldPose(
                    gun, out var posB, out _));

                Assert.Greater(Vector3.Distance(posA, posB), 0.01f);
            }
            finally
            {
                if (gun != null)
                    Object.DestroyImmediate(gun);
            }
        }

        [Test]
        public void TryGetHk416OnHandAnchor_FindsWeaponPickupSnappedToController()
        {
            var anchor = new GameObject("Right Controller").transform;
            var pickup = new GameObject("WeaponPickup_HK416");
            pickup.transform.SetParent(anchor, false);
            var visual = new GameObject("PickupVisual_HK416");
            visual.transform.SetParent(pickup.transform, false);

            Assert.IsTrue(SuperhotVrHk416FireLogic.TryGetHk416OnHandAnchor(anchor, out var weaponRoot));
            Assert.AreEqual(pickup.transform, weaponRoot);

            Object.DestroyImmediate(anchor.gameObject);
        }

        [Test]
        public void TryGetHk416OnHandAnchor_FindsHandGunUnderWeaponSocket()
        {
            var anchor = new GameObject("Right Controller").transform;
            var socket = new GameObject("WeaponSocket").transform;
            socket.SetParent(anchor, false);
            var hk416 = new GameObject("HandGun_HK416");
            hk416.transform.SetParent(socket, false);

            Assert.IsTrue(SuperhotVrHk416FireLogic.TryGetHk416OnHandAnchor(anchor, out var weaponRoot));
            Assert.AreEqual(hk416.transform, weaponRoot);

            Object.DestroyImmediate(anchor.gameObject);
        }

        [Test]
        public void TryRaycastKillEnemy_KillsSuperhotEnemyAlongAimRay()
        {
            var shooter = new GameObject("Shooter");
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(0f, 0f, 5f);
            enemy.AddComponent<SuperhotEnemy>();

            var aimRay = new Ray(shooter.transform.position, Vector3.forward);
            Assert.IsTrue(SuperhotVrHk416FireLogic.TryRaycastKillEnemy(
                aimRay,
                maxDistance: 20f,
                hitMask: Physics.DefaultRaycastLayers,
                exclusionRoot: shooter.transform,
                out var hit));
            Assert.Greater(hit.distance, 0f);

            Object.DestroyImmediate(shooter);
            if (enemy != null)
                Object.DestroyImmediate(enemy);
        }

        [Test]
        public void TrySweepSegmentKillEnemy_KillsSuperhotEnemyWhenSegmentReachesCollider()
        {
            var shooter = new GameObject("Shooter");
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(0f, 0f, 5f);
            enemy.AddComponent<SuperhotEnemy>();

            var segmentStart = new Vector3(0f, 0f, 4f);
            var segmentEnd = new Vector3(0f, 0f, 6f);
            Assert.IsTrue(SuperhotVrHk416FireLogic.TrySweepSegmentKillEnemy(
                segmentStart,
                segmentEnd,
                sphereRadius: 0.05f,
                hitMask: Physics.DefaultRaycastLayers,
                exclusionRoot: shooter.transform,
                out var hit));
            Assert.Greater(hit.distance, 0f);

            Object.DestroyImmediate(shooter);
            if (enemy != null)
                Object.DestroyImmediate(enemy);
        }

        [Test]
        public void TrySweepSegmentKillEnemy_DoesNotKillBeforeSegmentReachesEnemy()
        {
            var shooter = new GameObject("Shooter");
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(0f, 0f, 5f);
            var enemyComponent = enemy.AddComponent<SuperhotEnemy>();

            var segmentStart = new Vector3(0f, 0f, 0f);
            var segmentEnd = new Vector3(0f, 0f, 2f);
            Assert.IsFalse(SuperhotVrHk416FireLogic.TrySweepSegmentKillEnemy(
                segmentStart,
                segmentEnd,
                sphereRadius: 0.05f,
                hitMask: Physics.DefaultRaycastLayers,
                exclusionRoot: shooter.transform,
                out _));
            Assert.IsNotNull(enemyComponent);

            Object.DestroyImmediate(shooter);
            Object.DestroyImmediate(enemy);
        }
    }
}
