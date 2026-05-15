using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.VR;

namespace VRProject.Tests.EditMode
{
    public sealed class VrRigAnchorSetTests
    {
        [Test]
        public void AutoBindMissingReferences_BindsNamedChildAnchors()
        {
            var root = new GameObject("VRRigRoot");
            var head = new GameObject(VrRigAnchorNames.HeadAnchor);
            var right = new GameObject(VrRigAnchorNames.RightHandAnchor);
            var left = new GameObject(VrRigAnchorNames.LeftHandAnchor);
            head.transform.SetParent(root.transform, false);
            right.transform.SetParent(root.transform, false);
            left.transform.SetParent(root.transform, false);
            var anchors = root.AddComponent<VrRigAnchorSet>();

            anchors.AutoBindMissingReferences();

            Assert.That(anchors.HeadAnchor, Is.SameAs(head.transform));
            Assert.That(anchors.RightHandAnchor, Is.SameAs(right.transform));
            Assert.That(anchors.LeftHandAnchor, Is.SameAs(left.transform));
            Assert.That(anchors.HasRequiredAnchors, Is.True);
        }
    }
}
