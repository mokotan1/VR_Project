using NUnit.Framework;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Tests.EditMode
{
    public sealed class BattleRoyaleCrosshairSpreadTests
    {
        [Test]
        public void FireKickContribution_AtFire_IsFull()
        {
            var k = BattleRoyaleCrosshairSpread.FireKickContribution(0f);
            Assert.That(k, Is.EqualTo(BattleRoyaleCrosshairSpread.FireKickPixels).Within(0.001f));
        }

        [Test]
        public void FireKickContribution_AfterWindow_IsZero()
        {
            var k = BattleRoyaleCrosshairSpread.FireKickContribution(BattleRoyaleCrosshairSpread.FireKickDuration);
            Assert.That(k, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ComputeRawSpread_Aiming_ReducesVersusHip()
        {
            var hip = BattleRoyaleCrosshairSpread.ComputeRawSpread(1f, true, false, 999f);
            var ads = BattleRoyaleCrosshairSpread.ComputeRawSpread(1f, true, true, 999f);
            Assert.That(ads, Is.LessThan(hip));
            Assert.That(ads, Is.EqualTo(hip * BattleRoyaleCrosshairSpread.AimSpreadMultiplier).Within(0.02f));
        }

        [Test]
        public void ComputeRawSpread_InAir_AddsBonusVersusGrounded()
        {
            var g = BattleRoyaleCrosshairSpread.ComputeRawSpread(0f, true, false, 999f);
            var a = BattleRoyaleCrosshairSpread.ComputeRawSpread(0f, false, false, 999f);
            Assert.That(a - g, Is.EqualTo(BattleRoyaleCrosshairSpread.AirSpreadPixels).Within(0.02f));
        }
    }
}
