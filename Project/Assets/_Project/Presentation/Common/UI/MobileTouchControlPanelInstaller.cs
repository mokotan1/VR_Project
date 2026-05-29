using UnityEngine;
using UnityEngine.UI;

namespace VRProject.Presentation.Common.UI
{
    public static class MobileTouchControlPanelInstaller
    {
        const string HudRootName = "HUD";
        const string MobileHudRootName = "MobileTouchHUD";

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

            var mobileRoot = FindDirectChild(hudRoot.transform, MobileHudRootName);
            if (mobileRoot == null)
            {
                mobileRoot = new GameObject(MobileHudRootName);
                mobileRoot.transform.SetParent(hudRoot.transform, false);
                var rt = mobileRoot.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (mobileRoot.GetComponent<MobileTouchInputBus>() == null)
                mobileRoot.AddComponent<MobileTouchInputBus>();
            if (mobileRoot.GetComponent<MobileTouchControlPanel>() == null)
                mobileRoot.AddComponent<MobileTouchControlPanel>();

            return mobileRoot;
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
    }
}
