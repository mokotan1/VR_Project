#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRProject.Presentation.Rendering;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Permanently rewrites Unity-Chan material assets to URP Lit (fixes magenta in Scene view without runtime remapper).
    /// </summary>
    public static class UnityChanUrpMaterialBakeMenu
    {
        /// <summary>Unity project path (under the folder that contains Assets/). Example disk path: D:\VR_Project\project\Assets\unity-chan!</summary>
        public const string UnityChanAssetsFolder = "Assets/unity-chan!";

        /// <summary>Try these folders first with FindAssets; if empty (Unity quirk), fall back to all materials filtered by path.</summary>
        static readonly string[] PreferredSearchRoots = { UnityChanAssetsFolder };

        static readonly string[] PathSubstrings =
        {
            "unity-chan!",
            "Unity-chan!",
            "unity-chan",
        };

        [MenuItem("VR Project/Unity-Chan/Bake All Materials To URP Lit (Assets)")]
        public static void BakeAllUnityChanMaterials()
        {
            UnityChanUrpMaterialRemapCore.InvalidateShaderCache();
            var urp = UnityChanUrpMaterialRemapCore.ResolveUrpLitShader();
            if (urp == null)
            {
                EditorUtility.DisplayDialog(
                    "VR Project",
                    "Could not resolve URP Lit shader. Open a scene with URP active or ensure package com.unity.render-pipelines.universal is installed.",
                    "OK");
                return;
            }

            var guids = GatherUnityChanMaterialGuids();
            var count = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsUnityChanMaterialPath(path))
                    continue;

                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                    continue;

                var temp = UnityChanUrpMaterialRemapCore.ConvertToUrpLit(mat, urp);
                if (ReferenceEquals(temp, mat))
                    continue;

                mat.shader = temp.shader;
                mat.CopyPropertiesFromMaterial(temp);
                UnityEngine.Object.DestroyImmediate(temp);
                EditorUtility.SetDirty(mat);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var msg = count > 0
                ? "Baked " + count + " Unity-Chan material(s) to URP Lit."
                : "No materials matched under " + UnityChanAssetsFolder +
                  " (disk: …\\Assets\\unity-chan!). Ensure that folder exists in the Project window or extend PreferredSearchRoots / PathSubstrings.";
            EditorUtility.DisplayDialog("VR Project", msg, "OK");
        }

        static IEnumerable<string> GatherUnityChanMaterialGuids()
        {
            var seen = new HashSet<string>();

            foreach (var root in PreferredSearchRoots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                    continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { root }))
                    seen.Add(guid);
            }

            if (seen.Count > 0)
                return seen;

            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsUnityChanMaterialPath(path))
                    seen.Add(guid);
            }

            return seen;
        }

        static bool IsUnityChanMaterialPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
            foreach (var sub in PathSubstrings)
            {
                if (assetPath.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
#endif
