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
            : this(zone, hitPoint, hitNormal, kind, qualifyingScore, sessionId, default, Vector3.zero)
        {
        }

        public WeaponHitContext(
            HitZone zone,
            Vector3 hitPoint,
            Vector3 hitNormal,
            WeaponAttackKind kind,
            float qualifyingScore,
            int sessionId,
            AxeImpactPhysicsResult physics,
            Vector3 impulseDirection)
            : this(zone, hitPoint, hitNormal, kind, qualifyingScore, sessionId, physics, impulseDirection, 32f)
        {
        }

        public WeaponHitContext(
            HitZone zone,
            Vector3 hitPoint,
            Vector3 hitNormal,
            WeaponAttackKind kind,
            float qualifyingScore,
            int sessionId,
            AxeImpactPhysicsResult physics,
            Vector3 impulseDirection,
            float feedbackReferenceEnergyJoules)
        {
            Zone = zone;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Kind = kind;
            QualifyingScore = qualifyingScore;
            SessionId = sessionId;
            Physics = physics;
            ImpulseDirection = impulseDirection;
            FeedbackReferenceEnergyJoules = feedbackReferenceEnergyJoules;
        }

        public HitZone Zone { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public WeaponAttackKind Kind { get; }
        public float QualifyingScore { get; }
        public int SessionId { get; }
        public AxeImpactPhysicsResult Physics { get; }
        public Vector3 ImpulseDirection { get; }
        public float FeedbackReferenceEnergyJoules { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DamageReceiver : MonoBehaviour
    {
        public event Action<WeaponHitContext> HitConfirmed;

        [SerializeField] SuperhotEnemy _enemy;
        [SerializeField] Rigidbody _body;

        void Awake()
        {
            if (_enemy == null)
                _enemy = GetComponent<SuperhotEnemy>();
            if (_body == null)
                _body = GetComponentInParent<Rigidbody>();
        }

        public bool TryReceiveHit(WeaponHitContext context)
        {
            if (context.Zone == null)
                return false;

            HitConfirmed?.Invoke(context);
            MeleeImpactFeedback.SpawnDefault(context, context.FeedbackReferenceEnergyJoules);
            ApplyImpactImpulse(context);

            if (_enemy != null)
            {
                _enemy.Kill(context);
                return true;
            }

            return _body != null && context.Physics.ImpulseNewtonSeconds > 0f;
        }

        void ApplyImpactImpulse(WeaponHitContext context)
        {
            if (_body == null || _body.isKinematic || context.Physics.ImpulseNewtonSeconds <= 0f)
                return;

            var direction = context.ImpulseDirection.sqrMagnitude > 1e-6f
                ? context.ImpulseDirection.normalized
                : -context.HitNormal.normalized;

            if (direction.sqrMagnitude <= 1e-6f)
                return;

            _body.AddForceAtPosition(
                direction * context.Physics.ImpulseNewtonSeconds,
                context.HitPoint,
                ForceMode.Impulse);
        }
    }

    static class MeleeImpactFeedback
    {
        const int MinParticles = 4;
        const int MaxParticles = 14;
        const float LifetimeSeconds = 0.42f;

        static Material _flashMaterial;
        static Material _shardMaterial;

        public static void SpawnDefault(WeaponHitContext context, float referenceEnergyJoules)
        {
            if (!UnityEngine.Application.isPlaying || context.Zone == null)
                return;

            var intensity = MeleeImpactFeedbackCalculator.Intensity(
                context.QualifyingScore,
                context.Physics,
                referenceEnergyJoules);
            var count = MeleeImpactFeedbackCalculator.ParticleCount(intensity, MinParticles, MaxParticles);
            var root = new GameObject("MeleeImpactFeedback");
            root.transform.position = context.HitPoint;

            SpawnFlash(root.transform, context.HitNormal, intensity);
            SpawnShardBurst(root.transform, context, count, intensity);
            UnityEngine.Object.Destroy(root, LifetimeSeconds);
        }

        static void SpawnFlash(Transform parent, Vector3 normal, float intensity)
        {
            var flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flash.name = "ImpactFlash";
            flash.transform.SetParent(parent, false);
            flash.transform.localPosition = Vector3.zero;
            var n = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;
            flash.transform.rotation = Quaternion.LookRotation(n);
            var size = Mathf.Lerp(0.08f, 0.16f, intensity);
            flash.transform.localScale = new Vector3(size, size, size);

            var collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = FlashMaterial();
        }

        static void SpawnShardBurst(Transform parent, WeaponHitContext context, int count, float intensity)
        {
            var baseDirection = context.ImpulseDirection.sqrMagnitude > 1e-6f
                ? context.ImpulseDirection.normalized
                : -context.HitNormal.normalized;
            if (baseDirection.sqrMagnitude <= 1e-6f)
                baseDirection = Vector3.up;

            var tangent = Vector3.Cross(baseDirection, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(baseDirection, Vector3.right);
            tangent.Normalize();
            var bitangent = Vector3.Cross(baseDirection, tangent).normalized;

            for (var i = 0; i < count; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "ImpactShard";
                shard.transform.SetParent(parent, false);
                shard.transform.localPosition = Vector3.zero;
                var scale = Mathf.Lerp(0.018f, 0.032f, intensity);
                shard.transform.localScale = new Vector3(scale, scale * 0.45f, scale * 1.8f);

                var collider = shard.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    UnityEngine.Object.Destroy(collider);
                }

                var renderer = shard.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = ShardMaterial();

                var rb = shard.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 0.02f;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

                var angle = i * 137.50777f * Mathf.Deg2Rad;
                var spread = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
                var direction = (baseDirection * 0.75f + spread * 0.45f).normalized;
                rb.AddForce(direction * Mathf.Lerp(0.45f, 1.25f, intensity), ForceMode.Impulse);
                rb.AddTorque(spread * Mathf.Lerp(0.08f, 0.22f, intensity), ForceMode.Impulse);
            }
        }

        static Material FlashMaterial()
        {
            if (_flashMaterial != null)
                return _flashMaterial;

            _flashMaterial = CreateMaterial(new Color(1f, 0.85f, 0.25f, 0.85f));
            return _flashMaterial;
        }

        static Material ShardMaterial()
        {
            if (_shardMaterial != null)
                return _shardMaterial;

            _shardMaterial = CreateMaterial(new Color(1f, 0.18f, 0.08f, 0.9f));
            return _shardMaterial;
        }

        static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
            if (shader == null)
                return null;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }
    }
}
