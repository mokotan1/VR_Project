using System;

namespace VRProject.Application.Combat
{
    public readonly struct AxeImpactPhysicsResult
    {
        public AxeImpactPhysicsResult(
            float linearEnergyJoules,
            float rotationalEnergyJoules,
            float momentumKgMetersPerSecond,
            float impactForceNewtons,
            float pressurePascals,
            float impulseNewtonSeconds)
        {
            LinearEnergyJoules = linearEnergyJoules;
            RotationalEnergyJoules = rotationalEnergyJoules;
            MomentumKgMetersPerSecond = momentumKgMetersPerSecond;
            ImpactForceNewtons = impactForceNewtons;
            PressurePascals = pressurePascals;
            ImpulseNewtonSeconds = impulseNewtonSeconds;
        }

        public float LinearEnergyJoules { get; }
        public float RotationalEnergyJoules { get; }
        public float TotalEnergyJoules => LinearEnergyJoules + RotationalEnergyJoules;
        public float MomentumKgMetersPerSecond { get; }
        public float ImpactForceNewtons { get; }
        public float PressurePascals { get; }
        public float ImpulseNewtonSeconds { get; }
    }

    public static class AxeImpactPhysicsCalculator
    {
        const float DegreesToRadians = (float)(Math.PI / 180.0);

        public static AxeImpactPhysicsResult Calculate(
            float tipSpeedMetersPerSecond,
            float angularSpeedDegreesPerSecond,
            float massKg,
            float bladeRadiusMeters,
            float inertiaScale,
            float impactDurationSeconds,
            float bladeContactAreaSquareMeters,
            float impulseScale,
            float maxImpulseNewtonSeconds)
        {
            var speed = Math.Max(0f, tipSpeedMetersPerSecond);
            var angularSpeedRad = Math.Max(0f, angularSpeedDegreesPerSecond) * DegreesToRadians;
            var mass = Math.Max(0f, massKg);
            var radius = Math.Max(0f, bladeRadiusMeters);
            var inertia = mass * radius * radius * Math.Max(0f, inertiaScale);

            var linearEnergy = 0.5f * mass * speed * speed;
            var rotationalEnergy = 0.5f * inertia * angularSpeedRad * angularSpeedRad;
            var momentum = mass * speed;

            var force = impactDurationSeconds > 1e-5f ? momentum / impactDurationSeconds : 0f;
            var pressure = bladeContactAreaSquareMeters > 1e-7f ? force / bladeContactAreaSquareMeters : 0f;
            var impulse = Clamp(momentum * Math.Max(0f, impulseScale), 0f, Math.Max(0f, maxImpulseNewtonSeconds));

            return new AxeImpactPhysicsResult(linearEnergy, rotationalEnergy, momentum, force, pressure, impulse);
        }

        public static float Score(
            float existingMotionScore,
            AxeImpactPhysicsResult result,
            float minImpactEnergyJoules,
            float referenceImpactEnergyJoules,
            float referencePressurePascals,
            float motionWeight,
            float energyWeight,
            float pressureWeight)
        {
            var energy01 = Motion01FromRange(result.TotalEnergyJoules, minImpactEnergyJoules, referenceImpactEnergyJoules);
            var pressure01 = referencePressurePascals > 1e-5f
                ? Clamp01(result.PressurePascals / referencePressurePascals)
                : 0f;

            var weighted =
                Clamp01(existingMotionScore) * Math.Max(0f, motionWeight) +
                energy01 * Math.Max(0f, energyWeight) +
                pressure01 * Math.Max(0f, pressureWeight);

            return Clamp01(weighted);
        }

        static float Motion01FromRange(float value, float minimum, float reference)
        {
            if (value <= minimum || reference <= minimum)
                return 0f;

            return Clamp01((value - minimum) / (reference - minimum));
        }

        static float Clamp01(float value) => Clamp(value, 0f, 1f);

        static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }

    public readonly struct MeleeBladeTipHitColliderSpec
    {
        public MeleeBladeTipHitColliderSpec(CombatVector3 localCenter, CombatVector3 localSize)
        {
            LocalCenter = localCenter;
            LocalSize = localSize;
        }

        public CombatVector3 LocalCenter { get; }
        public CombatVector3 LocalSize { get; }
    }

    public static class MeleeHitColliderLayout
    {
        public const float DefaultBladeLengthMeters = 0.18f;
        public const float DefaultBladeWidthMeters = 0.08f;
        public const float DefaultBladeThicknessMeters = 0.05f;
        const float CenterBackFromTipFactor = 0.35f;

        public static MeleeBladeTipHitColliderSpec BuildBladeTipSpec(
            CombatVector3 tipLocalPosition,
            CombatVector3 bladeForwardLocal,
            float bladeLengthMeters = DefaultBladeLengthMeters,
            float bladeWidthMeters = DefaultBladeWidthMeters,
            float bladeThicknessMeters = DefaultBladeThicknessMeters)
        {
            var forward = bladeForwardLocal.Normalized;
            if (forward.Magnitude <= 1e-6f)
                forward = new CombatVector3(0f, 0f, 1f);

            var backOffset = bladeLengthMeters * CenterBackFromTipFactor;
            var center = new CombatVector3(
                tipLocalPosition.X - forward.X * backOffset,
                tipLocalPosition.Y - forward.Y * backOffset,
                tipLocalPosition.Z - forward.Z * backOffset);

            var size = new CombatVector3(bladeWidthMeters, bladeThicknessMeters, bladeLengthMeters);
            return new MeleeBladeTipHitColliderSpec(center, size);
        }
    }

    public static class MeleeImpactFeedbackCalculator
    {
        public static float Intensity(
            float qualifyingScore,
            AxeImpactPhysicsResult physics,
            float referenceEnergyJoules)
        {
            var score01 = Clamp01(qualifyingScore);
            var energy01 = referenceEnergyJoules > 1e-5f
                ? Clamp01(physics.TotalEnergyJoules / referenceEnergyJoules)
                : 0f;

            return Clamp01(Math.Max(0.25f, score01 * 0.65f + energy01 * 0.35f));
        }

        public static int ParticleCount(float intensity, int minParticles, int maxParticles)
        {
            minParticles = Math.Max(0, minParticles);
            maxParticles = Math.Max(minParticles, maxParticles);
            var count = minParticles + (int)Math.Round((maxParticles - minParticles) * Clamp01(intensity));
            return Math.Max(minParticles, Math.Min(maxParticles, count));
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
