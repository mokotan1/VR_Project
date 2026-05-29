using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    internal static class CombatMath
    {
        public static CombatVector3 FromUnity(Vector3 v) => new CombatVector3(v.x, v.y, v.z);
    }
}
