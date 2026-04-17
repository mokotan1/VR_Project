using UnityEngine;

namespace GlassShards
{
    /// <summary>
    /// Instantiates a <see cref="GlassShardBurst"/> prefab at a world hit and plays it once.
    /// </summary>
    public static class GlassShardBurstSpawner
    {
        /// <param name="prefab">Root must have <see cref="GlassShardBurst"/> (and ParticleSystem).</param>
        /// <param name="worldNormal">Surface normal at the hit; used for orientation.</param>
        public static void Spawn(GameObject prefab, Vector3 position, Vector3 worldNormal)
        {
            if (prefab == null)
            {
                return;
            }

            var n = worldNormal.sqrMagnitude > 1e-8f ? worldNormal.normalized : Vector3.up;
            var instance = Object.Instantiate(prefab, position, Quaternion.LookRotation(n));
            if (instance.TryGetComponent<GlassShardBurst>(out var burst))
            {
                burst.PlayBurst();
            }
        }
    }
}
