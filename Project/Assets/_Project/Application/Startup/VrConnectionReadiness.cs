namespace VRProject.Application.Startup
{
    /// <summary>
    /// Immutable snapshot of Bluetooth and Meta Quest connection signals used to
    /// decide whether VR Play should be offered on the startup screen.
    /// </summary>
    public readonly struct VrConnectionReadiness
    {
        public VrConnectionReadiness(
            bool isEditor,
            bool bluetoothAdapterEnabled,
            bool metaQuestBluetoothLinked,
            string metaQuestBluetoothDeviceName,
            bool xrRuntimeActive,
            string xrDeviceName)
        {
            IsEditor = isEditor;
            BluetoothAdapterEnabled = bluetoothAdapterEnabled;
            MetaQuestBluetoothLinked = metaQuestBluetoothLinked;
            MetaQuestBluetoothDeviceName = metaQuestBluetoothDeviceName ?? string.Empty;
            XrRuntimeActive = xrRuntimeActive;
            XrDeviceName = xrDeviceName ?? string.Empty;
        }

        public bool IsEditor { get; }
        public bool BluetoothAdapterEnabled { get; }
        public bool MetaQuestBluetoothLinked { get; }
        public string MetaQuestBluetoothDeviceName { get; }
        public bool XrRuntimeActive { get; }
        public string XrDeviceName { get; }

        public bool XrDeviceIsMetaQuest => MetaQuestDeviceMatcher.IsQuestDevice(XrDeviceName);
    }
}
