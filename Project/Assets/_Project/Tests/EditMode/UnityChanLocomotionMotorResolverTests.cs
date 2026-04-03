using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode
{
    public sealed class UnityChanLocomotionMotorResolverTests
    {
        [Test]
        public void ResolveOn_NullRoot_ReturnsNull()
        {
            Assert.That(UnityChanLocomotionMotorResolver.ResolveOn(null), Is.Null);
        }

        [Test]
        public void ResolveOn_NoMotorComponents_ReturnsNull()
        {
            var go = new GameObject("MotorLess");
            try
            {
                Assert.That(UnityChanLocomotionMotorResolver.ResolveOn(go), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
