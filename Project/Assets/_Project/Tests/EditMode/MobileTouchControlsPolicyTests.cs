using NUnit.Framework;
using VRProject.Application.Mobile;

namespace VRProject.Tests.EditMode
{
    public sealed class MobileTouchControlsPolicyTests
    {
        [Test]
        public void UsesMobile_WhenTouchscreenPresent()
        {
            Assert.IsTrue(MobileTouchControlsPolicy.ShouldUseMobileControls(false, true));
        }

        [Test]
        public void UsesMobile_WhenExplicitMobileSelected()
        {
            Assert.IsTrue(MobileTouchControlsPolicy.ShouldUseMobileControls(true, false));
        }

        [Test]
        public void SkipsMobile_OnDesktopFallbackWithoutTouch()
        {
            Assert.IsFalse(MobileTouchControlsPolicy.ShouldUseMobileControls(false, false));
        }
    }
}
