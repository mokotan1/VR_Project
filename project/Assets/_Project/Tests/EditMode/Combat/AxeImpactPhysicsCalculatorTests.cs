using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class AxeImpactPhysicsCalculatorTests
    {
        [Test]
        public void Calculate_ComputesEnergyForcePressureAndImpulse()
        {
            var result = AxeImpactPhysicsCalculator.Calculate(
                tipSpeedMetersPerSecond: 4f,
                angularSpeedDegreesPerSecond: 180f,
                massKg: 2f,
                bladeRadiusMeters: 0.75f,
                inertiaScale: 1f,
                impactDurationSeconds: 0.02f,
                bladeContactAreaSquareMeters: 0.0004f,
                impulseScale: 0.5f,
                maxImpulseNewtonSeconds: 20f);

            Assert.AreEqual(16f, result.LinearEnergyJoules, 1e-4f);
            Assert.AreEqual(5.5517f, result.RotationalEnergyJoules, 1e-4f);
            Assert.AreEqual(8f, result.MomentumKgMetersPerSecond, 1e-4f);
            Assert.AreEqual(400f, result.ImpactForceNewtons, 1e-4f);
            Assert.AreEqual(1000000f, result.PressurePascals, 1e-1f);
            Assert.AreEqual(4f, result.ImpulseNewtonSeconds, 1e-4f);
        }

        [Test]
        public void Score_CombinesMotionEnergyAndPressureIntoClampedValue()
        {
            var result = AxeImpactPhysicsCalculator.Calculate(
                tipSpeedMetersPerSecond: 3f,
                angularSpeedDegreesPerSecond: 0f,
                massKg: 2f,
                bladeRadiusMeters: 0.5f,
                inertiaScale: 1f,
                impactDurationSeconds: 0.03f,
                bladeContactAreaSquareMeters: 0.001f,
                impulseScale: 1f,
                maxImpulseNewtonSeconds: 100f);

            var score = AxeImpactPhysicsCalculator.Score(
                existingMotionScore: 0.5f,
                result,
                minImpactEnergyJoules: 4f,
                referenceImpactEnergyJoules: 24f,
                referencePressurePascals: 600000f,
                motionWeight: 0.2f,
                energyWeight: 0.5f,
                pressureWeight: 0.3f);

            Assert.AreEqual(0.425f, score, 1e-4f);
        }
    }
}
