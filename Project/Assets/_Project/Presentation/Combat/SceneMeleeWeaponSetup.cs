using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Adds melee combat components to scene-placed weapons (e.g. Fire Axe) that ship without the combat stack.
    /// </summary>
    public static class SceneMeleeWeaponSetup
    {
        const string DefaultAxeProfilePath =
            "Assets/_Project/Presentation/Combat/Profiles/WeaponAttackProfile_Axe.asset";
        const string DefaultRifleProfilePath =
            "Assets/_Project/Presentation/Combat/Profiles/WeaponAttackProfile_Rifle.asset";

        public static bool IsHk416WeaponRoot(GameObject weaponRoot)
        {
            if (weaponRoot == null)
                return false;

            // Match this object only — do not scan children or NavWorld (Floor parent) gets wired by mistake.
            return weaponRoot.name.IndexOf("HK416", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsHk416FloorPickupRoot(GameObject weaponRoot)
        {
            if (weaponRoot == null)
                return false;

            var name = weaponRoot.name;
            return name == "WeaponPickup_HK416" || name == "PickupVisual_HK416";
        }

        public static bool IsAllowedMeleeWeaponRoot(GameObject weaponRoot)
        {
            if (weaponRoot == null)
                return false;

            if (IsHk416FloorPickupRoot(weaponRoot))
                return true;

            var name = weaponRoot.name;
            return name.IndexOf("MeleeWeapon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasMiswiredMeleeStackMarkers(GameObject root)
        {
            if (root == null)
                return false;

            if (!IsAllowedMeleeWeaponRoot(root) && HasProtectedRootMiswirePhysics(root))
                return true;

            return root.GetComponent<SceneMeleeWeaponAutoSetup>() != null ||
                   root.GetComponent<XRGrabInteractable>() != null ||
                   root.GetComponent<WeaponMotion>() != null ||
                   root.GetComponent<MeleeWeaponRuntimeBinder>() != null;
        }

        static bool HasProtectedRootMiswirePhysics(GameObject root)
        {
            var name = root.name;
            if (name != "NavWorld" && name != "Floor" && name != "UnityChan_Player")
                return false;

            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
                return false;

            if (name == "UnityChan_Player")
                return !rigidbody.isKinematic;

            return true;
        }

        public static bool TrySanitizeMiswiredRootTransform(GameObject root)
        {
            if (root == null || root.name != "NavWorld")
                return false;

            var transform = root.transform;
            if (!IsCorruptTransform(transform))
                return false;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return true;
        }

        static bool IsCorruptTransform(Transform transform)
        {
            if (transform == null)
                return false;

            if (!IsFiniteVector(transform.position) || !IsFiniteVector(transform.localScale))
                return true;

            return transform.position.sqrMagnitude > 1e8f ||
                   transform.localScale.sqrMagnitude > 1e8f ||
                   transform.localScale.sqrMagnitude < 1e-8f;
        }

        static bool IsFiniteVector(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        public static int RepairMiswiredMeleeStacks(System.Action<GameObject> onRootRepaired = null)
        {
            var repaired = 0;
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (transform == null)
                    continue;

                var root = transform.gameObject;
                if (TrySanitizeMiswiredRootTransform(root))
                {
                    onRootRepaired?.Invoke(root);
                    repaired++;
                }

                if (!TryStripMiswiredSceneMeleeStack(root))
                    continue;

                onRootRepaired?.Invoke(root);
                repaired++;
            }

            return repaired;
        }

        /// <summary>
        /// Removes melee/physics stack when HK416 wire targeted NavWorld, UnityChan_Player, etc.
        /// </summary>
        public static bool TryStripMiswiredSceneMeleeStack(GameObject root)
        {
            if (root == null || !HasMiswiredMeleeStackMarkers(root) || IsAllowedMeleeWeaponRoot(root))
                return false;

            var stripped = false;
            stripped |= DestroyIfPresent<Rigidbody>(root);
            stripped |= DestroyIfPresent<XRGrabInteractable>(root);
            stripped |= DestroyIfPresent<SceneMeleeWeaponAutoSetup>(root);
            stripped |= DestroyIfPresent<SceneMeleeWeaponProfileSource>(root);
            stripped |= DestroyIfPresent<MeleeWeaponVrGrabLifecycle>(root);
            stripped |= DestroyIfPresent<WeaponMotionSourceRouter>(root);
            stripped |= DestroyIfPresent<VrGrabbedWeaponMotionSource>(root);
            stripped |= DestroyIfPresent<FlatMouseWeaponMotionSource>(root);
            stripped |= DestroyIfPresent<MobileTouchWeaponMotionSource>(root);
            stripped |= DestroyIfPresent<MeleeWeaponRuntimeBinder>(root);
            stripped |= DestroyIfPresent<WeaponMotion>(root);
            stripped |= DestroyIfPresent<WeaponAttackSession>(root);
            stripped |= DestroyIfPresent<WeaponHapticFeedback>(root);

            foreach (var detector in root.GetComponentsInChildren<WeaponHitDetector>(true))
            {
                if (detector != null)
                {
                    DestroyObject(detector.gameObject);
                    stripped = true;
                }
            }

            foreach (var childName in new[] { "Handle", "ForwardReference", "BladeTip", "MeleeHitDetector" })
            {
                var child = root.transform.Find(childName);
                if (child != null)
                {
                    DestroyObject(child.gameObject);
                    stripped = true;
                }
            }

            return stripped;
        }

        public static bool Ensure(GameObject weaponRoot, WeaponAttackProfile profile = null)
        {
            if (weaponRoot == null)
                return false;

            if (!IsAllowedMeleeWeaponRoot(weaponRoot))
            {
                Debug.LogWarning(
                    $"[SceneMeleeWeaponSetup] Refusing melee stack on '{weaponRoot.name}'. " +
                    "Only floor pickups (WeaponPickup_HK416) and scene melee weapons (Axe/MeleeWeapon_*) are allowed.",
                    weaponRoot);
                return false;
            }

            profile = ResolveProfile(weaponRoot, profile);
            if (profile == null)
                return false;

            if (weaponRoot.GetComponent<WeaponHitDetector>() != null &&
                weaponRoot.GetComponent<WeaponMotion>() != null &&
                weaponRoot.GetComponent<WeaponAttackSession>() != null)
            {
                RebindExisting(weaponRoot, profile);
                return false;
            }

            EnsureRigidbody(weaponRoot);
            EnsureGrabInteractable(weaponRoot);
            EnsureGrabLifecycle(weaponRoot);

            if (weaponRoot.GetComponent<WeaponMotionSourceRouter>() == null)
                weaponRoot.AddComponent<WeaponMotionSourceRouter>();
            if (weaponRoot.GetComponent<VrGrabbedWeaponMotionSource>() == null)
                weaponRoot.AddComponent<VrGrabbedWeaponMotionSource>();
            if (weaponRoot.GetComponent<FlatMouseWeaponMotionSource>() == null)
                weaponRoot.AddComponent<FlatMouseWeaponMotionSource>();
            if (weaponRoot.GetComponent<MobileTouchWeaponMotionSource>() == null)
                weaponRoot.AddComponent<MobileTouchWeaponMotionSource>();
            if (weaponRoot.GetComponent<MeleeWeaponRuntimeBinder>() == null)
                weaponRoot.AddComponent<MeleeWeaponRuntimeBinder>();

            var anchors = EnsureMotionAnchors(weaponRoot);
            var detector = EnsureHitDetector(weaponRoot, anchors.Tip, anchors.Handle);
            var motion = weaponRoot.GetComponent<WeaponMotion>() ?? weaponRoot.AddComponent<WeaponMotion>();
            var session = weaponRoot.GetComponent<WeaponAttackSession>() ?? weaponRoot.AddComponent<WeaponAttackSession>();
            var feedback = weaponRoot.GetComponent<WeaponHapticFeedback>() ?? weaponRoot.AddComponent<WeaponHapticFeedback>();
            var router = weaponRoot.GetComponent<WeaponMotionSourceRouter>();

            motion.BindSetup(router, profile, anchors.ForwardReference);
            session.BindSetup(motion, profile);
            detector.BindSetup(motion, session, profile);
            feedback.BindSetup(detector, profile);

            weaponRoot.GetComponent<VrGrabbedWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);
            weaponRoot.GetComponent<FlatMouseWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);
            weaponRoot.GetComponent<MobileTouchWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);

            weaponRoot.GetComponent<WeaponMotionSourceRouter>()?.ActivatePickedUpSource();
            weaponRoot.GetComponent<MeleeWeaponRuntimeBinder>()?.PickUpForFlatOrMobile();
            RefreshGrabColliders(weaponRoot);

            return true;
        }

        static void RebindExisting(GameObject weaponRoot, WeaponAttackProfile profile)
        {
            var anchors = EnsureMotionAnchors(weaponRoot);
            var motion = weaponRoot.GetComponent<WeaponMotion>();
            var session = weaponRoot.GetComponent<WeaponAttackSession>();
            var detector = weaponRoot.GetComponent<WeaponHitDetector>();
            var feedback = weaponRoot.GetComponent<WeaponHapticFeedback>();
            var router = weaponRoot.GetComponent<WeaponMotionSourceRouter>();

            motion?.BindSetup(router, profile, anchors.ForwardReference);
            session?.BindSetup(motion, profile);
            detector?.BindSetup(motion, session, profile);
            feedback?.BindSetup(detector, profile);

            weaponRoot.GetComponent<VrGrabbedWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);
            weaponRoot.GetComponent<FlatMouseWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);
            weaponRoot.GetComponent<MobileTouchWeaponMotionSource>()
                ?.BindSetup(anchors.Tip, anchors.Handle, anchors.ForwardReference);

            EnsureHitDetector(weaponRoot, anchors.Tip, anchors.Handle);
            RefreshGrabColliders(weaponRoot);
        }

        static void EnsureGrabLifecycle(GameObject weaponRoot)
        {
            if (weaponRoot.GetComponent<MeleeWeaponVrGrabLifecycle>() == null)
                weaponRoot.AddComponent<MeleeWeaponVrGrabLifecycle>();
        }

        static void RefreshGrabColliders(GameObject weaponRoot)
        {
            var grab = weaponRoot.GetComponent<XRGrabInteractable>();
            if (grab != null)
                MeleeWeaponGrabColliderUtility.RefreshGrabColliders(grab);
        }

        static WeaponAttackProfile ResolveProfile(GameObject weaponRoot, WeaponAttackProfile profile)
        {
            if (profile != null)
                return profile;

            var source = weaponRoot.GetComponent<SceneMeleeWeaponProfileSource>();
            if (source != null && source.Profile != null)
                return source.Profile;

#if UNITY_EDITOR
            var defaultPath = IsHk416WeaponRoot(weaponRoot)
                ? DefaultRifleProfilePath
                : DefaultAxeProfilePath;
            profile = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponAttackProfile>(defaultPath);
            if (profile == null && IsHk416WeaponRoot(weaponRoot))
                profile = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponAttackProfile>(DefaultAxeProfilePath);

            if (profile != null && source == null)
            {
                source = weaponRoot.AddComponent<SceneMeleeWeaponProfileSource>();
                source.SetProfile(profile);
            }
#endif
            return profile;
        }

        static void EnsureRigidbody(GameObject weaponRoot)
        {
            var body = weaponRoot.GetComponent<Rigidbody>();
            if (body == null)
                body = weaponRoot.AddComponent<Rigidbody>();
            body.mass = IsHk416WeaponRoot(weaponRoot) ? 3.2f : 0.85f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        static void EnsureGrabInteractable(GameObject weaponRoot)
        {
            if (weaponRoot.GetComponent<XRGrabInteractable>() != null)
                return;

            var grab = weaponRoot.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        }

        static MotionAnchors EnsureMotionAnchors(GameObject weaponRoot)
        {
            var bounds = CalculateRendererBounds(weaponRoot);
            var center = bounds.center;
            var localCenter = weaponRoot.transform.InverseTransformPoint(center);
            var forwardAxis = weaponRoot.transform.forward;
            var extentAlongForward = ProjectExtent(bounds, forwardAxis);

            var handle = FindOrCreateAnchor(weaponRoot.transform, "Handle");
            handle.localPosition = weaponRoot.transform.InverseTransformPoint(center - forwardAxis * extentAlongForward * 0.55f);

            var forwardRef = FindOrCreateAnchor(weaponRoot.transform, "ForwardReference");
            forwardRef.localPosition = handle.localPosition;
            forwardRef.localRotation = Quaternion.identity;

            var tip = FindOrCreateAnchor(weaponRoot.transform, "BladeTip");
            tip.localPosition = weaponRoot.transform.InverseTransformPoint(center + forwardAxis * extentAlongForward * 0.65f);

            return new MotionAnchors(tip, handle, forwardRef);
        }

        static WeaponHitDetector EnsureHitDetector(GameObject weaponRoot, Transform bladeTip, Transform handle)
        {
            WeaponHitDetector detector;
            Transform hitTransform;
            BoxCollider collider;

            var existing = weaponRoot.GetComponentInChildren<WeaponHitDetector>(true);
            if (existing != null)
            {
                detector = existing;
                hitTransform = existing.transform;
                collider = hitTransform.GetComponent<BoxCollider>();
                if (collider == null)
                    collider = hitTransform.gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                var hitGo = new GameObject("MeleeHitDetector");
                hitGo.transform.SetParent(weaponRoot.transform, false);
                hitTransform = hitGo.transform;
                collider = hitGo.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                detector = hitGo.AddComponent<WeaponHitDetector>();
            }

            ApplyBladeTipHitCollider(hitTransform, collider, weaponRoot.transform, bladeTip, handle);
            return detector;
        }

        static void ApplyBladeTipHitCollider(
            Transform hitTransform,
            BoxCollider collider,
            Transform weaponRoot,
            Transform bladeTip,
            Transform handle)
        {
            if (bladeTip == null || collider == null)
                return;

            var tipLocal = weaponRoot.InverseTransformPoint(bladeTip.position);
            var handleLocal = handle != null
                ? weaponRoot.InverseTransformPoint(handle.position)
                : tipLocal - Vector3.forward * 0.2f;
            var forwardLocal = tipLocal - handleLocal;
            if (forwardLocal.sqrMagnitude < 1e-6f)
                forwardLocal = Vector3.forward;

            var spec = MeleeHitColliderLayout.BuildBladeTipSpec(
                CombatMath.FromUnity(tipLocal),
                CombatMath.FromUnity(forwardLocal));

            hitTransform.localPosition = new Vector3(spec.LocalCenter.X, spec.LocalCenter.Y, spec.LocalCenter.Z);
            hitTransform.localRotation = Quaternion.identity;
            collider.center = Vector3.zero;
            collider.size = new Vector3(
                Mathf.Max(spec.LocalSize.X, 0.04f),
                Mathf.Max(spec.LocalSize.Y, 0.04f),
                Mathf.Max(spec.LocalSize.Z, 0.08f));
            collider.isTrigger = true;
        }

        static Transform FindOrCreateAnchor(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Bounds CalculateRendererBounds(GameObject root)
        {
            var bounds = new Bounds(root.transform.position, Vector3.one * 0.4f);
            var initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        static bool DestroyIfPresent<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component == null)
                return false;

            DestroyObject(component);
            return true;
        }

        static void DestroyObject(Object target)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                Object.DestroyImmediate(target);
            else
#endif
                Object.Destroy(target);
        }

        static float ProjectExtent(Bounds bounds, Vector3 axis)
        {
            axis.Normalize();
            var extents = bounds.extents;
            return Mathf.Abs(axis.x) * extents.x +
                   Mathf.Abs(axis.y) * extents.y +
                   Mathf.Abs(axis.z) * extents.z;
        }

        readonly struct MotionAnchors
        {
            public MotionAnchors(Transform tip, Transform handle, Transform forwardReference)
            {
                Tip = tip;
                Handle = handle;
                ForwardReference = forwardReference;
            }

            public Transform Tip { get; }
            public Transform Handle { get; }
            public Transform ForwardReference { get; }
        }
    }

    /// <summary>
    /// Keeps <see cref="XRGrabInteractable"/> colliders in sync after runtime setup or physics drops.
    /// </summary>
    public static class MeleeWeaponGrabColliderUtility
    {
        const float BrokenColliderWorldSizeThreshold = 5f;
        static readonly Vector3 FallbackBoxSize = new(0.26f, 0.14f, 0.92f);
        static readonly Vector3 FallbackBoxCenter = new(0f, 0f, 0.32f);
        static readonly Vector3 RifleFallbackBoxSize = new(0.12f, 0.16f, 0.88f);
        static readonly Vector3 RifleFallbackBoxCenter = new(0f, 0.04f, 0.36f);

        public static void RefreshGrabColliders(XRGrabInteractable grab)
        {
            if (grab == null)
                return;

            PruneBrokenColliders(grab.transform);
            EnsureFallbackSolidCollider(grab.gameObject, SceneMeleeWeaponSetup.IsHk416WeaponRoot(grab.gameObject));

            var colliders = new List<Collider>();
            foreach (var collider in grab.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider.enabled && !collider.isTrigger)
                    colliders.Add(collider);
            }

            grab.colliders.Clear();
            foreach (var collider in colliders)
                grab.colliders.Add(collider);
        }

        public static void PruneBrokenColliders(Transform root)
        {
            if (root == null)
                return;

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !(collider is BoxCollider))
                    continue;

                var worldSize = Vector3.Scale(((BoxCollider)collider).size, collider.transform.lossyScale);
                if (worldSize.magnitude > BrokenColliderWorldSizeThreshold)
                    collider.enabled = false;
            }
        }

        public static void EnsureFallbackSolidCollider(GameObject root, bool useRifleLayout = false)
        {
            if (root == null)
                return;

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider.enabled && !collider.isTrigger)
                    return;
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            box.isTrigger = false;
            box.enabled = true;
            box.size = useRifleLayout ? RifleFallbackBoxSize : FallbackBoxSize;
            box.center = useRifleLayout ? RifleFallbackBoxCenter : FallbackBoxCenter;
        }

        public static void RestoreWorldPickupPhysics(Transform weaponRoot)
        {
            if (weaponRoot == null)
                return;

            if (weaponRoot.parent != null)
                weaponRoot.SetParent(null, true);

            var body = weaponRoot.GetComponent<Rigidbody>();
            if (body == null)
                return;

            body.isKinematic = false;
            body.useGravity = true;
        }
    }

    /// <summary>
    /// Ensures scene melee weapons can be ray-grabbed again after snap or physics drops.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class MeleeWeaponVrGrabLifecycle : MonoBehaviour
    {
        XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }

        void Start()
        {
            RefreshForPickup();
        }

        void OnEnable()
        {
            if (_grab == null)
                _grab = GetComponent<XRGrabInteractable>();

            RegisterListeners();
            RefreshForPickup();
        }

        void OnDisable()
        {
            UnregisterListeners();
        }

        public void RefreshForPickup()
        {
            if (_grab == null)
                return;

            MeleeWeaponGrabColliderUtility.RefreshGrabColliders(_grab);

            if (!_grab.isSelected)
                MeleeWeaponGrabColliderUtility.RestoreWorldPickupPhysics(transform);
        }

        public void ReleaseFromHandSnap()
        {
            var vrSource = GetComponent<VrGrabbedWeaponMotionSource>();
            vrSource?.ResetHeldForVrSnap();

            MeleeWeaponGrabColliderUtility.RestoreWorldPickupPhysics(transform);
            RefreshForPickup();
        }

        void RegisterListeners()
        {
            if (_grab == null)
                return;

            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        void UnregisterListeners()
        {
            if (_grab == null)
                return;

            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs _)
        {
            var router = GetComponent<WeaponMotionSourceRouter>();
            router?.ActivateVrGrabbedHold();

            var binder = GetComponent<MeleeWeaponRuntimeBinder>();
            binder?.PickUpForFlatOrMobile();
        }

        void OnSelectExited(SelectExitEventArgs _)
        {
            ReleaseFromHandSnap();
        }
    }
}
