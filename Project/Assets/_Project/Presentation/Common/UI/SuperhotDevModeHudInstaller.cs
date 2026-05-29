using UnityEngine;
using UnityEngine.UI;

namespace VRProject.Presentation.Common.UI
{
    public static class SuperhotDevModeHudInstaller
    {
        const string HudRootName = "HUD";

        public static GameObject EnsureUnderPlayer(GameObject player)
        {
            if (player == null)
                return null;

            var hudRoot = FindDirectChild(player.transform, HudRootName);
            if (hudRoot == null)
            {
                hudRoot = new GameObject(HudRootName);
                hudRoot.transform.SetParent(player.transform, false);
            }

            EnsureHudCanvas(hudRoot);

            if (player.GetComponent<SuperhotDevModeHUD>() == null)
                player.AddComponent<SuperhotDevModeHUD>();

            return hudRoot;
        }

        static GameObject FindDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child.gameObject;
            }

            return null;
        }

        static void EnsureHudCanvas(GameObject hudRoot)
        {
            var canvas = hudRoot.GetComponent<Canvas>();
            if (canvas == null)
                canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (hudRoot.GetComponent<CanvasScaler>() == null)
                hudRoot.AddComponent<CanvasScaler>();
            if (hudRoot.GetComponent<GraphicRaycaster>() == null)
                hudRoot.AddComponent<GraphicRaycaster>();
        }
    }
}
