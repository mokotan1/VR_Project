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
            var platform = UnityEngine.Application.platform;
            var isAndroid = platform == RuntimePlatform.Android;
            var isEditor = UnityEngine.Application.isEditor;
            var xrActive = XRSettings.isDeviceActive;
            var xrName = xrActive
                ? XRSettings.loadedDeviceName
                : isEditor ? "XR Device Simulator" : string.Empty;
            var platformLabel = ResolvePlatformLabel(platform, isAndroid, isEditor);

            var mobileAvailable = isAndroid || isEditor ||
                                  platform == RuntimePlatform.WindowsPlayer ||
                                  platform == RuntimePlatform.OSXPlayer ||
                                  platform == RuntimePlatform.LinuxPlayer;

            var vrAvailable = ResolveVrAvailable(xrActive, isEditor);

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

        static string ResolvePlatformLabel(RuntimePlatform platform, bool isAndroid, bool isEditor)
        {
            if (isEditor)
                return "Unity Editor";
            if (isAndroid)
                return "Android Device";
            return platform.ToString();
        }

        public static bool ResolveVrAvailable(bool xrDeviceActive, bool isEditor)
        {
            return xrDeviceActive || isEditor;
        }
    }
}
