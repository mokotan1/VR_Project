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
            // (1*0.8 + 0*0.2) / (0.8+0.2) = 0.8
            var b = SuperhotTimeScaleCalculator.BlendWeightedMotion(1f, 0f, 0.8f, 0.2f);
            Assert.AreEqual(0.8f, b, 1e-5f);
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

        [Test]
        public void AngularSpeedDegreesPerSecond_90DegreesInOneSecond_Is90()
        {
            var w = SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(90f, 1f);
            Assert.AreEqual(90f, w, 1e-5f);
        }

        [Test]
        public void AngularSpeedDegreesPerSecond_ZeroDt_IsZero()
        {
            var w = SuperhotTimeScaleCalculator.AngularSpeedDegreesPerSecond(90f, 0f);
            Assert.AreEqual(0f, w, 1e-5f);
        }

        [Test]
        public void TrackerIntensity_CombinesLinearAndAngularWeighted()
        {
            var i = SuperhotTimeScaleCalculator.TrackerIntensity(1f, 60f, 0.01f);
            Assert.AreEqual(1.6f, i, 1e-5f);
        }

        [Test]
        public void RootSumSquareThree_UnitAxes_IsSqrt3()
        {
            var r = SuperhotTimeScaleCalculator.RootSumSquareThree(1f, 1f, 1f);
            Assert.AreEqual(1.7320508f, r, 1e-4f);
        }

        [Test]
        public void RootSumSquareThree_SingleNonZero_MatchesAbs()
        {
            var r = SuperhotTimeScaleCalculator.RootSumSquareThree(2f, 0f, 0f);
            Assert.AreEqual(2f, r, 1e-5f);
        }

        [Test]
        public void SmoothTimeScaleMoveTowards_ReachesTargetWithinOneStep()
        {
            var x = SuperhotTimeScaleCalculator.SmoothTimeScaleMoveTowards(0f, 1f, 10f, 0.2f);
            Assert.AreEqual(1f, x, 1e-5f);
        }

        [Test]
        public void SmoothTimeScaleMoveTowards_ClimbsGradually()
        {
            var x = SuperhotTimeScaleCalculator.SmoothTimeScaleMoveTowards(0f, 1f, 10f, 0.05f);
            Assert.AreEqual(0.5f, x, 1e-5f);
        }

        [Test]
        public void SmoothTimeScaleLerp_Halfway()
        {
            var x = SuperhotTimeScaleCalculator.SmoothTimeScaleLerp(0f, 1f, 0.5f);
            Assert.AreEqual(0.5f, x, 1e-5f);
        }

        [Test]
        public void DesktopAdditive_Idle_ReturnsMin()
        {
            var t = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(0.05f, 1f, 0f, 0f, 1f, 0.25f);
            Assert.AreEqual(0.05f, t, 1e-5f);
        }

        [Test]
        public void DesktopAdditive_FullMove_ReachesMax()
        {
            var t = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(0.05f, 1f, 1f, 0f, 1f, 0.25f);
            Assert.AreEqual(1f, t, 1e-4f);
        }

        [Test]
        public void DesktopAdditive_OnlyLook_ScalesByLookWeight()
        {
            var t = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(0.05f, 1f, 0f, 1f, 1f, 0.25f);
            Assert.AreEqual(0.05f + 0.95f * 0.25f, t, 1e-4f);
        }

        [Test]
        public void DesktopAdditive_MoveAndLook_ClampCombinedToOne()
        {
            var t = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(0.05f, 1f, 1f, 1f, 1f, 0.25f);
            Assert.AreEqual(1f, t, 1e-4f);
        }

        [Test]
        public void DesktopMaxBlended_MatchesAdditive_ForNonNegativeWeights()
        {
            const float min = 0.05f;
            const float max = 1f;
            var moves = new[] { 0f, 0.4f, 1f };
            var looks = new[] { 0f, 0.6f, 1f };
            var moveWs = new[] { 0.5f, 1f };
            var lookWs = new[] { 0.12f, 0.25f };
            foreach (var m in moves)
            foreach (var l in looks)
            foreach (var mw in moveWs)
            foreach (var lw in lookWs)
            {
                var add = SuperhotTimeScaleCalculator.DesktopAdditiveTargetTimeScale(min, max, m, l, mw, lw);
                var maxB = SuperhotTimeScaleCalculator.DesktopMaxBlendedTargetTimeScale(min, max, m, l, mw, lw);
                Assert.AreEqual(add, maxB, 1e-4f, $"m={m} l={l} mw={mw} lw={lw}");
            }
        }
    }
}
