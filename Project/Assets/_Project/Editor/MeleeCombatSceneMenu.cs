#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Application.Combat;
using VRProject.Presentation.Combat;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    public static class MeleeCombatSceneMenu
    {
        const string ScenePath = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity";
        const string ProfilesFolder = "Assets/_Project/Presentation/Combat/Profiles";
        const string PrefabsFolder = "Assets/_Project/Presentation/Combat/Prefabs";
        const string SwordProfilePath = ProfilesFolder + "/WeaponAttackProfile_Sword.asset";
        const string KnifeProfilePath = ProfilesFolder + "/WeaponAttackProfile_Knife.asset";
        const string BluntProfilePath = ProfilesFolder + "/WeaponAttackProfile_BluntHammer.asset";
        const string ShieldProfilePath = ProfilesFolder + "/WeaponAttackProfile_Shield.asset";
        const string SwordPrefabPath = PrefabsFolder + "/MeleeWeapon_Sword.prefab";

        [MenuItem("VR Project/Combat/Ensure Default Weapon Profiles")]
        public static void EnsureDefaultProfiles()
        {
            Directory.CreateDirectory(ProfilesFolder);
            AssetDatabase.Refresh();

            CreateProfileIfMissing(SwordProfilePath, WeaponFamily.Hybrid, 1.5f, 0.6f, 0.5f);
            CreateProfileIfMissing(KnifeProfilePath, WeaponFamily.Stab, 1.2f, 0.75f, 0.35f);
            CreateProfileIfMissing(BluntProfilePath, WeaponFamily.Blunt, 1.8f, 0.35f, 0.25f);
            CreateProfileIfMissing(ShieldProfilePath, WeaponFamily.Blunt, 1f, 0.2f, 0.2f);

            AssetDatabase.SaveAssets();
            Debug.Log("[VR Project] Melee weapon profiles ensured at " + ProfilesFolder);
        }

        [MenuItem("VR Project/Combat/Create Melee Shield Prefab")]
        public static void CreateMeleeShieldPrefab()
        {
            EnsureDefaultProfiles();
            Directory.CreateDirectory(PrefabsFolder);
            var profile = AssetDatabase.LoadAssetAtPath<WeaponAttackProfile>(ShieldProfilePath);
            var root = BuildShieldRoot("MeleeWeapon_Shield", profile);
            var path = PrefabsFolder + "/MeleeWeapon_Shield.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Selection.activeObject = prefab;
            Debug.Log("[VR Project] Saved melee shield prefab: " + path);
        }

        [MenuItem("VR Project/Combat/Create Melee Sword Prefab")]
        public static void CreateMeleeSwordPrefab()
        {
            EnsureDefaultProfiles();
            Directory.CreateDirectory(PrefabsFolder);

            var profile = AssetDatabase.LoadAssetAtPath<WeaponAttackProfile>(SwordProfilePath);
            var root = BuildMeleeWeaponRoot("MeleeWeapon_Sword", profile, new Color(0.75f, 0.78f, 0.82f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SwordPrefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = prefab;
            Debug.Log("[VR Project] Saved melee sword prefab: " + SwordPrefabPath);
        }

        [MenuItem("VR Project/Combat/Spawn Melee Weapon In Open Scene")]
        public static void SpawnMeleeWeaponInOpenScene()
        {
            if (!IsUnityChanSceneOpen())
            {
                Debug.LogWarning("[VR Project] Open UnityChanPrototypeFps before spawning melee weapon.");
                return;
            }

            EnsureDefaultProfiles();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath) == null)
                CreateMeleeSwordPrefab();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[VR Project] Missing melee sword prefab at " + SwordPrefabPath);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(1.5f, 1f, 2f);
            instance.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Selection.activeGameObject = instance;
            Debug.Log("[VR Project] Spawned melee weapon in open scene.");
        }

        [MenuItem("VR Project/Combat/Wire Enemy Melee Hit Zones In Open Scene")]
        public static void WireEnemyMeleeHitZonesInOpenScene()
        {
            if (!IsUnityChanSceneOpen())
            {
                Debug.LogWarning("[VR Project] Open UnityChanPrototypeFps before wiring enemy hit zones.");
                return;
            }

            var wired = 0;
            foreach (var brain in Object.FindObjectsByType<SuperhotEnemyBrain>(FindObjectsSortMode.None))
            {
                if (MeleeEnemySetup.Ensure(brain.gameObject))
                    wired++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[VR Project] Wired melee hit zones on {wired} enemy agent(s).");
        }

        static bool IsUnityChanSceneOpen()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return scene.path.Replace('\\', '/').EndsWith("UnityChanPrototypeFps.unity");
        }

        static void CreateProfileIfMissing(string path, WeaponFamily family, float enterLinear, float stabDot, float slashDot)
        {
            if (AssetDatabase.LoadAssetAtPath<WeaponAttackProfile>(path) != null)
                return;

            var profile = ScriptableObject.CreateInstance<WeaponAttackProfile>();
            var so = new SerializedObject(profile);
            so.FindProperty("_family").enumValueIndex = (int)family;
            so.FindProperty("_enterLinearSpeed").floatValue = enterLinear;
            so.FindProperty("_stabForwardDotMin").floatValue = stabDot;
            so.FindProperty("_slashSideDotMin").floatValue = slashDot;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, path);
        }

        static GameObject BuildMeleeWeaponRoot(string name, WeaponAttackProfile profile, Color bladeColor)
        {
            var root = new GameObject(name);
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.75f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            root.AddComponent<XRGrabInteractable>();
            root.AddComponent<WeaponMotionSourceRouter>();
            root.AddComponent<VrGrabbedWeaponMotionSource>();
            root.AddComponent<FlatMouseWeaponMotionSource>();
            root.AddComponent<MobileTouchWeaponMotionSource>();
            root.AddComponent<MeleeWeaponRuntimeBinder>();

            var handle = new GameObject("Handle").transform;
            handle.SetParent(root.transform, false);
            handle.localPosition = new Vector3(0f, 0f, -0.18f);

            var forwardRef = new GameObject("ForwardReference").transform;
            forwardRef.SetParent(handle, false);
            forwardRef.localRotation = Quaternion.identity;

            var tip = new GameObject("BladeTip").transform;
            tip.SetParent(root.transform, false);
            tip.localPosition = new Vector3(0f, 0f, 0.55f);

            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0f, 0.28f);
            blade.transform.localScale = new Vector3(0.05f, 0.02f, 0.62f);
            Object.DestroyImmediate(blade.GetComponent<BoxCollider>());
            blade.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = bladeColor
            };

            var hitCollider = blade.AddComponent<BoxCollider>();
            hitCollider.isTrigger = true;
            blade.AddComponent<WeaponHitDetector>();

            var motion = root.AddComponent<WeaponMotion>();
            var session = root.AddComponent<WeaponAttackSession>();
            root.AddComponent<WeaponHapticFeedback>();

            WireComponentReferences(root, handle, tip, forwardRef, profile, blade.GetComponent<WeaponHitDetector>());

            return root;
        }

        static void WireComponentReferences(
            GameObject root,
            Transform handle,
            Transform tip,
            Transform forwardRef,
            WeaponAttackProfile profile,
            WeaponHitDetector detector)
        {
            var router = root.GetComponent<WeaponMotionSourceRouter>();
            var routerSo = new SerializedObject(router);
            routerSo.FindProperty("_vrSource").objectReferenceValue = root.GetComponent<VrGrabbedWeaponMotionSource>();
            routerSo.FindProperty("_flatSource").objectReferenceValue = root.GetComponent<FlatMouseWeaponMotionSource>();
            routerSo.FindProperty("_mobileSource").objectReferenceValue = root.GetComponent<MobileTouchWeaponMotionSource>();
            routerSo.ApplyModifiedPropertiesWithoutUndo();

            WireMotionSource(root.GetComponent<VrGrabbedWeaponMotionSource>(), tip, handle, forwardRef);
            WireMotionSource(root.GetComponent<FlatMouseWeaponMotionSource>(), tip, handle, forwardRef);
            WireMotionSource(root.GetComponent<MobileTouchWeaponMotionSource>(), tip, handle, forwardRef);

            var motion = root.GetComponent<WeaponMotion>();
            var motionSo = new SerializedObject(motion);
            motionSo.FindProperty("_router").objectReferenceValue = router;
            motionSo.FindProperty("_profile").objectReferenceValue = profile;
            motionSo.FindProperty("_rotationReference").objectReferenceValue = forwardRef;
            motionSo.ApplyModifiedPropertiesWithoutUndo();

            var session = root.GetComponent<WeaponAttackSession>();
            var sessionSo = new SerializedObject(session);
            sessionSo.FindProperty("_motion").objectReferenceValue = motion;
            sessionSo.FindProperty("_profile").objectReferenceValue = profile;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();

            var detectorSo = new SerializedObject(detector);
            detectorSo.FindProperty("_motion").objectReferenceValue = motion;
            detectorSo.FindProperty("_session").objectReferenceValue = session;
            detectorSo.FindProperty("_profile").objectReferenceValue = profile;
            detectorSo.ApplyModifiedPropertiesWithoutUndo();

            var feedback = root.GetComponent<WeaponHapticFeedback>();
            var feedbackSo = new SerializedObject(feedback);
            feedbackSo.FindProperty("_detector").objectReferenceValue = detector;
            feedbackSo.FindProperty("_profile").objectReferenceValue = profile;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireMotionSource(MonoBehaviour source, Transform tip, Transform handle, Transform forwardRef)
        {
            if (source == null)
                return;

            var so = new SerializedObject(source);
            so.FindProperty("_tip").objectReferenceValue = tip;
            so.FindProperty("_handle").objectReferenceValue = handle;
            so.FindProperty("_forwardReference").objectReferenceValue = forwardRef;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildShieldRoot(string name, WeaponAttackProfile profile)
        {
            var root = BuildMeleeWeaponRoot(name, profile, new Color(0.35f, 0.45f, 0.75f));
            var shieldZone = new GameObject("HitZone_Shield");
            shieldZone.transform.SetParent(root.transform, false);
            shieldZone.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            var col = shieldZone.AddComponent<BoxCollider>();
            col.size = new Vector3(0.45f, 0.55f, 0.06f);
            col.isTrigger = true;
            var zone = shieldZone.AddComponent<HitZone>();
            var zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("_kind").enumValueIndex = (int)HitZoneKind.Shield;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();
            shieldZone.AddComponent<ShieldBlocker>();
            shieldZone.AddComponent<ParryWindow>();

            var parry = root.GetComponent<WeaponHapticFeedback>();
            if (parry != null)
            {
                var parrySo = new SerializedObject(parry);
                parrySo.FindProperty("_parryWindow").objectReferenceValue = shieldZone.GetComponent<ParryWindow>();
                parrySo.ApplyModifiedPropertiesWithoutUndo();
            }

            return root;
        }
    }

    public static class MeleeEnemySetup
    {
        public static bool Ensure(GameObject enemyRoot)
        {
            if (enemyRoot == null)
                return false;

            if (enemyRoot.GetComponent<SuperhotEnemy>() == null)
                enemyRoot.AddComponent<SuperhotEnemy>();
            if (enemyRoot.GetComponent<DamageReceiver>() == null)
                enemyRoot.AddComponent<DamageReceiver>();

            var changed = false;
            changed |= EnsureZone(enemyRoot.transform, "HitZone_Head", HitZoneKind.Head, new Vector3(0f, 1.55f, 0f), new Vector3(0.35f, 0.35f, 0.35f), 1.5f);
            changed |= EnsureZone(enemyRoot.transform, "HitZone_Torso", HitZoneKind.Torso, new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.75f, 0.35f), 1f);
            changed |= EnsureZone(enemyRoot.transform, "HitZone_Limb", HitZoneKind.Limb, new Vector3(0.35f, 0.55f, 0f), new Vector3(0.25f, 0.55f, 0.25f), 0.85f);
            return true;
        }

        static bool EnsureZone(
            Transform parent,
            string name,
            HitZoneKind kind,
            Vector3 localPosition,
            Vector3 size,
            float feedbackMultiplier)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return false;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var zone = go.AddComponent<HitZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_feedbackMultiplier").floatValue = feedbackMultiplier;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
#endif
