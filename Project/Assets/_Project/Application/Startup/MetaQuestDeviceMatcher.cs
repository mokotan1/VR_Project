using System;

namespace VRProject.Application.Startup
{
    /// <summary>
    /// Matches Meta Quest device names reported by XR runtimes or Android Bluetooth.
    /// </summary>
    public static class MetaQuestDeviceMatcher
    {
        static readonly string[] QuestNameTokens =
        {
            "quest",
            "oculus",
            "meta quest",
        };

        public static bool IsQuestDevice(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                return false;

            var normalized = deviceName.Trim();
            for (var i = 0; i < QuestNameTokens.Length; i++)
            {
                if (normalized.IndexOf(QuestNameTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
