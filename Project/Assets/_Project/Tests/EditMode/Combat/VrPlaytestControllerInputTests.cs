using NUnit.Framework;
using UnityEngine.XR;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class VrPlaytestControllerInputTests
    {
        [Test]
        public void TryReadGripHeld_ReturnsFalseWhenNoDeviceAndNoEditorKey()
        {
            Assert.IsTrue(VrPlaytestControllerInput.TryReadGripHeld(
                XRNode.RightHand,
                analogThreshold: 0.55f,
                out var gripHeld));
            Assert.IsFalse(gripHeld);
        }

        [Test]
        public void TryReadTriggerEdge_ReturnsFalseWhenNoDeviceAndNoEditorClick()
        {
            var detector = default(VrTriggerPressDetector);
            Assert.IsTrue(VrPlaytestControllerInput.TryReadTriggerEdge(
                XRNode.RightHand,
                analogThreshold: 0.55f,
                ref detector,
                out var edge));
            Assert.IsFalse(edge);
        }
    }
}
