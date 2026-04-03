using NUnit.Framework;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode
{
    public sealed class MantleHeightBandsTests
    {
        const float Step = 0.38f;
        const float Jump = 1.15f;
        const float Mantle = 1.95f;

        [Test]
        public void Classify_BelowStep_IsStepOrBelow()
        {
            Assert.That(MantleHeightBands.Classify(0.2f, Step, Jump, Mantle), Is.EqualTo(MantleBand.StepOrBelow));
        }

        [Test]
        public void Classify_MidHeight_IsJumpBand()
        {
            Assert.That(MantleHeightBands.Classify(0.9f, Step, Jump, Mantle), Is.EqualTo(MantleBand.JumpBand));
        }

        [Test]
        public void Classify_Tall_IsMantleBand()
        {
            Assert.That(MantleHeightBands.Classify(1.5f, Step, Jump, Mantle), Is.EqualTo(MantleBand.MantleBand));
        }

        [Test]
        public void Classify_TooHigh_IsTooHigh()
        {
            Assert.That(MantleHeightBands.Classify(2.5f, Step, Jump, Mantle), Is.EqualTo(MantleBand.TooHigh));
        }
    }
}
