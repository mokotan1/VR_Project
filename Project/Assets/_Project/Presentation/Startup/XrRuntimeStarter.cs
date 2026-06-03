using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Starts XR only after the player explicitly chooses VR play. This keeps
    /// Android phones and emulators from crashing during app launch when no
    /// OpenXR runtime is available, while preserving Quest VR startup.
    /// </summary>
    public static class XrRuntimeStarter
    {
        public static IEnumerator StartXrThenLoadScene(string sceneName, Text statusText)
        {
            var settings = XRGeneralSettings.Instance;
            var manager = settings != null ? settings.Manager : null;
            if (manager == null)
            {
                SetStatus(statusText, "XR settings missing. Starting without XR.");
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            if (manager.activeLoader == null)
                yield return manager.InitializeLoader();

            if (manager.activeLoader != null)
            {
                manager.StartSubsystems();
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            SetStatus(statusText, "XR runtime unavailable. Check headset connection and try again.");
        }

        static void SetStatus(Text statusText, string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
