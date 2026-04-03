using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class OsFpsInspiredHitscanExclusionTests
    {
        [Test]
        public void ResolveExclusionRoot_PrefersCharacterControllerAncestor()
        {
            var root = new GameObject("Root");
            root.AddComponent<CharacterController>();
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform, false);
            var host = child.AddComponent<BoxCollider>();

            var resolved = OsFpsInspiredHitscanExclusion.ResolveExclusionRoot(host);
            Assert.That(resolved, Is.SameAs(root.transform));
        }

        [Test]
        public void ResolveExclusionRoot_FallbacksToHostTransform_WithoutCharacterController()
        {
            var go = new GameObject("Solo");
            var host = go.AddComponent<BoxCollider>();

            var resolved = OsFpsInspiredHitscanExclusion.ResolveExclusionRoot(host);
            Assert.That(resolved, Is.SameAs(go.transform));
        }

        [Test]
        public void IsColliderUnderExclusionRoot_True_ForRootAndDescendants()
        {
            var root = new GameObject("Root");
            var arm = new GameObject("Arm");
            arm.transform.SetParent(root.transform, false);
            var col = arm.AddComponent<SphereCollider>();

            Assert.That(OsFpsInspiredHitscanExclusion.IsColliderUnderExclusionRoot(col, root.transform), Is.True);
        }

        [Test]
        public void IsColliderUnderExclusionRoot_False_ForSibling()
        {
            var root = new GameObject("Root");
            var a = new GameObject("A");
            a.transform.SetParent(root.transform, false);
            var b = new GameObject("B");
            b.transform.SetParent(root.transform, false);
            var col = b.AddComponent<SphereCollider>();

            Assert.That(OsFpsInspiredHitscanExclusion.IsColliderUnderExclusionRoot(col, a.transform), Is.False);
        }

        [Test]
        public void BuildBiasedAimRay_ShortensCastDistance_ByOffset()
        {
            var ray = new Ray(Vector3.zero, Vector3.forward);
            var biased = OsFpsInspiredHitscanExclusion.BuildBiasedAimRay(ray, 0.1f, 100f, out var dist);
            Assert.That(biased.origin, Is.EqualTo(new Vector3(0f, 0f, 0.1f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(dist, Is.EqualTo(99.9f).Within(0.001f));
            Assert.That(biased.direction, Is.EqualTo(Vector3.forward).Using(Vector3EqualityComparer.Instance));
        }

        sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            public static readonly Vector3EqualityComparer Instance = new();
            const float Eps = 0.0001f;

            public bool Equals(Vector3 x, Vector3 y) =>
                Mathf.Abs(x.x - y.x) < Eps && Mathf.Abs(x.y - y.y) < Eps && Mathf.Abs(x.z - y.z) < Eps;

            public int GetHashCode(Vector3 obj) => obj.GetHashCode();
        }
    }
}
