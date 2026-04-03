using UnityEngine;

namespace VRProject.Presentation.Rendering
{
    /// <summary>
    /// Unity-Chan uses Built-in custom shaders; in URP they become missing → magenta.
    /// Remaps to URP Lit (textures from _MainTex / _NormalMapSampler, etc.).
    /// See: Unity discussions "Convert UnityChan to URP", Unity Toon Shader (UTS) for closer anime look.
    /// </summary>
    public static class UnityChanUrpMaterialRemapCore
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        static readonly string[] UrpLitShaderFallbackNames =
        {
            UrpLitShaderName,
            "Universal Render Pipeline/Simple Lit",
        };

        const string UrpLitCarrierResourcePath = "VRProject/UrpLitCarrier";

        static Shader _cachedResolvedShader;
        static Material _carrierFromResources;

        /// <summary>Clears cached shader (e.g. after graphics settings change in Editor).</summary>
        public static void InvalidateShaderCache()
        {
            _cachedResolvedShader = null;
            _carrierFromResources = null;
        }

        /// <summary>
        /// Resolves URP Lit: Shader.Find first, then Resources material that references Lit (keeps shader in builds).
        /// </summary>
        public static Shader ResolveUrpLitShader()
        {
            if (_cachedResolvedShader != null)
                return _cachedResolvedShader;

            foreach (var name in UrpLitShaderFallbackNames)
            {
                var s = Shader.Find(name);
                if (s != null)
                {
                    _cachedResolvedShader = s;
                    return s;
                }
            }

            if (_carrierFromResources == null)
                _carrierFromResources = Resources.Load<Material>(UrpLitCarrierResourcePath);
            if (_carrierFromResources != null && _carrierFromResources.shader != null)
            {
                _cachedResolvedShader = _carrierFromResources.shader;
                return _cachedResolvedShader;
            }

            return null;
        }

        public static void RemapRenderersUnder(GameObject root, Shader urpLitOverride = null)
        {
            if (root == null)
                return;

            var urpLit = urpLitOverride != null ? urpLitOverride : ResolveUrpLitShader();
            if (urpLit == null)
            {
                Debug.LogWarning("[VR Project] No URP Lit shader resolved. Add URP, place Resources/" + UrpLitCarrierResourcePath +
                                 ".mat, or assign a shader on UnityChanRuntimeUrpMaterialRemap.");
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
                    if (m == null)
                    {
                        copy[i] = CreateFallbackUrpLit(urpLit);
                        replaced = true;
                        continue;
                    }

                    copy[i] = ConvertToUrpLit(m, urpLit);
                    if (copy[i] != m)
                        replaced = true;
                }

                if (replaced)
                    renderer.sharedMaterials = copy;
            }
        }

        public static Material ConvertToUrpLit(Material src, Shader urpLit)
        {
            if (src == null || urpLit == null)
                return null;

            if (IsAlreadyUrpCompatible(src))
                return src;

            var dst = new Material(urpLit);

            var main = GetAlbedoTexture(src);
            if (main != null)
                dst.SetTexture("_BaseMap", main);

            if (src.HasProperty("_Color"))
                dst.SetColor("_BaseColor", src.GetColor("_Color"));
            else if (src.HasProperty("_BaseColor"))
                dst.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            else
                dst.SetColor("_BaseColor", Color.white);

            var bump = GetNormalTexture(src);
            if (bump != null)
            {
                dst.SetTexture("_BumpMap", bump);
                dst.EnableKeyword("_NORMALMAP");
            }

            dst.SetFloat("_Smoothness", 0.35f);
            dst.SetFloat("_Metallic", 0f);

            var sn = src.shader != null ? src.shader.name : string.Empty;
            var needsCutout = sn.IndexOf("eyelash", System.StringComparison.OrdinalIgnoreCase) >= 0
                              || sn.IndexOf("eyeline", System.StringComparison.OrdinalIgnoreCase) >= 0
                              || MaterialNameSuggestCutout(src.name);

            if (needsCutout)
            {
                dst.SetFloat("_AlphaClip", 1f);
                dst.SetFloat("_Cutoff", 0.35f);
                dst.EnableKeyword("_ALPHATEST_ON");
                dst.SetFloat("_Surface", 0f);
            }
            else
                dst.SetFloat("_Surface", 0f);

            dst.name = src.name + " (URP Lit)";
            return dst;
        }

        static bool MaterialNameSuggestCutout(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return false;
            var n = materialName.ToLowerInvariant();
            return n.Contains("eyelash") || n.Contains("eyeline");
        }

        static bool IsAlreadyUrpCompatible(Material src)
        {
            if (src.shader == null)
                return false;
            var n = src.shader.name;
            if (string.IsNullOrEmpty(n))
                return false;
            if (n.IndexOf("InternalError", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Universal Render Pipeline", System.StringComparison.Ordinal) >= 0)
                return true;
            if (n.IndexOf("HDRP/", System.StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }

        /// <summary>URP Lit with flat gray — avoids magenta when a renderer slot has no material.</summary>
        static Material CreateFallbackUrpLit(Shader urpLit)
        {
            var m = new Material(urpLit)
            {
                name = "FallbackNullSlot (URP Lit)"
            };
            m.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.48f));
            m.SetFloat("_Smoothness", 0.35f);
            m.SetFloat("_Metallic", 0f);
            return m;
        }

        /// <summary>Works even when the source shader is missing (pink material): try common slot names.</summary>
        static Texture GetAlbedoTexture(Material src)
        {
            if (src == null)
                return null;
            if (src.HasProperty("_MainTex"))
            {
                var t = src.GetTexture("_MainTex");
                if (t != null)
                    return t;
            }

            if (src.HasProperty("_BaseMap"))
            {
                var t = src.GetTexture("_BaseMap");
                if (t != null)
                    return t;
            }

            return null;
        }

        static Texture GetNormalTexture(Material src)
        {
            if (src == null)
                return null;
            if (src.HasProperty("_NormalMapSampler"))
            {
                var t = src.GetTexture("_NormalMapSampler");
                if (t != null)
                    return t;
            }

            if (src.HasProperty("_BumpMap"))
            {
                var t = src.GetTexture("_BumpMap");
                if (t != null)
                    return t;
            }

            return null;
        }
    }
}
