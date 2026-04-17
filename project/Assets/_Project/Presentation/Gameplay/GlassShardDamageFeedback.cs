using GlassShards;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// On each damage event from <see cref="OsFpsInspiredDamageable"/>, spawns a glass shard burst at the reported hit point.
    /// Add to the same GameObject as the damageable (e.g. prototype NavMesh enemy).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OsFpsInspiredDamageable))]
    public sealed class GlassShardDamageFeedback : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("e.g. Assets/GlassShards/Prefabs/GlassShardBurst")]
        GameObject _glassShardBurstPrefab;

        OsFpsInspiredDamageable _health;

        void Awake()
        {
            _health = GetComponent<OsFpsInspiredDamageable>();
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
            }
        }

        void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
            }
        }

        void OnDamaged(float amount, Vector3 hitPoint)
        {
            if (_glassShardBurstPrefab == null || amount <= 0f)
            {
                return;
            }

            var outward = (hitPoint - transform.position).normalized;
            if (outward.sqrMagnitude < 1e-6f)
            {
                outward = Vector3.up;
            }

            GlassShardBurstSpawner.Spawn(_glassShardBurstPrefab, hitPoint, outward);
        }
    }
}
