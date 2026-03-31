#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Ensures <see cref="XRGeneralSettings.k_SettingsKey"/> is registered in <see cref="EditorBuildSettings"/>
    /// so XR code uses <c>TryGetConfigObject</c> instead of <c>AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")</c>.
    /// That scan can exhaust memory when Project Validation / XR Project Settings repaints on large projects (Unity 6000.x).
    /// </summary>
    [InitializeOnLoad]
    internal static class XrLoaderSettingsBuildSettingsBootstrap
    {
        const string DefaultAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        static XrLoaderSettingsBuildSettingsBootstrap()
        {
            EditorApplication.delayCall += RunOnceAfterLoad;
        }

        static void RunOnceAfterLoad()
        {
            EditorApplication.delayCall -= RunOnceAfterLoad;
            try
            {
                EnsureRegistered();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VR Project] XR loader settings bootstrap failed: {e.Message}");
            }
        }

        internal static void EnsureRegistered()
        {
            if (EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(
                    XRGeneralSettings.k_SettingsKey, out var configured) &&
                configured != null)
                return;

            var atPath = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(DefaultAssetPath);
            if (atPath != null)
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, atPath, true);
                return;
            }

            var created = CreatePerBuildTargetWithoutFindAssets();
            if (created != null)
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, created, true);
        }

        /// <summary>
        /// Calls internal <c>CreateAssetSynchronized</c> so we never hit <c>FindAssets</c> when no asset exists yet.
        /// </summary>
        static XRGeneralSettingsPerBuildTarget CreatePerBuildTargetWithoutFindAssets()
        {
            var method = typeof(XRGeneralSettingsPerBuildTarget).GetMethod(
                "CreateAssetSynchronized",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError(
                    "[VR Project] Unity XR Management API changed: CreateAssetSynchronized not found. Update this bootstrap or register loader_settings manually.");
                return null;
            }

            return method.Invoke(null, null) as XRGeneralSettingsPerBuildTarget;
        }

        [MenuItem("VR Project/XR/Register loader settings in EditorBuildSettings (OOM workaround)")]
        static void MenuRun()
        {
            EnsureRegistered();
            AssetDatabase.SaveAssets();
            Debug.Log("[VR Project] XR loader settings registration attempted. Check EditorBuildSettings and Assets/XR/XRGeneralSettingsPerBuildTarget.asset.");
        }
    }
}
#endif
