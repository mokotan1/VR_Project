// SC Post Effects — URP 6+ render graph bridge (internal)
using System;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace SCPE.Internal
{
    /// <summary>
    /// Retrieves <see cref="ScriptableRenderContext"/> from <see cref="UnsafeGraphContext"/> for legacy command-buffer paths.
    /// URP does not expose this on the public API; fields are stable across 6000.4 + matching Core RP versions.
    /// </summary>
    internal static class SCPERGBridge
    {
        static FieldInfo s_wrappedField;
        static FieldInfo s_renderContextField;

        public static ScriptableRenderContext GetScriptableRenderContext(UnsafeGraphContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            if (s_wrappedField == null)
            {
                s_wrappedField = typeof(UnsafeGraphContext).GetField(
                    "wrappedContext",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                s_renderContextField = typeof(InternalRenderGraphContext).GetField(
                    "renderContext",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }

            object wrapped = s_wrappedField?.GetValue(ctx);
            if (wrapped == null)
                return default;

            return (ScriptableRenderContext)s_renderContextField.GetValue(wrapped);
        }
    }
}
