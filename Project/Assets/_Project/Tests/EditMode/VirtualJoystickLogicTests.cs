using NUnit.Framework;
using VRProject.Application.Mobile;

namespace VRProject.Tests.EditMode
{
    public sealed class VirtualJoystickLogicTests
    {
        [Test]
        public void ComputeAxes_AtCenter_ReturnsZero()
        {
            VirtualJoystickLogic.ComputeAxes(100f, 100f, 100f, 100f, 80f, out var x, out var y);
            Assert.That(x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(y, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ComputeAxes_AtEdge_ReturnsUnitAxes()
        {
            VirtualJoystickLogic.ComputeAxes(180f, 100f, 100f, 100f, 80f, out var x, out var y);
            Assert.That(x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(y, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ComputeAxes_BeyondRadius_ClampedToOne()
        {
            VirtualJoystickLogic.ComputeAxes(300f, 100f, 100f, 100f, 80f, out var x, out var y);
            Assert.That(x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(y, Is.EqualTo(0f).Within(1e-4f));
        }
    }
}
