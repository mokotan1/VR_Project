using NUnit.Framework;
using VRProject.Application.Startup;

namespace VRProject.Tests.EditMode
{
    public sealed class PlayModeSelectionTests
    {
        [Test]
        public void CanSelectMobile_WhenMobileAvailable_ReturnsTrue()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.IsTrue(PlayModeSelection.CanSelect(PlayModeKind.Mobile, availability));
        }

        [Test]
        public void CanSelectVr_WhenVrUnavailable_ReturnsFalse()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.IsFalse(PlayModeSelection.CanSelect(PlayModeKind.Vr, availability));
        }

        [Test]
        public void ChooseFallback_PrefersVrWhenXrActive()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: true);

            Assert.AreEqual(PlayModeKind.Vr, PlayModeSelection.ChooseFallback(availability));
        }

        [Test]
        public void ChooseFallback_UsesMobileWhenVrUnavailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.AreEqual(PlayModeKind.Mobile, PlayModeSelection.ChooseFallback(availability));
        }

        [Test]
        public void ChooseFallback_ReturnsNoneWhenNothingAvailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: false,
                vrAvailable: false);

            Assert.AreEqual(PlayModeKind.None, PlayModeSelection.ChooseFallback(availability));
        }

        [Test]
        public void ResolveSelectedMode_UsesRequestedModeWhenAvailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: true);

            Assert.AreEqual(
                PlayModeKind.Mobile,
                PlayModeSelection.ResolveSelectedMode(PlayModeKind.Mobile, availability));
        }

        [Test]
        public void ResolveSelectedMode_FallsBackWhenRequestedModeUnavailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: true,
                vrAvailable: false);

            Assert.AreEqual(
                PlayModeKind.Mobile,
                PlayModeSelection.ResolveSelectedMode(PlayModeKind.Vr, availability));
        }

        [Test]
        public void ResolveSelectedMode_FallsBackToNoneWhenNothingAvailable()
        {
            var availability = new PlayModeAvailability(
                mobileAvailable: false,
                vrAvailable: false);

            Assert.AreEqual(
                PlayModeKind.None,
                PlayModeSelection.ResolveSelectedMode(PlayModeKind.Vr, availability));
        }
    }
}
