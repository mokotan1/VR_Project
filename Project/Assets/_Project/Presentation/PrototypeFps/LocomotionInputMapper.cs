using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Maps normalized planar WASD-style input to Unity-Chan Locomotions Mecanim axes
    /// (Direction = horizontal, Speed = forward/back), matching the official sample convention.
    /// </summary>
    public static class LocomotionInputMapper
    {
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
    }
}
