using UnityEngine;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Orients a melee weapon so its blade axis points toward the camera view center when VR-snapped.
    /// </summary>
    public static class VrMeleeWeaponViewAlignment
    {
        const float MinViewDistanceMeters = 0.25f;

        public static bool TryComputeSnapLocalRotation(
            Transform anchor,
            Vector3 weaponLocalPosition,
            Vector3 handleLocalPosition,
            Vector3 tipLocalPosition,
            Vector3 viewPosition,
            Vector3 viewForward,
            Vector3 viewUp,
            out Quaternion localRotation)
        {
            localRotation = Quaternion.identity;
            if (anchor == null)
                return false;

            var bladeLocal = tipLocalPosition - handleLocalPosition;
            if (bladeLocal.sqrMagnitude < 1e-8f)
                return false;

            bladeLocal.Normalize();

            var handleWorld = anchor.TransformPoint(weaponLocalPosition + handleLocalPosition);
            var viewDirection = viewForward.sqrMagnitude > 1e-8f ? viewForward.normalized : Vector3.forward;
            var viewUpAxis = viewUp.sqrMagnitude > 1e-8f ? viewUp.normalized : Vector3.up;
            var viewDistance = Vector3.Distance(viewPosition, handleWorld);
            var viewTarget = viewPosition + viewDirection * Mathf.Max(MinViewDistanceMeters, viewDistance);

            var desiredWorld = viewTarget - handleWorld;
            if (desiredWorld.sqrMagnitude < 1e-8f)
                desiredWorld = viewDirection;
            else
                desiredWorld.Normalize();

            var desiredInAnchor = Quaternion.Inverse(anchor.rotation) * desiredWorld;
            var upInAnchor = Quaternion.Inverse(anchor.rotation) * viewUpAxis;
            if (upInAnchor.sqrMagnitude < 1e-8f)
                upInAnchor = Vector3.up;

            var look = Quaternion.LookRotation(desiredInAnchor, upInAnchor);
            var rollFix = Quaternion.FromToRotation(look * bladeLocal, desiredInAnchor);
            localRotation = rollFix * look;
            return true;
        }

        public static bool TryGetBladeAxis(Transform weaponRoot, out Vector3 handleLocal, out Vector3 tipLocal)
        {
            handleLocal = default;
            tipLocal = default;
            if (weaponRoot == null)
                return false;

            var handle = weaponRoot.Find("Handle");
            var tip = weaponRoot.Find("BladeTip");
            if (handle == null || tip == null)
                return false;

            handleLocal = handle.localPosition;
            tipLocal = tip.localPosition;
            return (tipLocal - handleLocal).sqrMagnitude >= 1e-8f;
        }
    }
}
