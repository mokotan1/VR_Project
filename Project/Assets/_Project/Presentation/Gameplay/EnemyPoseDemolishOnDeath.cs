using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OsFpsInspiredDamageable))]
    public sealed class EnemyPoseDemolishOnDeath : MonoBehaviour
    {
        [SerializeField] Material _interiorMaterial;
        [SerializeField, Range(4, 48)] int _breakPointCount = 14;
        [SerializeField, Range(0.03f, 1.5f)] float _breakRadius = 0.35f;
        [SerializeField, Range(0f, 20f)] float _fragmentImpulse = 7.5f;
        [SerializeField, Range(0f, 20f)] float _fragmentTorque = 4f;
        [SerializeField, Range(0f, 30f)] float _fragmentLifetimeSeconds = 8f;
        [SerializeField] bool _useRuntimeMeshDemolisher;
        [SerializeField, Range(1, 16)] int _lowPolyShardCount = 10;
        [SerializeField, Range(0.05f, 2f)] float _lowPolyShardScale = 0.75f;
        [SerializeField, Range(0.25f, 3f)] float _lowPolyShardSpreadMultiplier = 1.6f;
        [SerializeField] bool _lowPolyShardsUseBoxColliders;

        OsFpsInspiredDamageable _damageable;
        Vector3 _lastHitPoint;
        bool _hasHitPoint;
        bool _demolished;
        bool _subscribed;
        Func<GameObject, Vector3, bool> _fragmentFactoryOverride;

        void Awake()
        {
            _damageable = GetComponent<OsFpsInspiredDamageable>();
        }

        void OnEnable()
        {
            SubscribeToDamageable();
        }

        void OnDisable()
        {
            if (_damageable != null && _subscribed)
                _damageable.Damaged -= OnDamaged;
            _subscribed = false;
        }

        public void SetFragmentFactoryForTests(Func<GameObject, Vector3, bool> factory)
        {
            _fragmentFactoryOverride = factory;
            SubscribeToDamageable();
        }

        void SubscribeToDamageable()
        {
            if (_subscribed)
                return;

            if (_damageable == null)
                _damageable = GetComponent<OsFpsInspiredDamageable>();
            if (_damageable == null)
                return;

            _damageable.Damaged += OnDamaged;
            _subscribed = true;
        }

        public static Vector3[] BuildBreakPointPositions(Bounds bounds, Vector3 hitPoint, int count, float radius)
        {
            count = Mathf.Max(1, count);
            radius = Mathf.Max(0.001f, radius);

            var points = new Vector3[count];
            points[0] = hitPoint;

            var inward = bounds.center - hitPoint;
            if (inward.sqrMagnitude < 1e-6f)
                inward = Vector3.up;
            inward.Normalize();

            var tangent = Vector3.Cross(inward, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(inward, Vector3.right);
            tangent.Normalize();
            var bitangent = Vector3.Cross(inward, tangent).normalized;

            for (var i = 1; i < count; i++)
            {
                var t = (float)i / Mathf.Max(1, count - 1);
                var angle = i * 137.50777f * Mathf.Deg2Rad;
                var ring = Mathf.Sqrt(t) * radius;
                var depth = Mathf.Lerp(radius * 0.15f, radius, t);
                points[i] = hitPoint
                            + inward * depth
                            + tangent * (Mathf.Cos(angle) * ring)
                            + bitangent * (Mathf.Sin(angle) * ring);
            }

            return points;
        }

        void OnDamaged(float amount, Vector3 hitPoint)
        {
            if (amount <= 0f || _demolished)
                return;

            _lastHitPoint = hitPoint;
            _hasHitPoint = true;

            if (_damageable != null && _damageable.Health <= 0f)
                DemolishFromLastHit();
        }

        void DemolishFromLastHit()
        {
            if (_demolished)
                return;

            _demolished = true;
            var hitPoint = _hasHitPoint ? _lastHitPoint : transform.position;
            if (_fragmentFactoryOverride != null)
            {
                _fragmentFactoryOverride(gameObject, hitPoint);
                return;
            }

            if (!_useRuntimeMeshDemolisher)
            {
                DemolishWithLowPolyShardBurst(hitPoint);
                return;
            }

            if (!TryCreateSourceMeshObject(out var source, out var sourceRenderer, out var ownsSourceMesh))
                return;

            try
            {
                var sourceBounds = sourceRenderer != null ? sourceRenderer.bounds : new Bounds(transform.position, Vector3.one);
                var breakPoints = CreateBreakPointTransforms(sourceBounds, hitPoint);
                var material = ResolveInteriorMaterial(sourceRenderer);

                try
                {
                    if (!TryDemolish(source, breakPoints, material, out var fragments))
                        return;

                    var parent = new GameObject($"{name}_DemolishedFragments");
                    parent.transform.SetPositionAndRotation(transform.position, transform.rotation);

                    foreach (var fragment in fragments)
                    {
                        fragment.transform.SetParent(parent.transform, true);
                        AddFragmentPhysics(fragment, hitPoint);
                        if (_fragmentLifetimeSeconds > 0f)
                            Destroy(fragment, _fragmentLifetimeSeconds);
                    }

                    HideOriginalEnemy();
                    if (_fragmentLifetimeSeconds > 0f)
                        Destroy(parent, _fragmentLifetimeSeconds + 0.25f);
                }
                finally
                {
                    foreach (var point in breakPoints)
                    {
                        if (point != null)
                            Destroy(point.gameObject);
                    }
                }
            }
            finally
            {
                if (ownsSourceMesh)
                {
                    var ownedMesh = source != null ? source.GetComponent<MeshFilter>()?.sharedMesh : null;
                    if (ownedMesh != null)
                        Destroy(ownedMesh);
                }
                Destroy(source);
            }
        }

        void DemolishWithLowPolyShardBurst(Vector3 hitPoint)
        {
            var sourceBounds = TryGetVisualBounds(out var sourceRenderer, out var visualBounds)
                ? visualBounds
                : new Bounds(transform.position, Vector3.one);
            var material = ResolveInteriorMaterial(sourceRenderer);
            var mobileDefaults = EnemyLowPolyShardBurst.Settings.MobileDefault;
            var settings = new EnemyLowPolyShardBurst.Settings(
                _lowPolyShardCount,
                _lowPolyShardScale,
                Mathf.Max(_fragmentImpulse, mobileDefaults.Impulse),
                Mathf.Max(_fragmentTorque, mobileDefaults.Torque),
                _lowPolyShardSpreadMultiplier,
                _lowPolyShardsUseBoxColliders);
            var parent = EnemyLowPolyShardBurst.Spawn(name, sourceBounds, hitPoint, material, settings);

            HideOriginalEnemy();
            if (_fragmentLifetimeSeconds > 0f)
                Destroy(parent, _fragmentLifetimeSeconds + 0.25f);
        }

        bool TryGetVisualBounds(out Renderer firstRenderer, out Bounds visualBounds)
        {
            firstRenderer = null;
            visualBounds = new Bounds(transform.position, Vector3.one);
            var renderers = GetComponentsInChildren<Renderer>();
            var initialized = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!initialized)
                {
                    firstRenderer = renderer;
                    visualBounds = renderer.bounds;
                    initialized = true;
                    continue;
                }

                visualBounds.Encapsulate(renderer.bounds);
            }

            return initialized;
        }

        bool TryCreateSourceMeshObject(out GameObject source, out Renderer sourceRenderer, out bool ownsMesh)
        {
            source = null;
            sourceRenderer = null;
            ownsMesh = false;

            var skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null && skinned.sharedMesh != null)
            {
                var baked = new Mesh { name = $"{name}_BakedPoseMesh" };
                skinned.BakeMesh(baked, false);
                source = CreateMeshSource($"{name}_BakedDemolishSource", baked, skinned.sharedMaterials,
                    skinned.transform.position, skinned.transform.rotation, skinned.transform.lossyScale);
                sourceRenderer = skinned;
                ownsMesh = true;
                return true;
            }

            var meshFilter = GetComponentInChildren<MeshFilter>();
            var meshRenderer = meshFilter != null ? meshFilter.GetComponent<MeshRenderer>() : null;
            if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
                return false;

            source = CreateMeshSource($"{name}_DemolishSource", meshFilter.sharedMesh, meshRenderer.sharedMaterials,
                meshFilter.transform.position, meshFilter.transform.rotation, meshFilter.transform.lossyScale);
            sourceRenderer = meshRenderer;
            return true;
        }

        static GameObject CreateMeshSource(string sourceName, Mesh mesh, Material[] materials, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var source = new GameObject(sourceName);
            source.transform.SetPositionAndRotation(position, rotation);
            source.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            source.hideFlags = HideFlags.HideAndDontSave;

            var filter = source.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = source.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = NormalizeMaterials(materials);
            return source;
        }

        static Material[] NormalizeMaterials(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
                return new[] { CreateFallbackMaterial() };

            var normalized = new Material[materials.Length];
            for (var i = 0; i < materials.Length; i++)
                normalized[i] = materials[i] != null ? materials[i] : CreateFallbackMaterial();
            return normalized;
        }

        List<Transform> CreateBreakPointTransforms(Bounds bounds, Vector3 hitPoint)
        {
            var positions = BuildBreakPointPositions(bounds, hitPoint, _breakPointCount, _breakRadius);
            var transforms = new List<Transform>(positions.Length);
            for (var i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"DemolishPoint_{i}");
                point.hideFlags = HideFlags.HideAndDontSave;
                point.transform.position = positions[i];
                transforms.Add(point.transform);
            }

            return transforms;
        }

        Material ResolveInteriorMaterial(Renderer sourceRenderer)
        {
            if (_interiorMaterial != null)
                return _interiorMaterial;
            if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
                return sourceRenderer.sharedMaterial;
            return CreateFallbackMaterial();
        }

        static Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            if (shader == null)
                return null;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(0.95f, 0.25f, 0.25f, 1f));
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(0.95f, 0.25f, 0.25f, 1f));
            return material;
        }

        void AddFragmentPhysics(GameObject fragment, Vector3 hitPoint)
        {
            if (fragment == null)
                return;

            var collider = fragment.GetComponent<MeshCollider>();
            if (collider == null)
                collider = fragment.AddComponent<MeshCollider>();
            collider.convex = true;

            var rb = fragment.GetComponent<Rigidbody>();
            if (rb == null)
                rb = fragment.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var direction = fragment.transform.position - hitPoint;
            if (direction.sqrMagnitude < 1e-6f)
                direction = UnityEngine.Random.onUnitSphere;
            rb.AddForce(direction.normalized * _fragmentImpulse, ForceMode.Impulse);
            if (_fragmentTorque > 0f)
                rb.AddTorque(UnityEngine.Random.onUnitSphere * _fragmentTorque, ForceMode.Impulse);
        }

        void HideOriginalEnemy()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            enabled = false;
        }

        static bool TryDemolish(
            GameObject source,
            List<Transform> breakPoints,
            Material interiorMaterial,
            out List<GameObject> fragments)
        {
            fragments = null;
            var demolisherType = Type.GetType("Hanzzz.MeshDemolisher.MeshDemolisher, Assembly-CSharp")
                                 ?? Type.GetType("Hanzzz.MeshDemolisher.MeshDemolisher, Hanzzz.MeshDemolisher");
            if (demolisherType == null)
            {
                Debug.LogWarning("MeshDemolisher runtime type was not found.");
                return false;
            }

            var demolisher = Activator.CreateInstance(demolisherType);
            var verify = demolisherType.GetMethod("VerifyDemolishInput", new[] { typeof(GameObject), typeof(List<Transform>) });
            var demolish = demolisherType.GetMethod("Demolish", new[] { typeof(GameObject), typeof(List<Transform>), typeof(Material) });
            if (verify == null || demolish == null)
            {
                Debug.LogWarning("MeshDemolisher public API was not found.");
                return false;
            }

            try
            {
                if (!Equals(verify.Invoke(demolisher, new object[] { source, breakPoints }), true))
                    return false;

                fragments = demolish.Invoke(demolisher, new object[] { source, breakPoints, interiorMaterial }) as List<GameObject>;
                return fragments != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MeshDemolisher failed to create fragments: {exception.Message}");
                return false;
            }
        }
    }
}
