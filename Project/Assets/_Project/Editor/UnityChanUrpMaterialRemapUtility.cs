#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRProject.Presentation.Rendering;

namespace VRProject.EditorTools
{
    public static class UnityChanUrpMaterialRemapUtility
    {
        [MenuItem("VR Project/Unity-Chan/Remap Materials To URP Lit (Selection)")]
        static void RemapSelection()
        {
            foreach (var go in Selection.gameObjects)
                RemapRenderersUnder(go);
        }

        public static void RemapRenderersUnder(GameObject root)
        {
            if (root == null)
                return;

            UnityChanUrpMaterialRemapCore.RemapRenderersUnder(root);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                EditorUtility.SetDirty(renderer);
                if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            EditorUtility.SetDirty(root);
        }
    }
}
#endif