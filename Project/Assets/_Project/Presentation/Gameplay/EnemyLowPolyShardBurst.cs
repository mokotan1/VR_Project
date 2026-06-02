using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class EnemyLowPolyShardBurst
    {
        public readonly struct Settings
        {
            public Settings(
                int shardCount,
                float shardScale,
                float impulse,
                float torque,
                float spreadMultiplier,
                bool useBoxColliders)
            {
                ShardCount = Mathf.Clamp(shardCount, 1, 16);
                ShardScale = Mathf.Clamp(shardScale, 0.05f, 2f);
                Impulse = Mathf.Max(0f, impulse);
                Torque = Mathf.Max(0f, torque);
                SpreadMultiplier = Mathf.Clamp(spreadMultiplier, 0.25f, 3f);
                UseBoxColliders = useBoxColliders;
            }

            public int ShardCount { get; }
            public float ShardScale { get; }
            public float Impulse { get; }
            public float Torque { get; }
            public float SpreadMultiplier { get; }
            public bool UseBoxColliders { get; }

            public static Settings MobileDefault => new Settings(
                shardCount: 10,
                shardScale: 0.75f,
                impulse: 7.5f,
                torque: 4f,
                spreadMultiplier: 1.6f,
                useBoxColliders: false);
        }

        public static GameObject Spawn(
            string sourceName,
            Bounds sourceBounds,
            Vector3 hitPoint,
            Material material,
            Settings settings)
        {
            return Spawn(sourceName, sourceBounds, hitPoint, material, settings, Vector3.zero);
        }

        public static GameObject Spawn(
            string sourceName,
            Bounds sourceBounds,
            Vector3 hitPoint,
            Material material,
            Settings settings,
            Vector3 impulseDirection)
        {
            var parent = new GameObject($"{sourceName}_LowPolyShards");
            parent.transform.position = sourceBounds.center;

            var count = Mathf.Clamp(settings.ShardCount, 1, 16);
            for (var i = 0; i < count; i++)
                CreateShard(parent.transform, sourceBounds, hitPoint, material, settings, impulseDirection, i, count);

            return parent;
        }

        static void CreateShard(
            Transform parent,
            Bounds sourceBounds,
            Vector3 hitPoint,
            Material material,
            Settings settings,
            Vector3 impulseDirection,
            int index,
            int count)
        {
            var shard = new GameObject($"Shard_{index + 1:00}");
            shard.transform.SetParent(parent, false);

            var offset = BuildShardOffset(sourceBounds, index, count, settings.SpreadMultiplier);
            shard.transform.position = sourceBounds.center + offset;
            shard.transform.rotation = Quaternion.Euler(index * 37f, index * 71f, index * 19f);

            var filter = shard.AddComponent<MeshFilter>();
            filter.sharedMesh = BuildShardMesh(sourceBounds, settings.ShardScale, index);

            var renderer = shard.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;

            if (settings.UseBoxColliders)
            {
                var box = shard.AddComponent<BoxCollider>();
                box.size = filter.sharedMesh.bounds.size;
                box.center = filter.sharedMesh.bounds.center;
            }

            var rb = shard.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.None;

            var direction = shard.transform.position - hitPoint;
            if (direction.sqrMagnitude < 1e-6f)
                direction = Vector3.up;

            if (impulseDirection.sqrMagnitude > 1e-6f)
                direction = (direction.normalized * 0.45f + impulseDirection.normalized * 0.85f).normalized;

            rb.AddForce(direction * settings.Impulse, ForceMode.Impulse);
            if (settings.Torque > 0f)
                rb.AddTorque(BuildDeterministicAxis(index) * settings.Torque, ForceMode.Impulse);
        }

        static Vector3 BuildShardOffset(Bounds bounds, int index, int count, float spreadMultiplier)
        {
            var t = (float)index / Mathf.Max(1, count);
            var angle = index * 137.50777f * Mathf.Deg2Rad;
            var horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.25f);
            var verticalExtent = Mathf.Max(bounds.extents.y, 0.25f);
            var radius = Mathf.Lerp(0.18f, 0.68f, t) * horizontalExtent * spreadMultiplier;
            var y = Mathf.Lerp(-0.35f, 0.55f, (index % 5) / 4f) * verticalExtent * Mathf.Sqrt(spreadMultiplier);
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        static Mesh BuildShardMesh(Bounds sourceBounds, float shardScale, int index)
        {
            var size = Mathf.Max(sourceBounds.extents.magnitude * 0.2f * shardScale, 0.08f);
            var skew = 0.35f + (index % 3) * 0.12f;

            var vertices = new[]
            {
                new Vector3(-size, -size * 0.5f, -size * skew),
                new Vector3(size * 0.8f, -size * 0.45f, -size * 0.25f),
                new Vector3(size * 0.2f, -size * 0.35f, size),
                new Vector3(-size * 0.15f, size, -size * 0.05f)
            };
            var triangles = new[]
            {
                0, 1, 2,
                0, 3, 1,
                1, 3, 2,
                2, 3, 0
            };

            var mesh = new Mesh { name = "EnemyLowPolyShard" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static Vector3 BuildDeterministicAxis(int index)
        {
            var axis = new Vector3(
                ((index * 17) % 11) - 5f,
                ((index * 29) % 13) - 6f,
                ((index * 43) % 7) - 3f);
            return axis.sqrMagnitude < 1e-6f ? Vector3.up : axis.normalized;
        }
    }
}
