using NUnit.Framework;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode.Gameplay
{
    public sealed class SuperhotPlaytestPlayerHealthTests
    {
        [Test]
        public void ApplyHit_SingleHit_TriggersDefeat()
        {
            var go = new UnityEngine.GameObject("PlayerHealthTest");
            try
            {
                var health = go.AddComponent<SuperhotPlaytestPlayerHealth>();
                var defeated = false;
                health.PlayerDefeated += () => defeated = true;

                health.ApplyHit();

                Assert.IsFalse(health.IsAlive);
                Assert.AreEqual(0, health.RemainingHits);
                Assert.IsTrue(defeated);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
