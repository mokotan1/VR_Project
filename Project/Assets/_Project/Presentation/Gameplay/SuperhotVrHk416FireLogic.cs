using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Presentation.Combat;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// XR HK416 hitscan: camera aim, muzzle tracer spawn, <see cref="SuperhotEnemy"/> instant kill.
    /// </summary>
    public static class SuperhotVrHk416FireLogic
    {
        public static bool ShouldFireThisFrame(bool gripHeld, bool triggerPressedThisFrame) =>
            gripHeld && triggerPressedThisFrame;

        public static bool TryGetHk416OnHandAnchor(Transform handAnchor, out Transform weaponRoot)
        {
            weaponRoot = null;
            if (handAnchor == null)
                return false;

            Transform pickupRoot = null;
            Transform handGunRoot = null;
            foreach (var child in handAnchor.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == handAnchor)
                    continue;

                var name = child.name;
                if (name == "WeaponPickup_HK416")
                    pickupRoot = child;
                else if (name == "HandGun_HK416" && SceneMeleeWeaponSetup.IsHk416WeaponRoot(child.gameObject))
                    handGunRoot = child;
                else if (name == "PickupVisual_HK416" && child.parent != null &&
                         child.parent.name == "WeaponPickup_HK416")
                    pickupRoot = child.parent;
            }

            if (pickupRoot != null)
            {
                weaponRoot = pickupRoot;
                return true;
            }

            if (handGunRoot != null)
            {
                weaponRoot = handGunRoot;
                return true;
            }

            return TryGetGrabbedHk416OnHandAnchor(handAnchor, out weaponRoot);
        }

        static bool TryGetGrabbedHk416OnHandAnchor(Transform handAnchor, out Transform weaponRoot)
        {
            weaponRoot = null;
            foreach (var grab in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include))
            {
                if (grab == null || !grab.isSelected)
                    continue;

                var root = grab.transform;
                if (!SceneMeleeWeaponSetup.IsHk416WeaponRoot(root.gameObject))
                    continue;

                foreach (var interactor in grab.interactorsSelecting)
                {
                    if (interactor == null)
                        continue;

                    var interactorTransform = interactor.transform;
                    if (interactorTransform == handAnchor || interactorTransform.IsChildOf(handAnchor))
                    {
                        weaponRoot = root;
                        return true;
                    }
                }
            }

            return false;
        }

        public static GameObject ResolveGunVisual(Transform weaponRoot)
        {
            if (weaponRoot == null)
                return null;

            foreach (var tr in weaponRoot.GetComponentsInChildren<Transform>(true))
            {
                var name = tr.name;
                if (name == "HandGun_HK416" || name == "PickupVisual_HK416")
                    return tr.gameObject;
            }

            return weaponRoot.gameObject;
        }

        public static bool TryRaycastKillEnemy(
            Ray aimRay,
            float maxDistance,
            LayerMask hitMask,
            Transform exclusionRoot,
            out RaycastHit bestHit)
        {
            var hits = Physics.RaycastAll(
                aimRay,
                maxDistance,
                hitMask,
                QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                bestHit = default;
                return false;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (exclusionRoot != null && hit.collider.transform.IsChildOf(exclusionRoot))
                    continue;

                var enemy = hit.collider.GetComponentInParent<SuperhotEnemy>();
                if (enemy == null)
                    continue;

                enemy.Kill(hit);
                bestHit = hit;
                return true;
            }

            bestHit = default;
            return false;
        }
    }
}
