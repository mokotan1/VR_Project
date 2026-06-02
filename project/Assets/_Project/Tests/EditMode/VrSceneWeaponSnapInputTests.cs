using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEngine;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode
{
    public sealed class VrSceneWeaponSnapInputTests
    {
        GameObject _xrObject;
        GameObject _cameraObject;
        GameObject _rightController;
        GameObject _gun;
        GameObject _axe;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_xrObject);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_rightController);
            Object.DestroyImmediate(_gun);
            Object.DestroyImmediate(_axe);
        }

        [Test]
        public void SnapSceneWeaponsToRightFront_MovesExistingGunAndAxeWithoutCreatingRuntimeGun()
        {
            _xrObject = new GameObject("XR Origin (XR Rig)");
            var origin = _xrObject.AddComponent<XROrigin>();
            _cameraObject = new GameObject("Main Camera");
            _cameraObject.transform.SetParent(_xrObject.transform, false);
            origin.Camera = _cameraObject.AddComponent<Camera>();
            _rightController = new GameObject("Right Controller");
            _rightController.transform.SetParent(_xrObject.transform, false);
            _rightController.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.identity);

            _gun = new GameObject("WeaponPickup_HK416");
            _axe = new GameObject("MeleeWeapon_Axe");

            var snapInput = _xrObject.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(origin);

            snapInput.SnapSceneWeaponsToRightFront();

            var firstSlot = new Vector3(1.32f, 1.88f, 3.55f);
            var secondSlot = new Vector3(1.32f, 1.70f, 3.47f);
            Assert.IsTrue(IsInSnapSlot(_gun.transform.position, firstSlot, secondSlot));
            Assert.IsTrue(IsInSnapSlot(_axe.transform.position, firstSlot, secondSlot));
            Assert.Greater(Vector3.Distance(_gun.transform.position, _axe.transform.position), 0.001f);
            Assert.IsNull(_rightController.transform.Find("XR_RuntimeGun"));
        }

        static bool IsInSnapSlot(Vector3 actual, Vector3 firstSlot, Vector3 secondSlot)
        {
            return Vector3.Distance(actual, firstSlot) < 0.001f ||
                   Vector3.Distance(actual, secondSlot) < 0.001f;
        }
    }
}
