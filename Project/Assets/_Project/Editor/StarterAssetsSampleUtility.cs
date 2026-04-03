#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Imports XR Interaction Toolkit "Starter Assets" without referencing <c>UnityEditor.PackageManager.UI.Sample</c> at compile time.
    /// That type can trigger Package Manager UI <c>ServicesContainer</c> during domain reload and throw
    /// "ScriptableSingleton already exists. Did you query the singleton in a constructor?".
    /// </summary>
    internal static class StarterAssetsSampleUtility
    {
        const string StarterAssetsDisplayName = "Starter Assets";

        internal static bool TryEnsureStarterAssetsImported(
            string packageJsonPath,
            Func<string> resolveRigPrefabPath,
            string logPrefix)
        {
            if (!string.IsNullOrEmpty(resolveRigPrefabPath()))
                return true;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageJsonPath);
            if (packageInfo == null)
            {
                Debug.LogError($"{logPrefix} com.unity.xr.interaction.toolkit package.json not found.");
                return false;
            }

            if (!TryFindStarterAssetsSample(packageInfo, out var sampleObject))
            {
                Debug.LogError($"{logPrefix} Starter Assets sample not found in XR Interaction Toolkit package.");
                return false;
            }

            var sampleType = sampleObject.GetType();
            var isImportedProp = sampleType.GetProperty("isImported", BindingFlags.Public | BindingFlags.Instance);
            var isImported = isImportedProp != null && (bool)isImportedProp.GetValue(sampleObject);

            if (!isImported)
            {
                if (!EditorUtility.DisplayDialog(
                    "Import Starter Assets",
                    "The XR Interaction Toolkit \"Starter Assets\" sample is required for the VR rig prefab. Import it now?",
                    "Import",
                    "Cancel"))
                    return false;

                if (!TryImportSample(sampleObject, sampleType))
                    return false;

                AssetDatabase.Refresh();
            }

            if (!string.IsNullOrEmpty(resolveRigPrefabPath()))
                return true;

            Debug.LogWarning($"{logPrefix} Starter Assets reported imported but rig prefab missing; re-importing sample.");
            if (!TryFindStarterAssetsSample(packageInfo, out sampleObject))
                return false;
            if (!TryImportSample(sampleObject, sampleType))
                return false;
            AssetDatabase.Refresh();
            return !string.IsNullOrEmpty(resolveRigPrefabPath());
        }

        static bool TryFindStarterAssetsSample(UnityEditor.PackageManager.PackageInfo packageInfo, out object sampleObject)
        {
            sampleObject = null;
            var sampleType = ResolveSampleType();
            if (sampleType == null)
            {
                Debug.LogError("[VR Project] Could not resolve UnityEditor.PackageManager.UI.Sample type.");
                return false;
            }

            var findByPackage = sampleType.GetMethod(
                "FindByPackage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);
            if (findByPackage == null)
            {
                Debug.LogError("[VR Project] Sample.FindByPackage(string, string) not found.");
                return false;
            }

            object result;
            try
            {
                result = findByPackage.Invoke(null, new object[] { packageInfo.name, packageInfo.version });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
                return false;
            }

            if (result is IEnumerable enumerable)
            {
                foreach (var s in enumerable)
                {
                    if (s == null)
                        continue;
                    var displayNameProp = s.GetType().GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance);
                    var name = displayNameProp?.GetValue(s) as string;
                    if (name == StarterAssetsDisplayName)
                    {
                        sampleObject = s;
                        return true;
                    }
                }
            }

            return false;
        }

        static bool TryImportSample(object sampleObject, Type sampleType)
        {
            var importOptionsType = sampleType.GetNestedType("ImportOptions", BindingFlags.Public);
            if (importOptionsType == null || !importOptionsType.IsEnum)
            {
                Debug.LogError("[VR Project] Sample.ImportOptions enum not found.");
                return false;
            }

            object overrideOption;
            try
            {
                overrideOption = Enum.Parse(importOptionsType, "OverridePreviousImports");
            }
            catch
            {
                Debug.LogError("[VR Project] Sample.ImportOptions.OverridePreviousImports not found.");
                return false;
            }

            var importMethod = sampleType.GetMethod(
                "Import",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { importOptionsType },
                null);
            if (importMethod == null)
            {
                Debug.LogError("[VR Project] Sample.Import(ImportOptions) not found.");
                return false;
            }

            try
            {
                importMethod.Invoke(sampleObject, new[] { overrideOption });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
                return false;
            }

            return true;
        }

        static Type ResolveSampleType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null || !asm.FullName.StartsWith("UnityEditor", StringComparison.Ordinal))
                    continue;
                Type t = null;
                try
                {
                    t = asm.GetType("UnityEditor.PackageManager.UI.Sample");
                }
                catch
                {
                    // ignore bad assemblies
                }

                if (t != null)
                    return t;
            }

            return null;
        }
    }
}
#endif
