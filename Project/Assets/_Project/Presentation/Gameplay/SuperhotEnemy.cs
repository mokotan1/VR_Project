using GlassShards;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemy : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional: glass shard burst at hit point when killed (e.g. GlassShardBurst prefab).")]
        GameObject _glassShardBurstPrefab;

        SuperhotCombatZone _zone;

        void Awake()
        {
            _zone = GetComponentInParent<SuperhotCombatZone>();
        }

        void OnDestroy()
        {
            _zone?.NotifyEnemyDestroyed(this);
        }

        /// <param name="hit">When set (e.g. from hitscan), spawns shard VFX at impact.</param>
        public void Kill(RaycastHit? hit = null)
        {
            if (hit.HasValue && _glassShardBurstPrefab != null)
            {
                GlassShardBurstSpawner.Spawn(_glassShardBurstPrefab, hit.Value.point, hit.Value.normal);
            }

            Destroy(gameObject);
        }
    }
}
