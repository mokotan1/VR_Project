#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    public static class XrDeviceSimulatorSetupMenu
    {
        const string GameplayScenePath = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity";
        const string StartupScenePath = "Assets/Scenes/Startup.unity";
        const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string XrDeviceSimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/XR Device Simulator/XR Device Simulator.prefab";
        const string XrDeviceSimulatorSettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

        [MenuItem("VR Project/XR/Ensure XR Device Simulator For Unity-Chan Prototype")]
        public static void EnsureForUnityChanPrototype()
        {
            var simulatorPrefab = LoadPrefab(XrDeviceSimulatorPrefabPath);
            if (simulatorPrefab == null)
                return;

            EnsureXriProjectSettings(simulatorPrefab);
            EnsureGameplayScene(simulatorPrefab);
            EnsureStartupBuildOrder();

            Debug.Log("[VR Project] XR Device Simulator ensured for UnityChanPrototypeFps and Startup build flow.");
        }

        static void EnsureGameplayScene(GameObject simulatorPrefab)
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            var xrOrigin = FindFirstComponentByTypeName("Unity.XR.CoreUtils.XROrigin");
            var xrOriginTransform = xrOrigin != null
                ? xrOrigin.transform
                : InstantiateXrOrigin()?.transform;

            EnsureSimulatorInstance(simulatorPrefab, xrOriginTransform);
            EnsureRigSelector();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static GameObject InstantiateXrOrigin()
        {
            var prefab = LoadPrefab(XrOriginPrefabPath);
            if (prefab == null)
                return null;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;

            instance.name = "XR Origin (XR Rig)";
            instance.transform.position = new Vector3(0f, 0f, -6f);
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(false);
            return instance;
        }

        static void EnsureSimulatorInstance(GameObject simulatorPrefab, Transform parent)
        {
            var existing = FindSceneGameObject("XR Device Simulator");
            if (existing != null)
            {
                if (parent != null && existing.transform.parent == null && !PrefabUtility.IsPartOfPrefabAsset(existing))
                    existing.transform.SetParent(parent, false);
                existing.SetActive(true);
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(simulatorPrefab, parent) as GameObject;
            if (instance == null)
                return;

            instance.name = "XR Device Simulator";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        static GameObject FindSceneGameObject(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!string.Equals(go.name, name, StringComparison.Ordinal))
                    continue;
                if (EditorUtility.IsPersistent(go))
                    continue;
                if (!go.scene.IsValid())
                    continue;
                return go;
            }

            return null;
        }

        static void EnsureRigSelector()
        {
            if (UnityEngine.Object.FindAnyObjectByType<SuperhotPlaytestRigSelector>(FindObjectsInactive.Include) != null)
                return;

            var systems = GameObject.Find("Systems") ?? new GameObject("Systems");
            systems.AddComponent<SuperhotPlaytestRigSelector>();
        }

        static void EnsureXriProjectSettings(GameObject simulatorPrefab)
        {
            var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(XrDeviceSimulatorSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning("[VR Project] XR Device Simulator settings asset missing: " + XrDeviceSimulatorSettingsPath);
                return;
            }

            var so = new SerializedObject(settings);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = simulatorPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        static void EnsureStartupBuildOrder()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            RemoveScene(scenes, StartupScenePath);
            RemoveScene(scenes, GameplayScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(StartupScenePath, true));
            scenes.Insert(1, new EditorBuildSettingsScene(GameplayScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void RemoveScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            scenes.RemoveAll(scene => string.Equals(scene.path, path, StringComparison.OrdinalIgnoreCase));
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                Debug.LogError("[VR Project] Missing prefab: " + path);
            return prefab;
        }

        static Component FindFirstComponentByTypeName(string fullName)
        {
            var type = ResolveType(fullName);
            if (type == null)
                return null;

            var objects = UnityEngine.Object.FindObjectsByType(
                type,
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            return objects.Length > 0 ? objects[0] as Component : null;
        }

        static Type ResolveType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
#endif
