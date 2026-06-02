using System.Collections;
using GlassShards;
using UnityEngine;
using UnityEngine.AI;
using VRProject.Application.Combat;
using VRProject.Presentation.Combat;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemy : MonoBehaviour
    {
        const string GlassShardBurstPrefabPath = "Assets/GlassShards/Prefabs/GlassShardBurst.prefab";

        [SerializeField]
        [Tooltip("Optional: glass shard burst at hit point when killed (e.g. GlassShardBurst prefab).")]
        GameObject _glassShardBurstPrefab;

        SuperhotCombatZone _zone;
        bool _killStarted;

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
            if (hit.HasValue)
                Kill(hit.Value.point, hit.Value.normal);
            else
                Destroy(gameObject);
        }

        public void Kill(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }

            if (_killStarted)
                return;

            _killStarted = true;
            DisableEnemyControl();
            StartCoroutine(KillRoutine(hitPoint, hitNormal, null));
        }

        public void Kill(WeaponHitContext context)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }

            if (_killStarted)
                return;

            _killStarted = true;
            DisableEnemyControl();
            StartCoroutine(KillRoutine(context.HitPoint, context.HitNormal, context));
        }

        IEnumerator KillRoutine(Vector3 hitPoint, Vector3 hitNormal, WeaponHitContext? meleeContext)
        {
            if (_glassShardBurstPrefab != null)
                GlassShardBurstSpawner.Spawn(_glassShardBurstPrefab, hitPoint, hitNormal);

            var tint = GetComponent<EnemyHitColorTint>();
            if (tint == null)
                tint = gameObject.AddComponent<EnemyHitColorTint>();

            tint.ApplyTintImmediate();
            yield return new WaitForSeconds(Mathf.Max(0.02f, tint.KillFlashDuration));

            if (meleeContext.HasValue)
            {
                var context = meleeContext.Value;
                var demolish = GetComponent<EnemyPoseDemolishOnDeath>();
                if (demolish != null &&
                    demolish.TryDemolishFromMeleeHit(
                        hitPoint,
                        context.ImpulseDirection,
                        context.Physics.ImpulseNewtonSeconds))
                {
                    Destroy(gameObject, demolish.FragmentLifetimeSeconds + 0.35f);
                    yield break;
                }
            }

            Destroy(gameObject);
        }

        void DisableEnemyControl()
        {
            foreach (var brain in GetComponents<SuperhotEnemyBrain>())
                brain.enabled = false;

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Assign Default Glass Shard Burst Prefab")]
        void AssignDefaultGlassShardBurstPrefab()
        {
            if (_glassShardBurstPrefab != null)
                return;

            _glassShardBurstPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GlassShardBurstPrefabPath);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
