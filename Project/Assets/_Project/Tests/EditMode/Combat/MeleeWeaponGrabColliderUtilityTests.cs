using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class MeleeWeaponGrabColliderUtilityTests
    {
        [Test]
        public void PruneBrokenColliders_DisablesOversizedImportedBoxCollider()
        {
            var root = new GameObject("Fire_Axe_Test");
            var child = new GameObject("LODA");
            child.transform.SetParent(root.transform, false);
            var broken = child.AddComponent<BoxCollider>();
            broken.size = new Vector3(200f, 2600f, 880f);

            MeleeWeaponGrabColliderUtility.PruneBrokenColliders(root.transform);

            Assert.IsFalse(broken.enabled);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RefreshGrabColliders_RegistersFallbackSolidColliderOnRoot()
        {
            var root = new GameObject("MeleeWeapon_Test");
            var grab = root.AddComponent<XRGrabInteractable>();

            MeleeWeaponGrabColliderUtility.RefreshGrabColliders(grab);

            Assert.Greater(grab.colliders.Count, 0);
            Assert.IsFalse(grab.colliders[0].isTrigger);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RestoreWorldPickupPhysics_UnparentsAndEnablesGravity()
        {
            var parent = new GameObject("Hand");
            var weapon = new GameObject("MeleeWeapon_Test");
            weapon.transform.SetParent(parent.transform, false);
            var body = weapon.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            MeleeWeaponGrabColliderUtility.RestoreWorldPickupPhysics(weapon.transform);

            Assert.IsNull(weapon.transform.parent);
            Assert.IsFalse(body.isKinematic);
            Assert.IsTrue(body.useGravity);
            Object.DestroyImmediate(parent);
        }
    }
}
