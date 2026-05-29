using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class UnityChanPrototypeWeaponMuzzleDefaults
    {
        public const string FirePointName = "WeaponFirePoint";
        public const float BulletVisualScale = 56f;
        public const float BulletSpeed = 95f;
        public const float BulletMuzzleForwardOffset = 0.45f;

        public static readonly Vector3 BulletVisualEulerOffset = new(90f, 0f, 0f);
        public static readonly Vector3 FallbackFirePointLocalPosition = new(0f, 0f, 0.35f);

        public static Transform EnsureFirePoint(GameObject gunVisual)
        {
            if (gunVisual == null)
                return null;

            var gunRoot = gunVisual.transform;
            var existing = FindFirePoint(gunRoot);
            if (existing != null)
                return existing;

            var firePoint = new GameObject(FirePointName).transform;
            firePoint.SetParent(gunRoot, false);

            if (TryFindForwardBoundsPoint(gunVisual, out var worldPoint, out var worldRotation))
            {
                firePoint.SetPositionAndRotation(worldPoint, worldRotation);
                firePoint.SetParent(gunRoot, true);
            }
            else
            {
                firePoint.localPosition = FallbackFirePointLocalPosition;
                firePoint.localRotation = Quaternion.identity;
            }

            return firePoint;
        }

        static Transform FindFirePoint(Transform root)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                var n = tr.name.ToLowerInvariant();
                if (n.Contains("muzzle") || n.Contains("firepoint") || n.Contains("fire_point") ||
                    n.Contains("barrel_tip") || n == FirePointName.ToLowerInvariant())
                    return tr;
            }

            return null;
        }

        static bool TryFindForwardBoundsPoint(GameObject gunVisual, out Vector3 worldPoint, out Quaternion worldRotation)
        {
            var gunRoot = gunVisual.transform;
            var renderers = gunVisual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                worldPoint = default;
                worldRotation = default;
                return false;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var forward = gunRoot.forward;
            var center = bounds.center;
            var extents = bounds.extents;
            var corners = new Vector3[8];
            var index = 0;
            for (var x = -1f; x <= 1f; x += 2f)
            for (var y = -1f; y <= 1f; y += 2f)
            for (var z = -1f; z <= 1f; z += 2f)
                corners[index++] = center + new Vector3(extents.x * x, extents.y * y, extents.z * z);

            worldPoint = corners[0];
            var bestDot = float.MinValue;
            foreach (var point in corners)
            {
                var dot = Vector3.Dot(point - center, forward);
                if (dot <= bestDot)
                    continue;

                bestDot = dot;
                worldPoint = point;
            }

            worldRotation = Quaternion.LookRotation(forward, gunRoot.up);
            return true;
        }
    }
}
