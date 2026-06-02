using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
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
        public void SnapSceneWeaponsToRightFront_MovesAxeOnly_Hk416UsesGripTriggerFire()
        {
            _xrObject = new GameObject("XR Origin (XR Rig)");
            var origin = _xrObject.AddComponent<XROrigin>();
            _cameraObject = new GameObject("Main Camera");
            _cameraObject.transform.SetParent(_xrObject.transform, false);
            origin.Camera = _cameraObject.AddComponent<Camera>();
            _rightController = new GameObject("Right Controller");
            _rightController.transform.SetParent(_xrObject.transform, false);
            _rightController.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.identity);

            _gun = CreateHk416PickupRoot();
            var gunStartPos = _gun.transform.position;
            _axe = new GameObject("MeleeWeapon_Axe");
            _axe.AddComponent<MeleeWeaponRuntimeBinder>();
            _axe.AddComponent<MeleeWeaponVrGrabLifecycle>();
            _axe.AddComponent<XRGrabInteractable>();
            _axe.AddComponent<WeaponMotionSourceRouter>();
            var flatSource = _axe.AddComponent<FlatMouseWeaponMotionSource>();
            var vrSource = _axe.AddComponent<VrGrabbedWeaponMotionSource>();
            var handle = new GameObject("Handle").transform;
            handle.SetParent(_axe.transform, false);
            handle.localPosition = new Vector3(0f, 0f, -0.18f);
            var tip = new GameObject("BladeTip").transform;
            tip.SetParent(_axe.transform, false);
            tip.localPosition = new Vector3(0f, 0f, 0.55f);

            var snapInput = _xrObject.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(origin);

            snapInput.SnapSceneWeaponsToRightFront();

            Assert.AreEqual(gunStartPos, _gun.transform.position);
            Assert.IsNull(_gun.transform.parent);

            var axeSlot = new Vector3(1.32f, 1.88f, 3.55f);
            Assert.IsTrue(IsInSnapSlot(_axe.transform.position, axeSlot, axeSlot));
            Assert.AreEqual(_rightController.transform, _axe.transform.parent);
            Assert.IsTrue(vrSource.IsActive);
            Assert.IsFalse(flatSource.enabled);

            var bladeWorld = (tip.position - handle.position).normalized;
            Assert.Greater(Vector3.Dot(bladeWorld, origin.Camera.transform.forward), 0.95f);
            Assert.IsNull(_rightController.transform.Find("XR_RuntimeGun"));
        }

        static bool IsInSnapSlot(Vector3 actual, Vector3 firstSlot, Vector3 secondSlot)
        {
            return Vector3.Distance(actual, firstSlot) < 0.001f ||
                   Vector3.Distance(actual, secondSlot) < 0.001f;
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

        [Test]
        public void TryReleaseSnappedWeapons_UnparentsWeaponFromRightController()
        {
            _xrObject = new GameObject("XR Origin (XR Rig)");
            var origin = _xrObject.AddComponent<XROrigin>();
            _rightController = new GameObject("Right Controller");
            _rightController.transform.SetParent(_xrObject.transform, false);

            _axe = new GameObject("MeleeWeapon_Axe");
            _axe.AddComponent<Rigidbody>();
            _axe.AddComponent<XRGrabInteractable>();
            _axe.AddComponent<MeleeWeaponVrGrabLifecycle>();
            _axe.transform.SetParent(_rightController.transform, false);

            var snapInput = _xrObject.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(origin);

            Assert.IsTrue(snapInput.TryReleaseSnappedWeapons());
            Assert.IsNull(_axe.transform.parent);
        }

        [Test]
        public void IsControllerPickupPressed_ReturnsTrueForGripButton()
        {
            Assert.IsTrue(VrSceneWeaponSnapInput.IsControllerPickupPressed(
                triggerButton: false,
                triggerValue: 0f,
                gripButton: true,
                gripValue: 0f,
                analogThreshold: 0.55f));
        }

        [Test]
        public void IsControllerPickupPressed_ReturnsFalseWhenNeitherPressed()
        {
            Assert.IsFalse(VrSceneWeaponSnapInput.IsControllerPickupPressed(
                triggerButton: false,
                triggerValue: 0.1f,
                gripButton: false,
                gripValue: 0.2f,
                analogThreshold: 0.55f));
        }

        [Test]
        public void ResolveRightHandAnchor_FindsChildNamedRightController()
        {
            _xrObject = new GameObject("XR Origin (XR Rig)");
            var origin = _xrObject.AddComponent<XROrigin>();
            _rightController = new GameObject("Right Controller");
            _rightController.transform.SetParent(_xrObject.transform, false);

            var snapInput = _xrObject.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(origin);

            Assert.AreEqual(_rightController.transform, snapInput.ResolveRightHandAnchor());
            Object.DestroyImmediate(_xrObject);
        }
    }
}
