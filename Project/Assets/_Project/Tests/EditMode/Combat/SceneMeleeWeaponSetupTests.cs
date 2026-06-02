using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Application.Combat;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class SceneMeleeWeaponSetupTests
    {
        [Test]
        public void Ensure_OnHk416Root_AddsGrabLifecycleAndHitDetector()
        {
            var root = CreateHk416PickupRoot();
            var profile = ScriptableObject.CreateInstance<WeaponAttackProfile>();
            root.AddComponent<SceneMeleeWeaponProfileSource>().SetProfile(profile);

            Assert.IsTrue(SceneMeleeWeaponSetup.Ensure(root, profile));
            Assert.IsNotNull(root.GetComponent<XRGrabInteractable>());
            Assert.IsNotNull(root.GetComponent<WeaponHitDetector>());
            Assert.IsNotNull(root.GetComponent<MeleeWeaponVrGrabLifecycle>());
            Assert.IsNotNull(root.GetComponent<VrGrabbedWeaponMotionSource>());
            Assert.IsNotNull(root.GetComponent<WeaponMotionSourceRouter>());

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Ensure_OnHk416Root_IsIdempotent()
        {
            var root = CreateHk416PickupRoot();
            var profile = ScriptableObject.CreateInstance<WeaponAttackProfile>();
            root.AddComponent<SceneMeleeWeaponProfileSource>().SetProfile(profile);

            Assert.IsTrue(SceneMeleeWeaponSetup.Ensure(root, profile));
            Assert.IsFalse(SceneMeleeWeaponSetup.Ensure(root, profile));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void IsHk416WeaponRoot_MatchesPickupNames()
        {
            Assert.IsTrue(SceneMeleeWeaponSetup.IsHk416WeaponRoot(new GameObject("WeaponPickup_HK416")));
            Assert.IsTrue(SceneMeleeWeaponSetup.IsHk416WeaponRoot(new GameObject("HandGun_HK416")));
            Assert.IsFalse(SceneMeleeWeaponSetup.IsHk416WeaponRoot(new GameObject("MeleeWeapon_Axe")));
        }

        [Test]
        public void IsHk416WeaponRoot_DoesNotMatchParentOfPickup()
        {
            var nav = new GameObject("NavWorld");
            var pickup = new GameObject("WeaponPickup_HK416");
            pickup.transform.SetParent(nav.transform);

            Assert.IsFalse(SceneMeleeWeaponSetup.IsHk416WeaponRoot(nav));
            Assert.IsTrue(SceneMeleeWeaponSetup.IsHk416FloorPickupRoot(pickup));

            Object.DestroyImmediate(nav);
        }

        [Test]
        public void TryStripMiswiredSceneMeleeStack_RemovesRigidbodyFromNavWorld()
        {
            var nav = new GameObject("NavWorld");
            nav.AddComponent<Rigidbody>();
            nav.AddComponent<SceneMeleeWeaponAutoSetup>();

            Assert.IsTrue(SceneMeleeWeaponSetup.TryStripMiswiredSceneMeleeStack(nav));
            Assert.IsNull(nav.GetComponent<Rigidbody>());
            Assert.IsNull(nav.GetComponent<SceneMeleeWeaponAutoSetup>());

            Object.DestroyImmediate(nav);
        }

        [Test]
        public void TryStripMiswiredSceneMeleeStack_RemovesStackFromUnityChanPlayer()
        {
            var player = new GameObject("UnityChan_Player");
            player.AddComponent<Rigidbody>();
            player.AddComponent<XRGrabInteractable>();
            player.AddComponent<SceneMeleeWeaponAutoSetup>();

            Assert.IsTrue(SceneMeleeWeaponSetup.TryStripMiswiredSceneMeleeStack(player));
            Assert.IsNull(player.GetComponent<Rigidbody>());
            Assert.IsNull(player.GetComponent<XRGrabInteractable>());

            Object.DestroyImmediate(player);
        }

        [Test]
        public void Ensure_RefusesUnityChanPlayer()
        {
            var player = new GameObject("UnityChan_Player");
            var profile = ScriptableObject.CreateInstance<WeaponAttackProfile>();

            Assert.IsFalse(SceneMeleeWeaponSetup.Ensure(player, profile));

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void IsAllowedMeleeWeaponRoot_AllowsFloorPickupOnly()
        {
            Assert.IsTrue(SceneMeleeWeaponSetup.IsAllowedMeleeWeaponRoot(new GameObject("WeaponPickup_HK416")));
            Assert.IsFalse(SceneMeleeWeaponSetup.IsAllowedMeleeWeaponRoot(new GameObject("UnityChan_Player")));
            Assert.IsFalse(SceneMeleeWeaponSetup.IsAllowedMeleeWeaponRoot(new GameObject("NavWorld")));
            Assert.IsTrue(SceneMeleeWeaponSetup.IsAllowedMeleeWeaponRoot(new GameObject("MeleeWeapon_Axe")));
        }

        static GameObject CreateHk416PickupRoot()
        {
            var root = new GameObject("WeaponPickup_HK416");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "PickupVisual_HK416";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.08f, 0.12f, 0.85f);
            Object.DestroyImmediate(visual.GetComponent<BoxCollider>());
            return root;
        }
    }
}
