using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public static class EnemyHitColorTintApplier
    {
        public static readonly Color DefaultHitTint = new(1f, 0.45f, 0.05f, 1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public static void ApplyTint(Renderer renderer, Color tint, MaterialPropertyBlock block)
        {
            if (renderer == null || block == null)
                return;

            block.Clear();
            var material = renderer.sharedMaterial;
            if (material == null)
                return;

            var applied = false;
            if (material.HasProperty(BaseColorId))
            {
                block.SetColor(BaseColorId, tint);
                applied = true;
            }
            else if (material.HasProperty(ColorId))
            {
                block.SetColor(ColorId, tint);
                applied = true;
            }

            if (material.HasProperty(EmissionColorId))
            {
                block.SetColor(EmissionColorId, tint * 1.8f);
                applied = true;
            }

            if (!applied)
                return;

            renderer.SetPropertyBlock(block);
        }

        public static void ClearTint(Renderer renderer)
        {
            if (renderer == null)
                return;

            renderer.SetPropertyBlock(null);
        }

        public static Color ReadTintFromBlock(Material material, MaterialPropertyBlock block)
        {
            if (material == null || block == null)
                return default;

            if (material.HasProperty(BaseColorId) && block.HasColor(BaseColorId))
                return block.GetColor(BaseColorId);
            if (material.HasProperty(ColorId) && block.HasColor(ColorId))
                return block.GetColor(ColorId);

            return default;
        }
    }
}
