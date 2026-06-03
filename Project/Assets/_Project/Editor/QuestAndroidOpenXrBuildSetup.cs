#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

#if UNITY_XR_OPENXR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VRProject.EditorTools
{
    /// <summary>
    /// Wires Android XR Plug-in Management to OpenXR (Quest) and bumps Min SDK for Meta devices.
    /// Run from menu if Unity was closed when assets were added, or let auto-setup run once on domain reload.
    /// </summary>
    static class QuestAndroidOpenXrBuildSetup
    {
        const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        const string MetaFeatureSetId = "com.unity.openxr.featureset.meta";
        const AndroidSdkVersions QuestMinAndroidApi = (AndroidSdkVersions)29;
        const string SessionKey = "VRProject.QuestOpenXR.Configured";

        [MenuItem("VR Project/Setup/Configure Android OpenXR (Quest)")]
        public static void ConfigureFromMenu()
        {
            if (!Configure(log: true))
                EditorUtility.DisplayDialog(
                    "Quest OpenXR setup",
                    "Setup did not complete. Check the Console for errors (missing OpenXR package, etc.).",
                    "OK");
        }

        [InitializeOnLoadMethod]
        static void AutoConfigureOnceIfNeeded()
        {
            // Keep regular Android phone/tablet builds from bundling OpenXR.
            // Run the menu item explicitly before making a Quest headset APK.
            return;

#pragma warning disable CS0162
            if (SessionState.GetBool(SessionKey, false))
                return;

            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionKey, false))
                    return;

                if (!NeedsAndroidOpenXrSetup())
                {
                    SessionState.SetBool(SessionKey, true);
                    return;
                }

                if (Configure(log: true))
                    SessionState.SetBool(SessionKey, true);
            };
#pragma warning restore CS0162
        }

        static bool NeedsAndroidOpenXrSetup()
        {
            if (!TryGetPerBuildTarget(out var perBuild))
                return true;

            if (!perBuild.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                return true;

            var android = perBuild.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (android == null)
                return true;

            var manager = android.Manager;
            if (manager == null || manager.activeLoaders == null || manager.activeLoaders.Count == 0)
                return true;

            return !manager.activeLoaders.Any(loader =>
                loader != null && loader.GetType().FullName == OpenXrLoaderTypeName);
        }

        static bool TryGetPerBuildTarget(out XRGeneralSettingsPerBuildTarget perBuild)
        {
            perBuild = null;
            return EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perBuild)
                && perBuild != null;
        }

        static bool Configure(bool log)
        {
            if (!TryGetPerBuildTarget(out var perBuild))
            {
                if (log)
                    Debug.LogError("[VR Project] XRGeneralSettingsPerBuildTarget not found in EditorBuildSettings.");
                return false;
            }

            if (!perBuild.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                perBuild.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);

            var android = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (android == null)
            {
                android = perBuild.SettingsForBuildTarget(BuildTargetGroup.Android);
            }

            if (android == null)
            {
                if (log)
                    Debug.LogError("[VR Project] Could not create Android XRGeneralSettings.");
                return false;
            }

            android.InitManagerOnStart = false;

            if (android.Manager == null)
                perBuild.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var manager = android.Manager;
            if (manager == null)
            {
                if (log)
                    Debug.LogError("[VR Project] Android XRManagerSettings is null.");
                return false;
            }

            manager.automaticLoading = false;
            manager.automaticRunning = false;

            var assigned = XRPackageMetadataStore.AssignLoader(
                manager,
                OpenXrLoaderTypeName,
                BuildTargetGroup.Android);

            if (!assigned && log)
                Debug.LogWarning("[VR Project] OpenXR loader assign returned false (may already be assigned).");

#if UNITY_XR_OPENXR
            EnableMetaQuestFeatureSet(log);
#else
            if (log)
                Debug.LogWarning("[VR Project] OpenXR editor assembly not available; enable Meta feature set in Project Settings > OpenXR.");
#endif

            ApplyAndroidPlayerSettings(log);

            EditorUtility.SetDirty(perBuild);
            EditorUtility.SetDirty(android);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();

            if (log)
                Debug.Log("[VR Project] Android OpenXR (Quest) build setup applied with manual XR startup. Rebuild APK and install on headset.");

            return true;
        }

#if UNITY_XR_OPENXR
        static void EnableMetaQuestFeatureSet(bool log)
        {
            OpenXRFeatureSetManager.InitializeFeatureSets();

            var featureSets = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(BuildTargetGroup.Android);
            if (featureSets == null || featureSets.Count == 0)
            {
                if (log)
                    Debug.LogWarning("[VR Project] No OpenXR feature sets for Android; open Project Settings > OpenXR once.");
                return;
            }

            var enabledAny = false;
            foreach (var featureSet in featureSets)
            {
                if (featureSet.featureSetId != MetaFeatureSetId)
                    continue;

                featureSet.isEnabled = true;
                enabledAny = true;
            }

            if (!enabledAny)
            {
                if (log)
                    Debug.LogWarning("[VR Project] Meta feature set id not found: " + MetaFeatureSetId);
                return;
            }

            OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.Android);
        }
#endif

        static void ApplyAndroidPlayerSettings(bool log)
        {
            if ((int)PlayerSettings.Android.minSdkVersion < (int)QuestMinAndroidApi)
                PlayerSettings.Android.minSdkVersion = QuestMinAndroidApi;

            if (log)
                Debug.Log("[VR Project] Android minSdkVersion = " + PlayerSettings.Android.minSdkVersion);
        }
    }
}
#endif
