namespace VRProject.Application.Startup
{
    /// <summary>
    /// Pure rules for when Bluetooth, Meta Quest, and VR Play are considered ready.
    /// </summary>
    public static class VrPlayAvailability
    {
        public static bool IsBluetoothReady(in VrConnectionReadiness readiness)
        {
            if (readiness.IsEditor)
                return true;

            if (readiness.XrRuntimeActive && readiness.XrDeviceIsMetaQuest)
                return true;

            return readiness.BluetoothAdapterEnabled && readiness.MetaQuestBluetoothLinked;
        }

        public static bool IsMetaQuestConnected(in VrConnectionReadiness readiness)
        {
            if (readiness.IsEditor)
                return true;

            if (readiness.XrRuntimeActive && readiness.XrDeviceIsMetaQuest)
                return true;

            return readiness.MetaQuestBluetoothLinked;
        }

        public static bool IsVrPlayAvailable(in VrConnectionReadiness readiness)
        {
            return IsBluetoothReady(readiness) && IsMetaQuestConnected(readiness);
        }
    }
}
