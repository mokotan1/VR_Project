#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VRProject.EditorTools
{
    public static class UnityChanUrpMaterialRemapUtility
    {
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";

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

            var urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                Debug.LogWarning("[VR Project] Shader not found: " + UrpLitShaderName);
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                if (shared == null || shared.Length == 0)
                    continue;

                var replaced = false;
                var copy = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    copy[i] = m != null ? ConvertToUrpLit(m, urpLit) : null;
                    if (copy[i] != m)
                        replaced = true;
                }

                if (replaced)
                    renderer.sharedMaterials = copy;
            }
        }

        static Material ConvertToUrpLit(Material src, Shader urpLit)
        {
            if (src == null)
                return null;

            if (src.shader != null && src.shader.name != null &&
                src.shader.name.IndexOf("Universal Render Pipeline", System.StringComparison.Ordinal) >= 0)
                return src;

            var dst = new Material(urpLit);

            if (src.HasProperty("_MainTex"))
            {
                var t = src.GetTexture("_MainTex");
                if (t != null)
                    dst.SetTexture("_BaseMap", t);
            }

            if (src.HasProperty("_Color"))
                dst.SetColor("_BaseColor", src.GetColor("_Color"));
            else
                dst.SetColor("_BaseColor", Color.white);

            Texture bump = null;
            if (src.HasProperty("_NormalMapSampler"))
                bump = src.GetTexture("_NormalMapSampler");
            if (bump == null && src.HasProperty("_BumpMap"))
                bump = src.GetTexture("_BumpMap");

            if (bump != null)
            {
                dst.SetTexture("_BumpMap", bump);
                dst.EnableKeyword("_NORMALMAP");
            }

            dst.SetFloat("_Smoothness", 0.35f);
            dst.SetFloat("_Metallic", 0f);

            var sn = src.shader != null ? src.shader.name : string.Empty;
            var needsCutout = sn.IndexOf("eyelash", System.StringComparison.OrdinalIgnoreCase) >= 0
                              || sn.IndexOf("eyeline", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (needsCutout)
            {
                dst.SetFloat("_AlphaClip", 1f);
                dst.SetFloat("_Cutoff", 0.35f);
                dst.EnableKeyword("_ALPHATEST_ON");
                dst.SetFloat("_Surface", 0f);
            }
            else
            {
                dst.SetFloat("_Surface", 0f);
            }

            dst.name = src.name + " (URP Lit)";
            return dst;
        }
    }
}
#endif