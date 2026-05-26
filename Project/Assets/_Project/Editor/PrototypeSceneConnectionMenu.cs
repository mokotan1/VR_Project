#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace VRProject.EditorTools
{
    /// <summary>
    /// One-shot prototype scene connector for the current vertical slice.
    /// Use this while prototyping to generate the startup mode-selection
    /// scene, generate the crystal-defense scene, and enforce build order.
    /// </summary>
    public static class PrototypeSceneConnectionMenu
    {
        const string StartupScenePath = "Assets/Scenes/Startup.unity";
        const string CrystalDefenseScenePath = "Assets/Scenes/CrystalDefensePrototype.unity";

        [MenuItem("VR Project/Scenes/Connect Prototype Startup Flow")]
        public static void ConnectPrototypeStartupFlow()
        {
            StartupSceneMenu.CreateStartupScene();
            SuperhotPrototypeSceneMenu.CreateCrystalDefensePrototypeScene();
            EditorApplication.delayCall += EnsureBuildOrderAfterDeferredSceneCreation;
            UnityEngine.Debug.Log("[VR Project] Prototype startup flow generation requested. Build order will be checked after deferred scene creation.");
        }

        [MenuItem("VR Project/Scenes/Ensure Prototype Build Order")]
        public static void EnsureBuildOrder()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            RemoveScene(scenes, StartupScenePath);
            RemoveScene(scenes, CrystalDefenseScenePath);

            var insertIndex = 0;
            if (SceneAssetExists(StartupScenePath))
                scenes.Insert(insertIndex++, new EditorBuildSettingsScene(StartupScenePath, true));
            if (SceneAssetExists(CrystalDefenseScenePath))
                scenes.Insert(insertIndex, new EditorBuildSettingsScene(CrystalDefenseScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void RemoveScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            scenes.RemoveAll(scene => scene.path == path);
        }

        static void EnsureBuildOrderAfterDeferredSceneCreation()
        {
            EditorApplication.delayCall -= EnsureBuildOrderAfterDeferredSceneCreation;
            EnsureBuildOrder();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[VR Project] Prototype startup flow connected. Build order: Startup -> CrystalDefensePrototype when both scenes exist.");
        }

        static bool SceneAssetExists(string path)
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path));
        }
    }
}
#endif
