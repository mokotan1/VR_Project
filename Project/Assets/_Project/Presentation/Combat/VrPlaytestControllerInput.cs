using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// XR controller features plus editor fallbacks that match the XR Device Simulator
    /// (G = grip, left mouse = trigger).
    /// </summary>
    public static class VrPlaytestControllerInput
    {
        public static bool TryReadGripHeld(XRNode hand, float analogThreshold, out bool gripHeld)
        {
            gripHeld = false;

#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.gKey.isPressed)
            {
                gripHeld = true;
                return true;
            }
#endif

            VrSceneWeaponSnapInput.ReadControllerFeatures(
                hand,
                out _,
                out _,
                out var gripButton,
                out var gripValue);
            gripHeld = VrSceneWeaponSnapInput.IsControllerGripPressed(
                gripButton,
                gripValue,
                analogThreshold);
            return true;
        }

        public static bool TryReadTriggerEdge(
            XRNode hand,
            float analogThreshold,
            ref VrTriggerPressDetector detector,
            out bool triggerEdge)
        {
            triggerEdge = false;

#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                triggerEdge = true;
                return true;
            }
#endif

            VrSceneWeaponSnapInput.ReadControllerFeatures(
                hand,
                out var triggerButton,
                out var triggerValue,
                out _,
                out _);
            triggerEdge = detector.Tick(
                VrSceneWeaponSnapInput.IsControllerTriggerPressed(
                    triggerButton,
                    triggerValue,
                    analogThreshold),
                out detector);
            return true;
        }
    }
}
