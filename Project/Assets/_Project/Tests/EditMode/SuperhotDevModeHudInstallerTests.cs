using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.Common.UI;

namespace VRProject.Tests.EditMode
{
    public sealed class SuperhotDevModeHudInstallerTests
    {
        [Test]
        public void EnsureUnderPlayer_CreatesHudRootAndDevModeHud()
        {
            var player = new GameObject("UnityChan_Player");
            try
            {
                var hud = SuperhotDevModeHudInstaller.EnsureUnderPlayer(player);

                Assert.IsNotNull(hud);
                Assert.AreSame(player.transform, hud.transform.parent);
                Assert.IsNotNull(hud.GetComponent<Canvas>());
                Assert.IsNotNull(hud.GetComponent<CanvasScaler>());
                Assert.IsNotNull(hud.GetComponent<GraphicRaycaster>());
                Assert.IsNotNull(player.GetComponent<SuperhotDevModeHUD>());
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void EnsureUnderPlayer_ReusesExistingHudRootAndDevModeHud()
        {
            var player = new GameObject("UnityChan_Player");
            var existingHud = new GameObject("HUD");
            try
            {
                existingHud.transform.SetParent(player.transform, false);
                var existingDevHud = player.AddComponent<SuperhotDevModeHUD>();

                var hud = SuperhotDevModeHudInstaller.EnsureUnderPlayer(player);

                Assert.AreSame(existingHud, hud);
                Assert.AreSame(existingDevHud, player.GetComponent<SuperhotDevModeHUD>());
                Assert.AreEqual(1, player.GetComponents<SuperhotDevModeHUD>().Length);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
