#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using VRProject.Application.Startup;

namespace VRProject.Infrastructure.Startup
{
    /// <summary>
    /// Reads Android Bluetooth adapter state and looks for bonded Meta Quest devices.
    /// </summary>
    public static class AndroidBluetoothMetaQuestProbe
    {
        const string BluetoothConnectPermission = "android.permission.BLUETOOTH_CONNECT";

        public readonly struct ScanResult
        {
            public ScanResult(
                bool adapterEnabled,
                bool permissionGranted,
                bool questDeviceLinked,
                string questDeviceName)
            {
                AdapterEnabled = adapterEnabled;
                PermissionGranted = permissionGranted;
                QuestDeviceLinked = questDeviceLinked;
                QuestDeviceName = questDeviceName ?? string.Empty;
            }

            public bool AdapterEnabled { get; }
            public bool PermissionGranted { get; }
            public bool QuestDeviceLinked { get; }
            public string QuestDeviceName { get; }
        }

        public static ScanResult Scan()
        {
            if (!HasBluetoothConnectPermission())
            {
                Permission.RequestUserPermission(BluetoothConnectPermission);
                return new ScanResult(
                    adapterEnabled: false,
                    permissionGranted: false,
                    questDeviceLinked: false,
                    questDeviceName: string.Empty);
            }

            try
            {
                using var bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter");
                using var adapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
                if (adapter == null)
                {
                    return new ScanResult(
                        adapterEnabled: false,
                        permissionGranted: true,
                        questDeviceLinked: false,
                        questDeviceName: string.Empty);
                }

                var adapterEnabled = adapter.Call<bool>("isEnabled");
                if (!adapterEnabled)
                {
                    return new ScanResult(
                        adapterEnabled: false,
                        permissionGranted: true,
                        questDeviceLinked: false,
                        questDeviceName: string.Empty);
                }

                using var bondedDevices = adapter.Call<AndroidJavaObject>("getBondedDevices");
                if (bondedDevices == null)
                {
                    return new ScanResult(
                        adapterEnabled: true,
                        permissionGranted: true,
                        questDeviceLinked: false,
                        questDeviceName: string.Empty);
                }

                using var iterator = bondedDevices.Call<AndroidJavaObject>("iterator");
                while (iterator != null && iterator.Call<bool>("hasNext"))
                {
                    using var device = iterator.Call<AndroidJavaObject>("next");
                    var deviceName = device?.Call<string>("getName") ?? string.Empty;
                    if (!MetaQuestDeviceMatcher.IsQuestDevice(deviceName))
                        continue;

                    return new ScanResult(
                        adapterEnabled: true,
                        permissionGranted: true,
                        questDeviceLinked: true,
                        questDeviceName: deviceName);
                }

                return new ScanResult(
                    adapterEnabled: true,
                    permissionGranted: true,
                    questDeviceLinked: false,
                    questDeviceName: string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[VR Project] Bluetooth Meta Quest scan failed: " + exception.Message);
                return new ScanResult(
                    adapterEnabled: false,
                    permissionGranted: HasBluetoothConnectPermission(),
                    questDeviceLinked: false,
                    questDeviceName: string.Empty);
            }
        }

        static bool HasBluetoothConnectPermission()
        {
            if (!IsAndroid12OrNewer())
                return true;

            return Permission.HasUserAuthorizedPermission(BluetoothConnectPermission);
        }

        static bool IsAndroid12OrNewer()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT") >= 31;
        }

    }
}
#endif
