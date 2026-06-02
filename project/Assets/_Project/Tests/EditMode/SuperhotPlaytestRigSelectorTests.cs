using System.Reflection;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEngine;
using VRProject.Presentation.Combat;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode
{
    public sealed class SuperhotPlaytestRigSelectorTests
    {
        GameObject _selectorObject;
        GameObject _xrObject;
        GameObject _rightController;
        GameObject _cameraObject;
        GameObject _legacyPlayer;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_selectorObject);
            Object.DestroyImmediate(_xrObject);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_legacyPlayer);
        }

        [Test]
        public void ApplyRigSelection_WhenUsingVr_ReplacesLegacyPlayerWithXrOrigin()
        {
            _selectorObject = new GameObject("Systems");
            var selector = _selectorObject.AddComponent<SuperhotPlaytestRigSelector>();

            _xrObject = new GameObject("XR Origin (XR Rig)");
            var origin = _xrObject.AddComponent<XROrigin>();
            _xrObject.tag = "Untagged";
            _cameraObject = new GameObject("Main Camera");
            _cameraObject.transform.SetParent(_xrObject.transform, false);
            _cameraObject.AddComponent<Camera>();
            origin.Camera = _cameraObject.GetComponent<Camera>();
            _rightController = new GameObject("Right Controller");
            _rightController.transform.SetParent(_xrObject.transform, false);

            _legacyPlayer = new GameObject("UnityChan_Player");
            _legacyPlayer.tag = "Player";

            InvokeApplyRigSelection(selector, useXr: true);

            Assert.IsTrue(_xrObject.activeSelf);
            Assert.AreEqual("Player", _xrObject.tag);
            Assert.NotNull(_xrObject.GetComponent<SuperhotPlaytestPlayerHealth>());
            Assert.NotNull(_xrObject.GetComponent<PrototypeFpsPlayerHealth>());
            Assert.NotNull(_xrObject.GetComponent<VrSceneWeaponSnapInput>());
            Assert.NotNull(_xrObject.GetComponent<PlaytestPlayerContactVolume>());
            Assert.IsNull(_rightController.transform.Find("XR_RuntimeGun"));
            Assert.IsFalse(_legacyPlayer.activeSelf);
        }

        static void InvokeApplyRigSelection(SuperhotPlaytestRigSelector selector, bool useXr)
        {
            var method = typeof(SuperhotPlaytestRigSelector).GetMethod(
                "ApplyRigSelection",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(method);
            method.Invoke(selector, new object[] { useXr });
        }
    }
}
