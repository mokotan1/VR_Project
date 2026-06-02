using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class EnemyHitColorTintApplierTests
    {
        [Test]
        public void DefaultHitTint_IsOrange()
        {
            var tint = EnemyHitColorTintApplier.DefaultHitTint;
            Assert.Greater(tint.r, tint.g);
            Assert.Greater(tint.g, tint.b);
            Assert.AreEqual(1f, tint.a, 1e-4f);
        }

        [Test]
        public void ApplyTint_WritesColorToPropertyBlock()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var renderer = go.GetComponent<Renderer>();
                var writeBlock = new MaterialPropertyBlock();
                EnemyHitColorTintApplier.ApplyTint(renderer, EnemyHitColorTintApplier.DefaultHitTint, writeBlock);

                var readBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(readBlock);
                var readColor = EnemyHitColorTintApplier.ReadTintFromBlock(renderer.sharedMaterial, readBlock);

                Assert.AreEqual(EnemyHitColorTintApplier.DefaultHitTint, readColor);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ClearTint_RemovesPropertyBlockOverride()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var renderer = go.GetComponent<Renderer>();
                var block = new MaterialPropertyBlock();
                EnemyHitColorTintApplier.ApplyTint(renderer, EnemyHitColorTintApplier.DefaultHitTint, block);

                EnemyHitColorTintApplier.ClearTint(renderer);

                var readBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(readBlock);
                Assert.IsTrue(readBlock.isEmpty);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
