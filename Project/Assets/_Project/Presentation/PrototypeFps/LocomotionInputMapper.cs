using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Maps planar WASD-style input for movement and for Unity-Chan Locomotions Mecanim.
    /// </summary>
    public static class LocomotionInputMapper
    {
        const float AnimatorBackwardDominance = 0.15f;
        const float AnimatorBackwardStrafeSlack = 0.1f;
        const float AnimatorLocomotionSpeedScale = 0.85f;
        const float AnimatorMinLocomotionSpeed = 0.12f;
        const float AnimatorMaxLocomotionSpeed = 0.8f;
        const float AnimatorMinWalkBackSpeed = -0.11f;

        /// <summary>
        /// Normalized planar axes (x = strafe, y = forward) for movement; magnitude ≤ 1.
        /// </summary>
        public static Vector2 ToUnityChanAxes(Vector3 localPlanarInput)
        {
            var x = localPlanarInput.x;
            var z = localPlanarInput.z;
            var m = new Vector2(x, z).magnitude;
            if (m > 1f)
            {
                x /= m;
                z /= m;
            }

            return new Vector2(x, z);
        }

        /// <summary>
        /// Animator parameters for UnityChanLocomotions: x = Direction (strafe -1..1),
        /// y = Speed (positive magnitude for Locomotion blend tree, negative for WalkBack when moving back).
        /// Pure A/D sets a positive Speed so Idle → Locomotion can fire.
        /// </summary>
        public static Vector2 ToUnityChanAnimatorAxes(Vector3 localPlanarInputRaw)
        {
            var xz = ToUnityChanAxes(localPlanarInputRaw);
            var x = xz.x;
            var z = xz.y;

            if (Mathf.Abs(x) < 0.0001f && Mathf.Abs(z) < 0.0001f)
                return Vector2.zero;

            if (z < -AnimatorBackwardDominance &&
                Mathf.Abs(x) <= Mathf.Abs(z) + AnimatorBackwardStrafeSlack)
                return new Vector2(0f, Mathf.Clamp(z, -1f, AnimatorMinWalkBackSpeed));

            var mag = xz.magnitude;
            var speed = Mathf.Clamp(
                mag * AnimatorLocomotionSpeedScale,
                AnimatorMinLocomotionSpeed,
                AnimatorMaxLocomotionSpeed);
            return new Vector2(Mathf.Clamp(x, -1f, 1f), speed);
        }
    }
}
