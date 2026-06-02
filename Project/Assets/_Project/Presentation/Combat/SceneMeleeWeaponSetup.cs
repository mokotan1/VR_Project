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

        public static bool Ensure(GameObject weaponRoot, WeaponAttackProfile profile = null)
        {
            if (weaponRoot == null)
                return false;

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
        }

        static WeaponAttackProfile ResolveProfile(GameObject weaponRoot, WeaponAttackProfile profile)
        {
            if (profile != null)
                return profile;

            var source = weaponRoot.GetComponent<SceneMeleeWeaponProfileSource>();
            if (source != null && source.Profile != null)
                return source.Profile;

#if UNITY_EDITOR
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
            body.mass = 0.85f;
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
}
