using NUnit.Framework;
using VRProject.Presentation.Combat;

namespace VRProject.Tests.EditMode
{
    public sealed class VrTriggerPressDetectorTests
    {
        [Test]
        public void Tick_ReturnsTrueOnlyOnRisingEdge()
        {
            var detector = new VrTriggerPressDetector();

            Assert.IsFalse(Tick(ref detector, false));
            Assert.IsTrue(Tick(ref detector, true));
            Assert.IsFalse(Tick(ref detector, true));
            Assert.IsFalse(Tick(ref detector, false));
            Assert.IsTrue(Tick(ref detector, true));
        }

        static bool Tick(ref VrTriggerPressDetector detector, bool pressed)
        {
            var result = detector.Tick(pressed, out var next);
            detector = next;
            return result;
        }
    }
}
