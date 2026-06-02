using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class MeleeImpactFeedbackCalculatorTests
    {
        [Test]
        public void Intensity_UsesScoreAndEnergyWithMinimumVisibleFeedback()
        {
            var physics = new AxeImpactPhysicsResult(
                linearEnergyJoules: 4f,
                rotationalEnergyJoules: 4f,
                momentumKgMetersPerSecond: 3f,
                impactForceNewtons: 120f,
                pressurePascals: 250000f,
                impulseNewtonSeconds: 2f);

            var intensity = MeleeImpactFeedbackCalculator.Intensity(
                qualifyingScore: 0.2f,
                physics,
                referenceEnergyJoules: 32f);

            Assert.AreEqual(0.25f, intensity, 1e-4f);
        }

        [Test]
        public void ParticleCount_StaysSmallForMobile()
        {
            Assert.AreEqual(4, MeleeImpactFeedbackCalculator.ParticleCount(0f, 4, 14));
            Assert.AreEqual(9, MeleeImpactFeedbackCalculator.ParticleCount(0.5f, 4, 14));
            Assert.AreEqual(14, MeleeImpactFeedbackCalculator.ParticleCount(1f, 4, 14));
        }
    }
}
