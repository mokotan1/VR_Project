using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Combat
{
    public static class VrHapticChannel
    {
        const float MinDuration = 0.01f;

        public static void PulseBoth(float amplitude, float durationSeconds)
        {
            Pulse(XRNode.LeftHand, amplitude, durationSeconds);
            Pulse(XRNode.RightHand, amplitude, durationSeconds);
        }

        public static void Pulse(XRNode node, float amplitude, float durationSeconds)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return;

            device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), Mathf.Max(MinDuration, durationSeconds));
        }
    }
}
