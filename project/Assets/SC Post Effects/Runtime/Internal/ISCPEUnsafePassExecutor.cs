using UnityEngine.Rendering.RenderGraphModule;

namespace SCPE
{
    /// <summary>
    /// Implemented by <see cref="PostEffectRenderer{T}"/> so a non-generic render-graph payload can invoke the effect.
    /// </summary>
    internal interface ISCPEUnsafePassExecutor
    {
        void RunUnsafeRenderGraphPass(UnsafeGraphContext ctx);
    }

    internal sealed class SCPERGPassPayload
    {
        public ISCPEUnsafePassExecutor Executor;
    }
}
