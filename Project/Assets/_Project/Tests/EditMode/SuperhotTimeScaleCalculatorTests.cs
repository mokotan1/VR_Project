using NUnit.Framework;
using VRProject.Application.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class SuperhotTimeScaleCalculatorTests
    {
        [Test]
        public void BlendWeightedMotion_HeadOnly_FollowsHead()
        {
            var b = SuperhotTimeScaleCalculator.BlendWeightedMotion(1f, 0f, 1f, 0f);
            Assert.AreEqual(1f, b, 1e-5f);
        }

        [Test]
        public void BlendWeightedMotion_HandOnly_FollowsHand()
        {
            var b = SuperhotTimeScaleCalculator.BlendWeightedMotion(0f, 1f, 0f, 1f);
            Assert.AreEqual(1f, b, 1e-5f);
        }

        [Test]
        public void BlendWeightedMotion_WeightsAverage()
        {
            var b = SuperhotTimeScaleCalculator.BlendWeightedMotion(1f, 0f, 0.8f, 0.2f);
            Assert.AreEqual(1f, b, 1e-5f);
        }

        [Test]
        public void BlendWeightedMotion_ZeroWeights_ReturnsZero()
        {
            var b = SuperhotTimeScaleCalculator.BlendWeightedMotion(0.5f, 0.5f, 0f, 0f);
            Assert.AreEqual(0f, b, 1e-5f);
        }

        [Test]
        public void Motion01FromSpeed_BelowDeadZone_IsZero()
        {
            var m = SuperhotTimeScaleCalculator.Motion01FromSpeed(0.1f, 0.2f, 1f);
            Assert.AreEqual(0f, m, 1e-5f);
        }

        [Test]
        public void Motion01FromSpeed_AtReference_IsOne()
        {
            var m = SuperhotTimeScaleCalculator.Motion01FromSpeed(2f, 0.5f, 2f);
            Assert.AreEqual(1f, m, 1e-4f);
        }

        [Test]
        public void ToTimeFactor_Endpoints()
        {
            Assert.AreEqual(0.05f, SuperhotTimeScaleCalculator.ToTimeFactor(0f, 0.05f, 1f), 1e-5f);
            Assert.AreEqual(1f, SuperhotTimeScaleCalculator.ToTimeFactor(1f, 0.05f, 1f), 1e-5f);
        }

        [Test]
        public void ToTimeFactor_MinZero_StationaryIsZero()
        {
            Assert.AreEqual(0f, SuperhotTimeScaleCalculator.ToTimeFactor(0f, 0f, 1f), 1e-5f);
            Assert.AreEqual(1f, SuperhotTimeScaleCalculator.ToTimeFactor(1f, 0f, 1f), 1e-5f);
        }

        [Test]
        public void EffectivePlanarSpeedForTime_UsesMaxOfVelocityAndIntent()
        {
            var a = SuperhotTimeScaleCalculator.EffectivePlanarSpeedForTime(2f, 0f, 5f);
            Assert.AreEqual(2f, a, 1e-5f);

            var b = SuperhotTimeScaleCalculator.EffectivePlanarSpeedForTime(0f, 1f, 5f);
            Assert.AreEqual(5f, b, 1e-5f);

            var c = SuperhotTimeScaleCalculator.EffectivePlanarSpeedForTime(3f, 0.5f, 5f);
            Assert.AreEqual(3f, c, 1e-5f);
        }

        [Test]
        public void SmoothTowards_Converges()
        {
            var v = 0f;
            var x = 0f;
            for (var i = 0; i < 30; i++)
                x = SuperhotTimeScaleCalculator.SmoothTowards(x, 1f, ref v, 0.1f, 0.016f);
            Assert.Greater(x, 0.99f);
        }
    }
}
