using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class OsFpsInspiredDamageReceiverTests
    {
        [Test]
        public void FindInParents_ReturnsParentDamageReceiver_FromChildCollider()
        {
            var parent = new GameObject("DamageableParent");
            var damageable = parent.AddComponent<OsFpsInspiredDamageable>();
            var child = new GameObject("ChildCollider");
            child.transform.SetParent(parent.transform, false);
            var collider = child.AddComponent<BoxCollider>();

            var receiver = OsFpsInspiredDamageReceiver.FindInParents(collider);

            Assert.That(receiver, Is.SameAs(damageable));
        }

        [Test]
        public void FindInParents_ReturnsNull_WhenColliderMissing()
        {
            var receiver = OsFpsInspiredDamageReceiver.FindInParents(null);

            Assert.That(receiver, Is.Null);
        }
    }
}
