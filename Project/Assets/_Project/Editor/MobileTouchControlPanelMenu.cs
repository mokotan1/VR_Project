#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRProject.Presentation.Common.UI;

namespace VRProject.EditorTools
{
    public static class MobileTouchControlPanelMenu
    {
        [MenuItem("VR Project/Mobile/Ensure Touch Control Panel On Player")]
        public static void EnsureInOpenScene()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[VR Project] No Player-tagged object found in the open scene.");
                return;
            }

            MobileTouchControlPanelInstaller.EnsureUnderPlayer(player);
            EditorUtility.SetDirty(player);
            Debug.Log("[VR Project] Mobile touch control panel installed under " + player.name + "/HUD/MobileTouchHUD");
        }
    }
}
#endif
