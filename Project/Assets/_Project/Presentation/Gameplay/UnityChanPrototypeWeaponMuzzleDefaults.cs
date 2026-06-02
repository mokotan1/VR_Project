using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class UnityChanPrototypeWeaponMuzzleDefaults
    {
        public const string FirePointName = "WeaponFirePoint";
        public const string DefaultBulletPrefabPath = "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_01.prefab";
        public const float BulletVisualScale = 1f;
        public const float BulletSpeed = 95f;
        public const float BulletMuzzleForwardOffset = 0.45f;

        /// <summary>총구 bounds tip 기준 HK416 <see cref="WeaponFirePoint"/> 추가 전방 (로컬 +Z, m). 10포인트=0.1m(10cm).</summary>
        public const float Hk416FirePointForwardFromMuzzleLocalZ = 0.1f;

        public static readonly Vector3 BulletVisualEulerOffset = new(90f, 0f, 0f);
        public static readonly Vector3 FallbackFirePointLocalPosition = new(0f, 0f, 0.35f);

        /// <summary>Viewport center → world point at <paramref name="aimDistance"/> → direction from muzzle (parallax-safe).</summary>
        public static Vector3 ComputeAimDirectionFromViewport(Camera camera, Vector3 muzzleWorldPosition, float aimDistance)
        {
            if (camera == null)
                return Vector3.forward;

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var dist = Mathf.Max(1f, aimDistance);
            var aimPoint = ray.GetPoint(dist);
            var delta = aimPoint - muzzleWorldPosition;
            if (delta.sqrMagnitude < 1e-6f)
                return ray.direction.sqrMagnitude > 1e-6f ? ray.direction.normalized : Vector3.forward;

            return delta.normalized;
        }

        /// <summary><see cref="WeaponFirePoint"/> 월드 위치·전방(+Z). 없으면 false.</summary>
        public static bool TryGetShotPoseFromFirePoint(Transform firePoint, out Vector3 position, out Vector3 direction)
        {
            position = default;
            direction = Vector3.forward;
            if (firePoint == null)
                return false;

            position = firePoint.position;
            direction = firePoint.forward.sqrMagnitude > 1e-6f
                ? firePoint.forward.normalized
                : Vector3.forward;
            return true;
        }

        public static bool TryGetMuzzleWorldPose(GameObject gunVisual, out Vector3 position, out Vector3 forward)
        {
            position = default;
            forward = Vector3.forward;
            if (gunVisual == null)
                return false;

            var gunRoot = gunVisual.transform;
            var authored = FindFirePoint(gunRoot);
            if (authored != null)
            {
                position = authored.position;
                forward = authored.forward;
                return true;
            }

            if (TryComputeFirePointLocalPosition(gunVisual, out var localPosition))
            {
                position = gunRoot.TransformPoint(localPosition);
                forward = gunRoot.forward;
                return true;
            }

            position = gunRoot.position + gunRoot.forward * BulletMuzzleForwardOffset;
            forward = gunRoot.forward;
            return true;
        }

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

            if (TryComputeFirePointLocalPosition(gunVisual, out var localPosition))
                firePoint.localPosition = localPosition;
            else
                firePoint.localPosition = FallbackFirePointLocalPosition;

            firePoint.localRotation = Quaternion.identity;
            return firePoint;
        }

        static void SyncFirePointLocalFromGun(GameObject gunVisual, Transform firePoint)
        {
            if (gunVisual == null || firePoint == null)
                return;

            if (TryComputeFirePointLocalPosition(gunVisual, out var localPosition))
                firePoint.localPosition = localPosition;
        }

        public static bool IsHk416GunVisual(GameObject gunVisual)
        {
            if (gunVisual == null)
                return false;

            var n = gunVisual.name;
            return n.IndexOf("hk416", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryComputeFirePointLocalPosition(GameObject gunVisual, out Vector3 localPosition)
        {
            if (!TryComputeLocalMuzzleOffset(gunVisual, out localPosition))
                return false;

            if (IsHk416GunVisual(gunVisual))
                localPosition += new Vector3(0f, 0f, Hk416FirePointForwardFromMuzzleLocalZ);

            return true;
        }

        static bool TryComputeLocalMuzzleOffset(GameObject gunVisual, out Vector3 localPosition)
        {
            localPosition = FallbackFirePointLocalPosition;
            if (gunVisual == null)
                return false;

            var gunRoot = gunVisual.transform;
            if (!TryFindForwardBoundsPoint(gunVisual, out var worldPoint, out _))
                return false;

            localPosition = gunRoot.InverseTransformPoint(worldPoint);
            return true;
        }

        static Transform FindFirePoint(Transform root)
        {
            Transform fallback = null;
            var exactName = FirePointName.ToLowerInvariant();
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null || tr == root)
                    continue;

                var n = tr.name.ToLowerInvariant();
                if (n == exactName)
                    return tr;

                if (fallback == null &&
                    (n.Contains("muzzle") || n.Contains("firepoint") || n.Contains("fire_point") ||
                     n.Contains("barrel_tip")))
                    fallback = tr;
            }

            return fallback;
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
