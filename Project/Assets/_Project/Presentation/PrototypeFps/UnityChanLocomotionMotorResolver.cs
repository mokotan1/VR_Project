using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Resolves <see cref="IUnityChanLocomotionMotor"/> on the player root (built-in motor vs Dyrda adapter).
    /// </summary>
    public static class UnityChanLocomotionMotorResolver
    {
        public static IUnityChanLocomotionMotor ResolveOn(GameObject root)
        {
            if (root == null)
                return null;
            var p = root.GetComponent<PrototypeThirdPersonPlayer>();
            if (p != null)
                return p;
            return root.GetComponent<DyrdaFirstPersonMotorAdapter>();
        }

        /// <summary>For editor wiring of <see cref="BattleRoyaleStyleCrosshair"/> (assign as <c>MonoBehaviour</c>).</summary>
        public static MonoBehaviour ResolveMotorBehaviourOn(GameObject root)
        {
            return ResolveOn(root) as MonoBehaviour;
        }
    }
}
