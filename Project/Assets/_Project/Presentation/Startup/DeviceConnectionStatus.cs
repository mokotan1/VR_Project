using VRProject.Application.Startup;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Immutable snapshot of the player's device, Bluetooth, Meta Quest, and XR
    /// runtime state, plus the derived <see cref="PlayModeAvailability"/>.
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
            bool vrAvailable,
            VrConnectionReadiness vrReadiness)
        {
            PlatformLabel = platformLabel ?? string.Empty;
            XrDeviceName = xrDeviceName ?? string.Empty;
            IsAndroid = isAndroid;
            IsEditor = isEditor;
            XrDeviceActive = xrDeviceActive;
            Availability = new PlayModeAvailability(mobileAvailable, vrAvailable);
            VrReadiness = vrReadiness;
        }

        public string PlatformLabel { get; }
        public string XrDeviceName { get; }
        public bool IsAndroid { get; }
        public bool IsEditor { get; }
        public bool XrDeviceActive { get; }
        public PlayModeAvailability Availability { get; }
        public VrConnectionReadiness VrReadiness { get; }

        public bool BluetoothReady => VrPlayAvailability.IsBluetoothReady(VrReadiness);
        public bool MetaQuestConnected => VrPlayAvailability.IsMetaQuestConnected(VrReadiness);

        public string MobileStatusText =>
            Availability.MobileAvailable ? "Mobile Play Ready" : "Mobile Play Unavailable";

        public string BluetoothStatusText
        {
            get
            {
                if (BluetoothReady)
                    return "Bluetooth Ready";
                if (!VrReadiness.BluetoothAdapterEnabled)
                    return "Bluetooth Off or Unavailable";
                return "Bluetooth On — Meta Quest Not Linked";
            }
        }

        public string MetaQuestStatusText
        {
            get
            {
                if (MetaQuestConnected)
                {
                    if (XrDeviceActive && VrReadiness.XrDeviceIsMetaQuest)
                        return "Meta Quest Connected (XR Active)";

                    var questName = VrReadiness.MetaQuestBluetoothDeviceName;
                    return string.IsNullOrEmpty(questName)
                        ? "Meta Quest Connected (Bluetooth)"
                        : "Meta Quest Connected (" + questName + ")";
                }

                if (XrDeviceActive && !VrReadiness.XrDeviceIsMetaQuest)
                    return "XR Device Connected (Not Meta Quest)";

                return "Meta Quest Not Connected";
            }
        }

        public string VrStatusText
        {
            get
            {
                if (Availability.VrAvailable)
                    return "VR Play Ready";
                if (IsEditor)
                    return "XR Simulator Ready";
                if (!BluetoothReady && !MetaQuestConnected)
                    return "Enable Bluetooth and connect Meta Quest";
                if (!BluetoothReady)
                    return "Bluetooth / Meta Quest Link Required";
                if (!MetaQuestConnected)
                    return "Meta Quest Connection Required";
                return "VR Play Unavailable";
            }
        }
    }
}
