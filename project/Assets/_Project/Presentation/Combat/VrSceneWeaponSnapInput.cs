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

        VrTriggerPressDetector _triggerDetector;

        void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponent<XROrigin>();
        }

        void Update()
        {
            if (!_triggerDetector.Tick(IsPickupPressed(), out _triggerDetector))
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

        bool IsPickupPressed()
        {
            if (IsEditorMousePickupPressed())
                return true;

            var device = InputDevices.GetDeviceAtXRNode(_fireHand);
            if (!device.isValid)
                return false;

            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var triggerButton);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out var triggerValue);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out var gripButton);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out var gripValue);
            return IsControllerPickupPressed(
                triggerButton,
                triggerValue,
                gripButton,
                gripValue,
                _analogTriggerThreshold);
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
            float analogThreshold)
        {
            if (triggerButton || triggerValue >= analogThreshold)
                return true;

            return gripButton || gripValue >= analogThreshold;
        }

        void SnapWeapon(Transform weaponRoot, Transform anchor, int stackIndex)
        {
            var localOffset = _rightFrontOffset + _additionalWeaponStackOffset * Mathf.Max(0, stackIndex);
            weaponRoot.SetParent(anchor, false);
            weaponRoot.localPosition = localOffset;
            weaponRoot.localRotation = Quaternion.identity;

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

        Transform ResolveRightHandAnchor()
        {
            var root = _xrOrigin != null ? _xrOrigin.transform : transform;
            var rightController = FindChildTransformByName(root, "Right Controller");
            if (rightController != null)
                return rightController;
            return _xrOrigin != null && _xrOrigin.Camera != null ? _xrOrigin.Camera.transform : root;
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

            return IsSceneAxeRoot(weaponRoot) || IsSceneGunRoot(weaponRoot);
        }

        static Transform[] FindSceneWeaponRoots()
        {
            var results = new System.Collections.Generic.List<Transform>();

            foreach (var melee in UnityEngine.Object.FindObjectsByType<MeleeWeaponRuntimeBinder>(FindObjectsInactive.Include))
                AddUnique(results, melee.transform);

            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (IsSceneGunRoot(transform) || IsSceneAxeRoot(transform))
                    AddUnique(results, transform);
            }

            return results.ToArray();
        }

        static bool IsSceneGunRoot(Transform transform)
        {
            var name = transform.name;
            return name == "WeaponPickup_HK416" ||
                   name == "PickupVisual_HK416" ||
                   name == "HandGun_HK416";
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
