using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class OsFpsInspiredWeaponViewmodelOffsetTests
    {
        [Test]
        public void ApplyViewmodelLocalOffsets_AddsPositionAndAppliesEulerAfterBase()
        {
            var basePos = new Vector3(0.1f, -0.2f, 0.3f);
            var baseRot = Quaternion.Euler(2f, 94f, -1.5f);
            var posOff = new Vector3(0.05f, 0.01f, -0.02f);
            var eulerOff = new Vector3(0f, 10f, 0f);

            OsFpsInspiredWeapon.ApplyViewmodelLocalOffsets(
                basePos,
                baseRot,
                posOff,
                eulerOff,
                out var outPos,
                out var outRot);

            Assert.That(outPos, Is.EqualTo(basePos + posOff));

            var expectedRot = baseRot * Quaternion.Euler(eulerOff);
            Assert.That(Quaternion.Angle(outRot, expectedRot), Is.LessThan(0.02f));
        }
    }
}
