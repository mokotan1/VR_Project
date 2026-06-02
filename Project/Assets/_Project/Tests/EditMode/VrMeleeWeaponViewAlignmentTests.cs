using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode
{
    public sealed class VrMeleeWeaponViewAlignmentTests
    {
        GameObject _anchorObject;
        GameObject _weaponObject;
        GameObject _handleObject;
        GameObject _tipObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_weaponObject);
            Object.DestroyImmediate(_anchorObject);
        }

        [Test]
        public void TryComputeSnapLocalRotation_AlignsBladeTowardViewCenter()
        {
            _anchorObject = new GameObject("Anchor");
            _anchorObject.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.identity);

            _weaponObject = new GameObject("MeleeWeapon_Axe");
            _handleObject = new GameObject("Handle");
            _tipObject = new GameObject("BladeTip");
            _handleObject.transform.SetParent(_weaponObject.transform, false);
            _handleObject.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            _tipObject.transform.SetParent(_weaponObject.transform, false);
            _tipObject.transform.localPosition = new Vector3(0f, 0f, 0.55f);

            var localOffset = new Vector3(0.32f, -0.12f, 0.55f);
            var viewPosition = new Vector3(1f, 2.1f, 2.2f);
            var viewForward = Vector3.forward;

            Assert.IsTrue(VrMeleeWeaponViewAlignment.TryComputeSnapLocalRotation(
                _anchorObject.transform,
                localOffset,
                _handleObject.transform.localPosition,
                _tipObject.transform.localPosition,
                viewPosition,
                viewForward,
                Vector3.up,
                out var localRotation));

            _weaponObject.transform.SetParent(_anchorObject.transform, false);
            _weaponObject.transform.localPosition = localOffset;
            _weaponObject.transform.localRotation = localRotation;

            var bladeWorld = (_tipObject.transform.position - _handleObject.transform.position).normalized;
            Assert.Greater(Vector3.Dot(bladeWorld, viewForward), 0.95f);
        }

        [Test]
        public void TryComputeSnapLocalRotation_ReturnsFalseWhenBladeAxisMissing()
        {
            _anchorObject = new GameObject("Anchor");

            Assert.IsFalse(VrMeleeWeaponViewAlignment.TryComputeSnapLocalRotation(
                _anchorObject.transform,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                Vector3.forward,
                Vector3.up,
                out _));
        }
    }
}
