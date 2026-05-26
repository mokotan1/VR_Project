using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class CrystalDefenseAwarenessMath
    {
        public static bool IsVisibleInViewport(Vector3 viewportPoint)
        {
            return viewportPoint.z > 0f &&
                   viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                   viewportPoint.y >= 0f && viewportPoint.y <= 1f;
        }

        public static bool TryGetOffscreenIndicator(
            Vector3 viewportPoint,
            Vector2 canvasSize,
            float edgePadding,
            out Vector2 anchoredPosition,
            out float angleDegrees)
        {
            var fromCenter = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            if (viewportPoint.z < 0f)
                fromCenter = -fromCenter;

            if (fromCenter.sqrMagnitude < 0.0001f)
                fromCenter = Vector2.up;

            var direction = fromCenter.normalized;
            var halfSize = canvasSize * 0.5f;
            var paddedHalf = new Vector2(
                Mathf.Max(0f, halfSize.x - edgePadding),
                Mathf.Max(0f, halfSize.y - edgePadding));

            if (paddedHalf.x <= 0f || paddedHalf.y <= 0f)
            {
                anchoredPosition = Vector2.zero;
                angleDegrees = 0f;
                return false;
            }

            var xScale = Mathf.Abs(direction.x) > 0.0001f ? paddedHalf.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
            var yScale = Mathf.Abs(direction.y) > 0.0001f ? paddedHalf.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
            anchoredPosition = direction * Mathf.Min(xScale, yScale);
            angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            return true;
        }

        public static float Threat01(float distance, float nearDistance, float farDistance)
        {
            if (farDistance <= nearDistance)
                return distance <= nearDistance ? 1f : 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(farDistance, nearDistance, distance));
        }
    }
}
