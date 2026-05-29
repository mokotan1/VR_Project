using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class ShieldBlocker : MonoBehaviour
    {
        [SerializeField] Transform _blockFacing;
        [SerializeField] float _blockFacingDotMin = 0.35f;

        public bool TryBlock(Vector3 weaponApproachDirection, Vector3 hitPoint, out float facingDot)
        {
            var facing = _blockFacing != null ? _blockFacing.forward : transform.forward;
            facingDot = Vector3.Dot(-weaponApproachDirection.normalized, facing.normalized);
            return facingDot >= _blockFacingDotMin;
        }
    }
}
