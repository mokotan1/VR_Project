using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class VrSceneWeaponSnapInput : MonoBehaviour
    {
        [SerializeField] XROrigin _xrOrigin;
        [SerializeField] XRNode _fireHand = XRNode.RightHand;
        [SerializeField, Range(0.01f, 1f)] float _analogTriggerThreshold = 0.55f;
        [SerializeField] Vector3 _rightFrontOffset = new Vector3(0.32f, -0.12f, 0.55f);
        [SerializeField] Vector3 _additionalWeaponStackOffset = new Vector3(0f, -0.18f, -0.08f);
        [SerializeField] WeaponAttackProfile _sceneMeleeProfile;

        VrTriggerPressDetector _pickupDetector;

        void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponent<XROrigin>();
        }

        void Update()
        {
            ReadControllerFeatures(
                _fireHand,
                out var triggerButton,
                out var triggerValue,
                out var gripButton,
                out var gripValue);

            if (!_pickupDetector.Tick(
                    IsControllerPickupPressed(
                        triggerButton,
                        triggerValue,
                        gripButton,
                        gripValue,
                        _analogTriggerThreshold),
                    out _pickupDetector))
                return;

            if (TryReleaseSnappedWeapons())
                return;

            SnapSceneWeaponsToRightFront();
        }

        public void Bind(XROrigin xrOrigin)
        {
            _xrOrigin = xrOrigin;
        }

        public bool TryReleaseSnappedWeapons()
        {
            var anchor = ResolveRightHandAnchor();
            if (anchor == null)
                return false;

            var released = false;
            foreach (var weaponRoot in FindSceneWeaponRoots())
            {
                if (weaponRoot == null || weaponRoot.parent != anchor)
                    continue;

                ReleaseSnappedWeapon(weaponRoot);
                released = true;
            }

            return released;
        }

        /// <summary>Snap the nearest floor <c>WeaponPickup_HK416</c> to the right hand (grip / G).</summary>
        public bool TrySnapNearestHk416PickupToRightHand(float maxDistance)
        {
            var anchor = ResolveRightHandAnchor();
            if (anchor == null)
                return false;

            if (HasHk416ChildOnAnchor(anchor))
                return true;

            Transform best = null;
            var bestSqr = maxDistance * maxDistance;
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (transform == null || transform.name != "WeaponPickup_HK416")
                    continue;

                var sqr = (transform.position - anchor.position).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = transform;
            }

            if (best == null)
                return false;

            SnapHk416Pickup(best, anchor);
            return true;
        }

        /// <summary>Release HK416 held on the right hand after grip is released.</summary>
        public bool TryReleaseHk416FromRightHand()
        {
            var anchor = ResolveRightHandAnchor();
            if (anchor == null)
                return false;

            var released = false;
            foreach (var child in anchor.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == anchor || child.name != "WeaponPickup_HK416")
                    continue;

                ReleaseSnappedWeapon(child);
                released = true;
            }

            return released;
        }

        static bool HasHk416ChildOnAnchor(Transform anchor)
        {
            foreach (var child in anchor.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == anchor)
                    continue;

                if (child.name == "WeaponPickup_HK416" || child.name == "HandGun_HK416")
                    return true;
            }

            return false;
        }

        void SnapHk416Pickup(Transform weaponRoot, Transform anchor)
        {
            SnapWeapon(weaponRoot, anchor, 0);
            SceneMeleeWeaponSetup.TrySanitizeTransform(weaponRoot);
            SceneMeleeWeaponSetup.StripMiswiredAncestorGrabInteractables(weaponRoot.gameObject);

            var router = weaponRoot.GetComponent<WeaponMotionSourceRouter>();
            if (router != null)
                router.ActivateVrSnappedHeldSource();
        }

        public void SnapSceneWeaponsToRightFront()
        {
            var anchor = ResolveRightHandAnchor();
            if (anchor == null)
                return;

            var snapped = 0;
            foreach (var weaponRoot in FindSceneWeaponRoots())
            {
                if (weaponRoot == null)
                    continue;

                SnapWeapon(weaponRoot, anchor, snapped);
                snapped++;
            }
        }

        public static void ReadControllerFeatures(
            XRNode hand,
            out bool triggerButton,
            out float triggerValue,
            out bool gripButton,
            out float gripValue)
        {
            triggerButton = false;
            triggerValue = 0f;
            gripButton = false;
            gripValue = 0f;

            var device = InputDevices.GetDeviceAtXRNode(hand);
            if (!device.isValid)
                return;

            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerButton);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerValue);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out gripButton);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out gripValue);
        }

        public Transform ResolveRightHandAnchor()
        {
            var root = _xrOrigin != null ? _xrOrigin.transform : transform;
            var rightController = FindChildTransformByName(root, "Right Controller");
            if (rightController != null)
                return rightController;
            return _xrOrigin != null && _xrOrigin.Camera != null ? _xrOrigin.Camera.transform : root;
        }

        static bool IsEditorMousePickupPressed()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool IsControllerPickupPressed(
            bool triggerButton,
            float triggerValue,
            bool gripButton,
            float gripValue,
            float analogThreshold) =>
            IsControllerTriggerPressed(triggerButton, triggerValue, analogThreshold) ||
            IsControllerGripPressed(gripButton, gripValue, analogThreshold);

        public static bool IsControllerTriggerPressed(bool triggerButton, float triggerValue, float analogThreshold) =>
            triggerButton || triggerValue >= analogThreshold;

        public static bool IsControllerGripPressed(bool gripButton, float gripValue, float analogThreshold) =>
            gripButton || gripValue >= analogThreshold;

        void SnapWeapon(Transform weaponRoot, Transform anchor, int stackIndex)
        {
            var localOffset = _rightFrontOffset + _additionalWeaponStackOffset * Mathf.Max(0, stackIndex);
            weaponRoot.SetParent(anchor, false);
            weaponRoot.localPosition = localOffset;
            weaponRoot.localRotation = Quaternion.identity;
            SceneMeleeWeaponSetup.TrySanitizeTransform(weaponRoot);

            var profile = ResolveMeleeProfile(weaponRoot.gameObject);
            SceneMeleeWeaponSetup.Ensure(weaponRoot.gameObject, profile);
            TryAlignMeleeBladeTowardViewCenter(weaponRoot, anchor, localOffset);

            var binder = weaponRoot.GetComponent<MeleeWeaponRuntimeBinder>();
            if (binder != null)
                binder.MarkPickedUpForVrSnap();

            var router = weaponRoot.GetComponent<WeaponMotionSourceRouter>();
            if (router != null)
                router.ActivateVrSnappedHeldSource();

            var vrSource = weaponRoot.GetComponent<VrGrabbedWeaponMotionSource>();
            if (vrSource != null)
                vrSource.NotifyHeldForVrSnap();

            EnsureGrabLifecycle(weaponRoot.gameObject);

            var rb = weaponRoot.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        static void ReleaseSnappedWeapon(Transform weaponRoot)
        {
            weaponRoot.GetComponent<MeleeWeaponVrGrabLifecycle>()?.ReleaseFromHandSnap();
        }

        static void EnsureGrabLifecycle(GameObject weaponRoot)
        {
            if (weaponRoot.GetComponent<MeleeWeaponVrGrabLifecycle>() == null)
                weaponRoot.AddComponent<MeleeWeaponVrGrabLifecycle>();
        }

        Transform ResolveViewTransform()
        {
            return _xrOrigin != null && _xrOrigin.Camera != null
                ? _xrOrigin.Camera.transform
                : null;
        }

        void TryAlignMeleeBladeTowardViewCenter(Transform weaponRoot, Transform anchor, Vector3 localOffset)
        {
            if (!IsMeleeSnapTarget(weaponRoot))
                return;

            if (!VrMeleeWeaponViewAlignment.TryGetBladeAxis(weaponRoot, out var handleLocal, out var tipLocal))
                return;

            var view = ResolveViewTransform();
            if (view == null)
                return;

            if (!VrMeleeWeaponViewAlignment.TryComputeSnapLocalRotation(
                    anchor,
                    localOffset,
                    handleLocal,
                    tipLocal,
                    view.position,
                    view.forward,
                    view.up,
                    out var localRotation))
                return;

            weaponRoot.localRotation = localRotation;
        }

        WeaponAttackProfile ResolveMeleeProfile(GameObject weaponRoot)
        {
            if (_sceneMeleeProfile != null)
                return _sceneMeleeProfile;

            var source = weaponRoot.GetComponent<SceneMeleeWeaponProfileSource>();
            if (source != null && source.Profile != null)
                return source.Profile;

            return null;
        }

        static bool IsMeleeSnapTarget(Transform weaponRoot)
        {
            if (weaponRoot == null)
                return false;

            if (weaponRoot.GetComponent<MeleeWeaponRuntimeBinder>() != null)
                return true;

            return IsSceneAxeRoot(weaponRoot);
        }

        static Transform[] FindSceneWeaponRoots()
        {
            var results = new System.Collections.Generic.List<Transform>();

            foreach (var melee in UnityEngine.Object.FindObjectsByType<MeleeWeaponRuntimeBinder>(FindObjectsInactive.Include))
            {
                if (SceneMeleeWeaponSetup.IsHk416WeaponRoot(melee.gameObject))
                    continue;

                AddUnique(results, melee.transform);
            }

            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (IsSceneAxeRoot(transform))
                    AddUnique(results, transform);
            }

            return results.ToArray();
        }

        static bool IsSceneAxeRoot(Transform transform)
        {
            var name = transform.name;
            return name.Contains("Axe") || name.Contains("MeleeWeapon");
        }

        static void AddUnique(System.Collections.Generic.List<Transform> results, Transform candidate)
        {
            if (candidate == null)
                return;
            var root = ResolveSceneWeaponRoot(candidate);
            if (!results.Contains(root))
                results.Add(root);
        }

        static Transform ResolveSceneWeaponRoot(Transform candidate)
        {
            var melee = candidate.GetComponentInParent<MeleeWeaponRuntimeBinder>();
            if (melee != null)
                return melee.transform;

            var current = candidate;
            while (current.parent != null)
            {
                if (current.parent.name == "WeaponPickup_HK416" ||
                    current.parent.name.Contains("MeleeWeapon") ||
                    current.parent.name.Contains("Axe"))
                    current = current.parent;
                else
                    break;
            }

            return current;
        }

        static Transform FindChildTransformByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
