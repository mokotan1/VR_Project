using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.VR;

namespace VRProject.Tests.EditMode
{
    public sealed class VrWeaponGripPoseTests
    {
        [Test]
        public void FindByName_FindsInactiveDescendant_ByPreferredName()
        {
            var root = new GameObject("Weapon");
            var nested = new GameObject("Nested");
            nested.transform.SetParent(root.transform, false);
            var grip = new GameObject(VrWeaponGripPoseNames.RightHandGrip);
            grip.SetActive(false);
            grip.transform.SetParent(nested.transform, false);

            var found = VrWeaponGripPoseResolver.FindByName(
                root.transform,
                VrWeaponGripPoseNames.RightHandGrip);

            Assert.That(found, Is.SameAs(grip.transform));
        }

        [Test]
        public void AutoBindMissingReferences_BindsExistingGripMarkers()
        {
            var root = new GameObject("Weapon");
            var right = new GameObject(VrWeaponGripPoseNames.RightHandGrip);
            var left = new GameObject(VrWeaponGripPoseNames.LeftHandGrip);
            var muzzle = new GameObject(VrWeaponGripPoseNames.WeaponFirePoint);
            var aim = new GameObject(VrWeaponGripPoseNames.AimReference);
            right.transform.SetParent(root.transform, false);
            left.transform.SetParent(root.transform, false);
            muzzle.transform.SetParent(root.transform, false);
            aim.transform.SetParent(root.transform, false);
            var pose = root.AddComponent<VrWeaponGripPose>();

            pose.AutoBindMissingReferences();

            Assert.That(pose.RightHandGrip, Is.SameAs(right.transform));
            Assert.That(pose.LeftHandGrip, Is.SameAs(left.transform));
            Assert.That(pose.Muzzle, Is.SameAs(muzzle.transform));
            Assert.That(pose.AimReference, Is.SameAs(aim.transform));
        }

        [Test]
        public void AutoBindMissingReferences_UsesMuzzleAsAimReference_WhenAimReferenceMissing()
        {
            var root = new GameObject("Weapon");
            var muzzle = new GameObject(VrWeaponGripPoseNames.Muzzle);
            muzzle.transform.SetParent(root.transform, false);
            var pose = root.AddComponent<VrWeaponGripPose>();

            pose.AutoBindMissingReferences();

            Assert.That(pose.Muzzle, Is.SameAs(muzzle.transform));
            Assert.That(pose.AimReference, Is.SameAs(muzzle.transform));
        }
    }
}
