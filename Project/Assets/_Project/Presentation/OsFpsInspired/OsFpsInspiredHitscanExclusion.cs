using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    /// <summary>
    /// Hitscan이 플레이어 몸통·본 콜라이더를 맞고 막히지 않도록 제외할 계층을 정합니다.
    /// </summary>
    public static class OsFpsInspiredHitscanExclusion
    {
        public static Transform ResolveExclusionRoot(Component weaponBehaviour)
        {
            if (weaponBehaviour == null)
                return null;
            var cc = weaponBehaviour.GetComponent<CharacterController>()
                     ?? weaponBehaviour.GetComponentInParent<CharacterController>();
            return cc != null ? cc.transform : weaponBehaviour.transform;
        }

        public static bool IsColliderUnderExclusionRoot(Collider c, Transform exclusionRoot)
        {
            if (c == null || exclusionRoot == null)
                return false;
            var t = c.transform;
            return t == exclusionRoot || t.IsChildOf(exclusionRoot);
        }

        /// <summary>
        /// 카메라 원점이 캡슐/메시 안에 있을 때 근거리 자기 히트를 건너뜁니다.
        /// </summary>
        public static Ray BuildBiasedAimRay(Ray viewportRay, float forwardOffset, float maxDistance,
            out float castDistance)
        {
            var dir = viewportRay.direction.normalized;
            var off = Mathf.Clamp(forwardOffset, 0f, Mathf.Max(0f, maxDistance * 0.45f));
            castDistance = Mathf.Max(0.05f, maxDistance - off);
            return new Ray(viewportRay.origin + dir * off, dir);
        }
    }
}
