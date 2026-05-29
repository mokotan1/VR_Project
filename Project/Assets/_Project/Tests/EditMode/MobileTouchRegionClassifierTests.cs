using NUnit.Framework;
using VRProject.Application.Mobile;

namespace VRProject.Tests.EditMode
{
    public sealed class MobileTouchRegionClassifierTests
    {
        static readonly MobileTouchLayoutRects Layout = MobileTouchLayoutRects.LandscapeTabletDefault;

        [Test]
        public void Classify_MoveJoystickRegion()
        {
            var kind = MobileTouchRegionClassifier.Classify(0.15f, 0.2f, Layout);
            Assert.AreEqual(MobileTouchRegionKind.MoveJoystick, kind);
        }

        [Test]
        public void Classify_LookRegion()
        {
            var kind = MobileTouchRegionClassifier.Classify(0.7f, 0.6f, Layout);
            Assert.AreEqual(MobileTouchRegionKind.Look, kind);
        }

        [Test]
        public void Classify_MeleeRegion()
        {
            var kind = MobileTouchRegionClassifier.Classify(0.55f, 0.55f, Layout);
            Assert.AreEqual(MobileTouchRegionKind.MeleeSwing, kind);
        }

        [Test]
        public void Classify_FireButtonTakesPriorityOverLook()
        {
            var kind = MobileTouchRegionClassifier.Classify(0.88f, 0.12f, Layout);
            Assert.AreEqual(MobileTouchRegionKind.FireButton, kind);
        }

        [Test]
        public void Classify_Outside_ReturnsNone()
        {
            var kind = MobileTouchRegionClassifier.Classify(0.32f, 0.12f, Layout);
            Assert.AreEqual(MobileTouchRegionKind.None, kind);
        }
    }
}
