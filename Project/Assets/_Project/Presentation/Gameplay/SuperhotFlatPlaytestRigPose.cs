using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Pure helpers for snapping body yaw and camera pitch from a world-space forward vector (FPS-style).
    /// </summary>
    public static class SuperhotFlatPlaytestRigPose
    {
        public static void DecomposeYawPitchDegrees(Vector3 worldForward, out float yawDegrees, out float pitchDegrees)
        {
            var f = worldForward.normalized;
            var xz = Mathf.Sqrt(f.x * f.x + f.z * f.z);
            if (xz < 1e-6f)
                xz = 1e-6f;

            yawDegrees = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            pitchDegrees = Mathf.Atan2(-f.y, xz) * Mathf.Rad2Deg;
        }
    }
}
