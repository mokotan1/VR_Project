using System;
using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WeaponHitDetector : MonoBehaviour
    {
        public event Action<WeaponHitContext> HitConfirmed;

        [SerializeField] WeaponMotion _motion;
        [SerializeField] WeaponAttackSession _session;
        [SerializeField] WeaponAttackProfile _profile;
        [SerializeField] AudioSource _hitAudio;

        readonly DuplicateHitGuard _duplicateGuard = new DuplicateHitGuard();

        public void BindSetup(WeaponMotion motion, WeaponAttackSession session, WeaponAttackProfile profile)
        {
            if (motion != null)
                _motion = motion;
            if (session != null)
                _session = session;
            if (profile != null)
                _profile = profile;
        }

        void Awake()
        {
            if (_motion == null)
                _motion = GetComponentInParent<WeaponMotion>();
            if (_session == null)
                _session = GetComponentInParent<WeaponAttackSession>();

            var collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            TryHit(other, other.ClosestPoint(transform.position), -transform.forward);
        }

        internal void TryHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_profile == null || _motion == null || _session == null)
                return;

            var zone = HitZone.Resolve(other);
            if (zone == null)
                return;

            var receiver = other.GetComponentInParent<DamageReceiver>();
            if (receiver == null)
                return;

            var motion = _motion.Current;
            var kind = motion.ActiveKind;
            var motionScore = MeleeQualifyingHitCalculator.QualifyingScore(
                motion.LinearSpeedMps,
                _profile.MinLinearSpeed,
                _profile.ReferenceLinearSpeed,
                kind,
                zone.FeedbackMultiplier);
            var impact = AxeImpactPhysicsCalculator.Calculate(
                motion.LinearSpeedMps,
                motion.AngularSpeedDps,
                _profile.MassKg,
                _profile.BladeRadiusMeters,
                _profile.MomentOfInertiaScale,
                _profile.ImpactDurationSeconds,
                _profile.BladeContactAreaSquareMeters,
                _profile.RigidbodyImpulseScale,
                _profile.MaxRigidbodyImpulse);
            var score = AxeImpactPhysicsCalculator.Score(
                motionScore,
                impact,
                _profile.MinImpactEnergyJoules,
                _profile.ReferenceImpactEnergyJoules,
                _profile.ReferencePressurePascals,
                _profile.MotionScoreWeight,
                _profile.EnergyScoreWeight,
                _profile.PressureScoreWeight);

            if (!MeleeHitValidator.IsQualifyingHit(
                    _session.IsActive,
                    score,
                    _profile.MinQualifyingScore,
                    motion.LinearSpeedMps,
                    motion.AngularSpeedDps,
                    _profile.EnterLinearSpeed,
                    _profile.EnterAngularSpeed,
                    kind,
                    _profile.Family))
                return;

            if (zone.Kind == HitZoneKind.Shield)
            {
                var shield = zone.GetComponent<ShieldBlocker>() ?? zone.GetComponentInParent<ShieldBlocker>();
                if (shield != null && shield.TryBlock(motion.SwingDirection, hitPoint, out _))
                {
                    var parry = zone.GetComponent<ParryWindow>() ?? zone.GetComponentInParent<ParryWindow>();
                    parry?.RegisterBlock(Time.time);
                    return;
                }
            }

            var targetId = receiver.GetInstanceID();
            if (!_duplicateGuard.TryRegisterHit(
                    _session.CurrentSessionId,
                    targetId,
                    zone.ZoneId,
                    Time.time,
                    _profile.PerTargetCooldownSeconds))
                return;

            var parryWindow = GetComponentInParent<ParryWindow>();
            parryWindow?.TryConsumeParry(
                Time.time,
                _profile.ParryWindowSeconds,
                score,
                _profile.MinQualifyingScore);

            var context = new WeaponHitContext(
                zone,
                hitPoint,
                hitNormal,
                kind,
                score,
                _session.CurrentSessionId,
                impact,
                motion.SwingDirection,
                _profile.ReferenceImpactEnergyJoules);
            if (receiver.TryReceiveHit(context))
            {
                HitConfirmed?.Invoke(context);
                if (_hitAudio != null)
                    _hitAudio.Play();
            }
        }
    }
}
