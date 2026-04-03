using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode
{
    public sealed class LocomotionInputMapperTests
    {
        [Test]
        public void ToUnityChanAxes_Diagonal_IsUnitLength()
        {
            var r = LocomotionInputMapper.ToUnityChanAxes(new Vector3(1f, 0f, 1f));
            Assert.That(new Vector2(r.x, r.y).magnitude, Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void ToUnityChanAxes_AlreadySmall_Unchanged()
        {
            var r = LocomotionInputMapper.ToUnityChanAxes(new Vector3(0.3f, 0f, 0.4f));
            Assert.That(r.x, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(r.y, Is.EqualTo(0.4f).Within(0.001f));
        }

        [Test]
        public void ToUnityChanAxes_Forward_IsPositiveY()
        {
            var r = LocomotionInputMapper.ToUnityChanAxes(new Vector3(0f, 0f, 1f));
            Assert.That(r.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(r.x, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
