using UnityEngine;
using UnityEngine.XR;
using VRProject.Application.Startup;

namespace VRProject.Infrastructure.Startup
{
    /// <summary>
    /// Builds a <see cref="VrConnectionReadiness"/> snapshot from Unity XR state and,
    /// on Android devices, Bluetooth Meta Quest link state.
    /// </summary>
    public static class VrConnectionSignalSampler
    {
        public static VrConnectionReadiness Sample()
        {
            var isEditor = UnityEngine.Application.isEditor;
            var xrActive = XRSettings.isDeviceActive;
            var xrName = xrActive
                ? XRSettings.loadedDeviceName
                : isEditor ? "XR Device Simulator" : string.Empty;

            var bluetoothAdapterEnabled = false;
            var questBluetoothLinked = false;
            var questBluetoothName = string.Empty;

#if UNITY_ANDROID && !UNITY_EDITOR
            var scan = AndroidBluetoothMetaQuestProbe.Scan();
            bluetoothAdapterEnabled = scan.AdapterEnabled;
            questBluetoothLinked = scan.QuestDeviceLinked;
            questBluetoothName = scan.QuestDeviceName;
#endif

            return new VrConnectionReadiness(
                isEditor: isEditor,
                bluetoothAdapterEnabled: bluetoothAdapterEnabled,
                metaQuestBluetoothLinked: questBluetoothLinked,
                metaQuestBluetoothDeviceName: questBluetoothName,
                xrRuntimeActive: xrActive,
                xrDeviceName: xrName);
        }
    }
}
