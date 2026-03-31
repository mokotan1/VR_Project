#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VRProject.Application.Gameplay;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    public static class SuperhotPrototypeSceneMenu
    {
        const string ScenePath = "Assets/Scenes/SuperhotPrototype.unity";
        const string ProjectilePrefabPath = "Assets/_Project/Presentation/Gameplay/Prefabs/SuperhotProjectile.prefab";
        const string XriPackageJsonPath = "Packages/com.unity.xr.interaction.toolkit/package.json";

        static readonly Vector3 XrRigSpawnPosition = new Vector3(0f, 0f, -2f);

        [MenuItem("VR Project/Scenes/Create Superhot Prototype Scene")]
        public static void CreateSuperhotPrototypeScene()
        {
            if (!TryEnsureStarterAssetsImported())
            {
                EditorUtility.DisplayDialog(
                    "Superhot Prototype",
                    "XR Interaction Toolkit Starter Assets could not be imported or the VR rig prefab is still missing. " +
                    "Open Window → Package Manager → XR Interaction Toolkit → Samples → import \"Starter Assets\" (add Shader Graph if prompted), then run this menu again.",
                    "OK");
                return;
            }

            var rigPrefabPath = ResolveXrRigPrefabAssetPath();
            if (string.IsNullOrEmpty(rigPrefabPath))
            {
                EditorUtility.DisplayDialog(
                    "Superhot Prototype",
                    "Could not locate \"XR Origin (XR Rig).prefab\" under Starter Assets after import.",
                    "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(6f, 1f, 6f);
            floor.AddComponent<TeleportationArea>();

            var systems = new GameObject("Systems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<XRInteractionManager>();
            systems.AddComponent<SuperhotGameplayDriver>();
            systems.AddComponent<SuperhotPlaytestRigSelector>();

            var flowGo = new GameObject("NodeFlow");
            var flow = flowGo.AddComponent<SuperhotNodeFlow>();

            var zonesParent = new GameObject("CombatZones");
            var zoneA = BuildZone("Zone_A", zonesParent.transform, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.25f, 3f), flow, isFirst: true);
            var zoneB = BuildZone("Zone_B", zonesParent.transform, new Vector3(0f, 0f, 14f), new Vector3(0f, 0.25f, 3f), flow, isFirst: false);

            var entryPoseB = new GameObject("EntryCameraPose");
            entryPoseB.transform.SetParent(zoneB.transform, false);
            entryPoseB.transform.localPosition = new Vector3(0f, 1.6f, -2f);
            entryPoseB.transform.localRotation = Quaternion.identity;

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("_zonesInOrder").arraySize = 2;
            flowSo.FindProperty("_zonesInOrder").GetArrayElementAtIndex(0).objectReferenceValue = zoneA;
            flowSo.FindProperty("_zonesInOrder").GetArrayElementAtIndex(1).objectReferenceValue = zoneB;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            var projectilePrefab = EnsureProjectilePrefab();
            AssignProjectileToZones(zoneA, zoneB, projectilePrefab);

            WirePortalDestination(zoneA, entryPoseB.transform);
            WirePortalDestinationFromLocal(zoneB, zoneB.transform, new Vector3(0f, 1.6f, 0f));

            InstantiateXrRigAndWireSystems(scene, systems, rigPrefabPath);
            BuildFlatPlaytestRig(XrRigSpawnPosition);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            AddSceneToBuildSettingsIfNeeded(ScenePath);

            Debug.Log(
                $"[VR Project] Saved {ScenePath} with XR Origin (XR Rig). Locomotion matches Starter Assets (smooth move / teleport / turn). Floor has TeleportationArea. Add SuperhotLocomotionDisabler on Systems and enable Disable On Awake for room-scale-only SUPERHOT lock.");
        }

        static bool TryEnsureStarterAssetsImported()
        {
            if (!string.IsNullOrEmpty(ResolveXrRigPrefabAssetPath()))
                return true;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(XriPackageJsonPath);
            if (packageInfo == null)
            {
                Debug.LogError("[VR Project] com.unity.xr.interaction.toolkit package.json not found.");
                return false;
            }

            if (!TryFindStarterAssetsSample(packageInfo, out var starterSample))
            {
                Debug.LogError("[VR Project] Starter Assets sample not found in XR Interaction Toolkit package.");
                return false;
            }

            if (!starterSample.isImported)
            {
                if (!EditorUtility.DisplayDialog(
                    "Import Starter Assets",
                    "The XR Interaction Toolkit \"Starter Assets\" sample is required for the VR rig prefab. Import it now?",
                    "Import",
                    "Cancel"))
                    return false;

                starterSample.Import(Sample.ImportOptions.OverridePreviousImports);
                AssetDatabase.Refresh();
            }

            if (!string.IsNullOrEmpty(ResolveXrRigPrefabAssetPath()))
                return true;

            Debug.LogWarning("[VR Project] Starter Assets reported imported but rig prefab missing; re-importing sample.");
            if (!TryFindStarterAssetsSample(packageInfo, out starterSample))
                return false;
            starterSample.Import(Sample.ImportOptions.OverridePreviousImports);
            AssetDatabase.Refresh();
            return !string.IsNullOrEmpty(ResolveXrRigPrefabAssetPath());
        }

        static bool TryFindStarterAssetsSample(UnityEditor.PackageManager.PackageInfo packageInfo, out Sample starterSample)
        {
            starterSample = default;
            var packageSamples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
            if (packageSamples == null)
                return false;

            foreach (var s in packageSamples)
            {
                if (s.displayName != "Starter Assets")
                    continue;
                starterSample = s;
                return true;
            }

            return false;
        }

        static string ResolveXrRigPrefabAssetPath()
        {
            var guids = AssetDatabase.FindAssets("XR Origin (XR Rig) t:Prefab");
            if (guids == null || guids.Length == 0)
                return null;

            var paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            return SuperhotXrRigPrefabPathSelector.SelectPreferredPath(paths);
        }

        static void BuildFlatPlaytestRig(Vector3 worldPosition)
        {
            var root = new GameObject("Flat Playtest Rig");
            root.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<SuperhotFlatPlaytestRig>();
            root.AddComponent<SuperhotFlatFpsController>();
            root.AddComponent<SuperhotFlatHitscanWeapon>();

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var cam = camGo.GetComponent<Camera>();

            var rig = root.GetComponent<SuperhotFlatPlaytestRig>();
            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("_characterController").objectReferenceValue = cc;
            rigSo.FindProperty("_camera").objectReferenceValue = camGo.transform;
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            var fps = root.GetComponent<SuperhotFlatFpsController>();
            var fpsSo = new SerializedObject(fps);
            fpsSo.FindProperty("_characterController").objectReferenceValue = cc;
            fpsSo.FindProperty("_cameraTransform").objectReferenceValue = camGo.transform;
            fpsSo.ApplyModifiedPropertiesWithoutUndo();

            var weapon = root.GetComponent<SuperhotFlatHitscanWeapon>();
            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("_camera").objectReferenceValue = cam;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void InstantiateXrRigAndWireSystems(Scene scene, GameObject systems, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[VR Project] Failed to load rig prefab at {prefabPath}");
                return;
            }

            var rigInstance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (rigInstance == null)
            {
                Debug.LogError("[VR Project] InstantiatePrefab returned null for XR rig.");
                return;
            }

            rigInstance.name = "XR Origin (XR Rig)";
            rigInstance.transform.SetPositionAndRotation(XrRigSpawnPosition, Quaternion.identity);

            foreach (var mgr in rigInstance.GetComponentsInChildren<XRInteractionManager>(true))
            {
                Object.DestroyImmediate(mgr);
            }

            var origin = rigInstance.GetComponent<XROrigin>();

            var driver = systems.GetComponent<SuperhotGameplayDriver>();
            var driverSo = new SerializedObject(driver);
            driverSo.FindProperty("_xrOrigin").objectReferenceValue = origin;
            driverSo.FindProperty("_hmd").objectReferenceValue =
                origin != null && origin.Camera != null ? origin.Camera.transform : null;
            driverSo.FindProperty("_leftController").objectReferenceValue =
                FindChildTransformByExactName(rigInstance.transform, "Left Controller");
            driverSo.FindProperty("_rightController").objectReferenceValue =
                FindChildTransformByExactName(rigInstance.transform, "Right Controller");
            driverSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform FindChildTransformByExactName(Transform root, string exactName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == exactName)
                    return t;
            }

            return null;
        }

        static void AddSceneToBuildSettingsIfNeeded(string scenePath)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath)))
                return;

            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
            {
                if (s.path == scenePath)
                    return;
            }

            var list = new List<EditorBuildSettingsScene>(existing)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        static SuperhotCombatZone BuildZone(
            string name,
            Transform parent,
            Vector3 rootWorldPosition,
            Vector3 enemyLocalBase,
            SuperhotNodeFlow flow,
            bool isFirst)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = rootWorldPosition;
            if (!isFirst)
                root.SetActive(false);

            var zone = root.AddComponent<SuperhotCombatZone>();

            var exitRoot = new GameObject("ExitPyramid");
            exitRoot.transform.SetParent(root.transform, false);
            exitRoot.transform.localPosition = new Vector3(1.2f, 1f, 0f);
            exitRoot.SetActive(false);

            var pyramid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pyramid.name = "PyramidMesh";
            pyramid.transform.SetParent(exitRoot.transform, false);
            pyramid.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            pyramid.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var rb = pyramid.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            pyramid.AddComponent<XRGrabInteractable>();

            var portal = pyramid.AddComponent<SuperhotGrabExitPortal>();
            var portalSo = new SerializedObject(portal);
            portalSo.FindProperty("_owningZone").objectReferenceValue = zone;
            portalSo.FindProperty("_nodeFlow").objectReferenceValue = flow;
            portalSo.ApplyModifiedPropertiesWithoutUndo();

            var zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("_exitInteractableRoot").objectReferenceValue = exitRoot;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < 2; i++)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = $"Enemy_{i}";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = enemyLocalBase + new Vector3(i * 1.2f - 0.6f, 0.75f, 0f);
                cap.AddComponent<SuperhotEnemy>();
                cap.AddComponent<SuperhotEnemyMover>();
                cap.AddComponent<SuperhotEnemyShooter>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.85f, 0.15f, 0.12f);
                cap.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            return zone;
        }

        static void WirePortalDestination(SuperhotCombatZone fromZone, Transform cameraDestination)
        {
            var exitRoot = fromZone.transform.Find("ExitPyramid");
            var pyramid = exitRoot.GetChild(0);
            var portal = pyramid.GetComponent<SuperhotGrabExitPortal>();

            var so = new SerializedObject(portal);
            so.FindProperty("_cameraWorldDestination").objectReferenceValue = cameraDestination;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WirePortalDestinationFromLocal(SuperhotCombatZone fromZone, Transform parent, Vector3 localPosition)
        {
            var exitRoot = fromZone.transform.Find("ExitPyramid");
            var pyramid = exitRoot.GetChild(0);
            var portal = pyramid.GetComponent<SuperhotGrabExitPortal>();

            var destGo = new GameObject("EndCameraPose");
            destGo.transform.SetParent(parent, false);
            destGo.transform.localPosition = localPosition;
            destGo.transform.localRotation = Quaternion.identity;

            var so = new SerializedObject(portal);
            so.FindProperty("_cameraWorldDestination").objectReferenceValue = destGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SuperhotProjectile EnsureProjectilePrefab()
        {
            var dir = System.IO.Path.GetDirectoryName(ProjectilePrefabPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var existing = AssetDatabase.LoadAssetAtPath<SuperhotProjectile>(ProjectilePrefabPath);
            if (existing != null)
                return existing;

            var projGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projGo.name = "SuperhotProjectile";
            projGo.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(projGo.GetComponent<Collider>());
            var sphere = projGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            var rb = projGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            projGo.AddComponent<SuperhotProjectile>();
            var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pMat.color = new Color(0.9f, 0.85f, 0.2f);
            projGo.GetComponent<MeshRenderer>().sharedMaterial = pMat;

            var prefab = PrefabUtility.SaveAsPrefabAsset(projGo, ProjectilePrefabPath);
            Object.DestroyImmediate(projGo);
            return prefab.GetComponent<SuperhotProjectile>();
        }

        static void AssignProjectileToZones(SuperhotCombatZone a, SuperhotCombatZone b, SuperhotProjectile prefab)
        {
            foreach (var shooter in a.GetComponentsInChildren<SuperhotEnemyShooter>(true))
            {
                var so = new SerializedObject(shooter);
                so.FindProperty("_projectilePrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var shooter in b.GetComponentsInChildren<SuperhotEnemyShooter>(true))
            {
                var so = new SerializedObject(shooter);
                so.FindProperty("_projectilePrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
