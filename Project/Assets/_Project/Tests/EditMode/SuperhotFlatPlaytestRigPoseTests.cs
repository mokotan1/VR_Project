using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class SuperhotFlatPlaytestRigPoseTests
    {
        [Test]
        public void DecomposeYawPitchDegrees_ForwardAlongWorldZ_YieldsZeroYawZeroPitch()
        {
            SuperhotFlatPlaytestRigPose.DecomposeYawPitchDegrees(Vector3.forward, out var yaw, out var pitch);
            Assert.That(yaw, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pitch, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DecomposeYawPitchDegrees_ForwardAlongWorldX_YieldsPositiveYaw()
        {
            SuperhotFlatPlaytestRigPose.DecomposeYawPitchDegrees(Vector3.right, out var yaw, out var pitch);
            Assert.That(yaw, Is.EqualTo(90f).Within(0.01f));
            Assert.That(pitch, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void DecomposeYawPitchDegrees_LookingUp_YieldsNegativePitch()
        {
            var upish = new Vector3(0f, 1f, 1f).normalized;
            SuperhotFlatPlaytestRigPose.DecomposeYawPitchDegrees(upish, out _, out var pitch);
            Assert.That(pitch, Is.LessThan(0f));
        }
    }
}
