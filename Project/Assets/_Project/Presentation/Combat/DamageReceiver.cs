using System;
using UnityEngine;
using VRProject.Application.Combat;
using VRProject.Presentation.Gameplay;

namespace VRProject.Presentation.Combat
{
    public readonly struct WeaponHitContext
    {
        public WeaponHitContext(
            HitZone zone,
            Vector3 hitPoint,
            Vector3 hitNormal,
            WeaponAttackKind kind,
            float qualifyingScore,
            int sessionId)
        {
            Zone = zone;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Kind = kind;
            QualifyingScore = qualifyingScore;
            SessionId = sessionId;
        }

        public HitZone Zone { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public WeaponAttackKind Kind { get; }
        public float QualifyingScore { get; }
        public int SessionId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DamageReceiver : MonoBehaviour
    {
        public event Action<WeaponHitContext> HitConfirmed;

        [SerializeField] SuperhotEnemy _enemy;

        void Awake()
        {
            if (_enemy == null)
                _enemy = GetComponent<SuperhotEnemy>();
        }

        public bool TryReceiveHit(WeaponHitContext context)
        {
            if (context.Zone == null)
                return false;

            HitConfirmed?.Invoke(context);

            if (_enemy != null)
            {
                _enemy.Kill(context.HitPoint, context.HitNormal);
                return true;
            }

            return false;
        }
    }
}
