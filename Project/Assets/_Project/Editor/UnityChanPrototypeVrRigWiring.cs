#if UNITY_EDITOR
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using VRProject.Application.Gameplay;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Adds XR Origin (XR Rig) + <see cref="SuperhotPlaytestRigSelector"/> to Unity-Chan prototype scenes
    /// so Startup VR Play uses the same rig path as SuperhotPrototype.
    /// </summary>
    public static class UnityChanPrototypeVrRigWiring
    {
        const string XriPackageJsonPath = "Packages/com.unity.xr.interaction.toolkit/package.json";
        const string MainScenePath = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity";

        [MenuItem("VR Project/Scenes/Wire VR Rig into Unity-Chan Prototype FPS")]
        public static void WireMenu()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!System.IO.File.Exists(MainScenePath))
                {
                    EditorUtility.DisplayDialog(
                        "Wire VR Rig",
                        "Open UnityChanPrototypeFps or run Create Unity-Chan Prototype FPS first.",
                        "OK");
                    return;
                }

                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            if (!TryWireScene(scene, out var message))
            {
                EditorUtility.DisplayDialog("Wire VR Rig", message, "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[VR Project] Wired VR rig into " + scene.path + ". Mobile Play keeps UnityChan_Player; VR Play uses XR Origin.");
        }

        /// <summary>
        /// Idempotent wiring for editor scene builders and the wire menu.
        /// </summary>
        public static bool TryWireScene(Scene scene, out string message)
        {
            message = null;

            if (!scene.IsValid())
            {
                message = "Scene is not valid.";
                return false;
            }

            if (!StarterAssetsSampleUtility.TryEnsureStarterAssetsImported(
                    XriPackageJsonPath,
                    ResolveXrRigPrefabAssetPath,
                    "[VR Project]"))
            {
                message =
                    "Import XR Interaction Toolkit \"Starter Assets\" sample (Package Manager), then run this menu again.";
                return false;
            }

            var rigPrefabPath = ResolveXrRigPrefabAssetPath();
            if (string.IsNullOrEmpty(rigPrefabPath))
            {
                message = "Could not find \"XR Origin (XR Rig).prefab\" under Starter Assets.";
                return false;
            }

            var systems = FindOrCreateSystems(scene);
            EnsureInteractionManager(systems);
            EnsureRigSelector(systems);

            var spawn = ResolveSpawnPosition(scene);
            var rigRoot = EnsureXrRig(scene, systems, rigPrefabPath, spawn);
            if (rigRoot == null)
            {
                message = "Failed to instantiate XR Origin (XR Rig).";
                return false;
            }

            rigRoot.SetActive(false);
            return true;
        }

        static GameObject FindOrCreateSystems(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Systems")
                    return root;
            }

            return new GameObject("Systems");
        }

        static void EnsureInteractionManager(GameObject systems)
        {
            if (systems.GetComponent<XRInteractionManager>() == null)
                systems.AddComponent<XRInteractionManager>();
        }

        static void EnsureRigSelector(GameObject systems)
        {
            if (systems.GetComponent<SuperhotPlaytestRigSelector>() == null)
                systems.AddComponent<SuperhotPlaytestRigSelector>();
        }

        static Vector3 ResolveSpawnPosition(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "UnityChan_Player")
                    return root.transform.position;
            }

            return new Vector3(0f, 0f, -6f);
        }

        static GameObject EnsureXrRig(Scene scene, GameObject systems, string prefabPath, Vector3 spawn)
        {
            var existing = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject rigRoot = null;
            XROrigin origin = null;

            if (existing.Length > 0)
            {
                origin = existing[0];
                rigRoot = origin.gameObject;
                rigRoot.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    return null;

                rigRoot = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (rigRoot == null)
                    return null;

                rigRoot.name = "XR Origin (XR Rig)";
                rigRoot.transform.SetPositionAndRotation(spawn, Quaternion.identity);

                foreach (var mgr in rigRoot.GetComponentsInChildren<XRInteractionManager>(true))
                    Object.DestroyImmediate(mgr);

                origin = rigRoot.GetComponent<XROrigin>();
            }

            WireGameplayDriver(systems, origin, rigRoot.transform);
            return rigRoot;
        }

        static void WireGameplayDriver(GameObject systems, XROrigin origin, Transform rigRoot)
        {
            var driver = systems.GetComponent<SuperhotGameplayDriver>();
            if (driver == null)
                driver = systems.AddComponent<SuperhotGameplayDriver>();

            var driverSo = new SerializedObject(driver);
            driverSo.FindProperty("_xrOrigin").objectReferenceValue = origin;
            driverSo.FindProperty("_hmd").objectReferenceValue =
                origin != null && origin.Camera != null ? origin.Camera.transform : null;
            driverSo.FindProperty("_leftController").objectReferenceValue =
                FindChildTransformByExactName(rigRoot, "Left Controller");
            driverSo.FindProperty("_rightController").objectReferenceValue =
                FindChildTransformByExactName(rigRoot, "Right Controller");
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
    }
}
#endif
