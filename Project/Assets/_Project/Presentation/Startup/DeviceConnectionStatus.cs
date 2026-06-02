using VRProject.Application.Startup;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Immutable snapshot of the player's device and XR runtime state, plus
    /// the derived <see cref="PlayModeAvailability"/> used by the startup UI.
    /// </summary>
    public readonly struct DeviceConnectionStatus
    {
        public DeviceConnectionStatus(
            string platformLabel,
            string xrDeviceName,
            bool isAndroid,
            bool isEditor,
            bool xrDeviceActive,
            bool mobileAvailable,
            bool vrAvailable)
        {
            PlatformLabel = platformLabel ?? string.Empty;
            XrDeviceName = xrDeviceName ?? string.Empty;
            IsAndroid = isAndroid;
            IsEditor = isEditor;
            XrDeviceActive = xrDeviceActive;
            Availability = new PlayModeAvailability(mobileAvailable, vrAvailable);
        }

        public string PlatformLabel { get; }
        public string XrDeviceName { get; }
        public bool IsAndroid { get; }
        public bool IsEditor { get; }
        public bool XrDeviceActive { get; }
        public PlayModeAvailability Availability { get; }

        public string MobileStatusText =>
            Availability.MobileAvailable ? "Mobile Play Ready" : "Mobile Play Unavailable";

        public string VrStatusText
        {
            get
            {
                if (XrDeviceActive)
                    return "VR Headset Ready";
                if (IsEditor && Availability.VrAvailable)
                    return "XR Simulator Ready";
                return "VR Headset Not Connected";
            }
        }
    }
}
