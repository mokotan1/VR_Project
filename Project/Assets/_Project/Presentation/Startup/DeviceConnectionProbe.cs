using UnityEngine;
using VRProject.Application.Startup;
using VRProject.Infrastructure.Startup;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Samples the current platform, Bluetooth Meta Quest link, and XR runtime to
    /// produce a fresh <see cref="DeviceConnectionStatus"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeviceConnectionProbe : MonoBehaviour
    {
        public DeviceConnectionStatus CurrentStatus { get; private set; }
        public VrConnectionReadiness VrReadiness { get; private set; }

        void Awake()
        {
            Refresh();
        }

        public DeviceConnectionStatus Refresh()
        {
            VrReadiness = VrConnectionSignalSampler.Sample();

            var platform = UnityEngine.Application.platform;
            var isAndroid = platform == RuntimePlatform.Android;
            var isEditor = UnityEngine.Application.isEditor;
            var xrActive = VrReadiness.XrRuntimeActive;
            var xrName = VrReadiness.XrDeviceName;
            var platformLabel = ResolvePlatformLabel(platform, isAndroid, isEditor);

            var mobileAvailable = isAndroid || isEditor ||
                                  platform == RuntimePlatform.WindowsPlayer ||
                                  platform == RuntimePlatform.OSXPlayer ||
                                  platform == RuntimePlatform.LinuxPlayer;

            var vrAvailable = VrPlayAvailability.IsVrPlayAvailable(VrReadiness);

            CurrentStatus = new DeviceConnectionStatus(
                platformLabel,
                xrName,
                isAndroid,
                isEditor,
                xrActive,
                mobileAvailable,
                vrAvailable,
                VrReadiness);

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
    }
}
