using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Samples the current platform and XR runtime to produce a fresh
    /// <see cref="DeviceConnectionStatus"/>. This is the single place that
    /// touches Unity device APIs; UI and selection logic depend on the
    /// resulting value type only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeviceConnectionProbe : MonoBehaviour
    {
        public DeviceConnectionStatus CurrentStatus { get; private set; }

        void Awake()
        {
            Refresh();
        }

        public DeviceConnectionStatus Refresh()
        {
            var isAndroid = Application.platform == RuntimePlatform.Android;
            var isEditor = Application.isEditor;
            var xrActive = XRSettings.isDeviceActive;
            var xrName = xrActive ? XRSettings.loadedDeviceName : string.Empty;
            var platformLabel = ResolvePlatformLabel(isAndroid, isEditor);

            var mobileAvailable = isAndroid || isEditor ||
                                  Application.platform == RuntimePlatform.WindowsPlayer ||
                                  Application.platform == RuntimePlatform.OSXPlayer ||
                                  Application.platform == RuntimePlatform.LinuxPlayer;

            var vrAvailable = xrActive;

            CurrentStatus = new DeviceConnectionStatus(
                platformLabel,
                xrName,
                isAndroid,
                isEditor,
                xrActive,
                mobileAvailable,
                vrAvailable);

            return CurrentStatus;
        }

        static string ResolvePlatformLabel(bool isAndroid, bool isEditor)
        {
            if (isEditor)
                return "Unity Editor";
            if (isAndroid)
                return "Android Device";
            return Application.platform.ToString();
        }
    }
}
