using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class CrystalDefenseAwarenessMathTests
    {
        [Test]
        public void IsVisibleInViewport_WhenInsideAndInFront_ReturnsTrue()
        {
            Assert.IsTrue(CrystalDefenseAwarenessMath.IsVisibleInViewport(new Vector3(0.5f, 0.5f, 3f)));
        }

        [Test]
        public void IsVisibleInViewport_WhenBehindCamera_ReturnsFalse()
        {
            Assert.IsFalse(CrystalDefenseAwarenessMath.IsVisibleInViewport(new Vector3(0.5f, 0.5f, -1f)));
        }

        [Test]
        public void TryGetOffscreenIndicator_RightSide_ClampsToPaddedEdge()
        {
            var ok = CrystalDefenseAwarenessMath.TryGetOffscreenIndicator(
                new Vector3(1.4f, 0.5f, 3f),
                new Vector2(1000f, 600f),
                60f,
                out var anchoredPosition,
                out var angleDegrees);

            Assert.IsTrue(ok);
            Assert.AreEqual(440f, anchoredPosition.x, 0.01f);
            Assert.AreEqual(0f, anchoredPosition.y, 0.01f);
            Assert.AreEqual(-90f, angleDegrees, 0.01f);
        }

        [Test]
        public void Threat01_IsHigherWhenEnemyIsCloser()
        {
            var far = CrystalDefenseAwarenessMath.Threat01(18f, 2f, 20f);
            var close = CrystalDefenseAwarenessMath.Threat01(4f, 2f, 20f);

            Assert.Greater(close, far);
            Assert.AreEqual(1f, CrystalDefenseAwarenessMath.Threat01(1f, 2f, 20f), 0.001f);
        }
    }
}
