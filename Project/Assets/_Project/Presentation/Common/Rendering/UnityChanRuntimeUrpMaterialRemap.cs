using UnityEngine;

namespace VRProject.Presentation.Rendering
{
    /// <summary>
    /// Runs <see cref="UnityChanUrpMaterialRemapCore"/> on Awake so Unity-Chan is not magenta in Play Mode
    /// or in scenes that never ran the Editor menu remap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityChanRuntimeUrpMaterialRemap : MonoBehaviour
    {
        [SerializeField] bool _remapOnAwake = true;
        [Tooltip("If set, used instead of Shader.Find / Resources carrier (fixes stripped shaders in builds).")]
        [SerializeField] Shader _urpLitShaderOverride;
        [Tooltip("Second pass next frame; helps if materials were not ready in Awake.")]
        [SerializeField] bool _remapOnStart = true;

        void Awake()
        {
            if (_remapOnAwake)
                Remap();
        }

        void Start()
        {
            if (_remapOnStart)
                Remap();
        }

        void Remap()
        {
            UnityChanUrpMaterialRemapCore.RemapRenderersUnder(gameObject, _urpLitShaderOverride);
        }

#if UNITY_EDITOR
        [ContextMenu("Remap materials to URP Lit (this hierarchy)")]
        void RemapContextMenu()
        {
            Remap();
        }
#endif
    }
}
