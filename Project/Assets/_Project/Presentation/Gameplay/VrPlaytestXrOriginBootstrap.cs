using Unity.XR.CoreUtils;
using UnityEngine;
using VRProject.Presentation.Combat;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Ensures XR combat components exist when playing a scene directly without
    /// <see cref="SuperhotPlaytestRigSelector"/> (e.g. Unity-Chan prototype FPS).
    /// </summary>
    public static class VrPlaytestXrOriginBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureActiveXrOriginCombatStack()
        {
            if (Object.FindAnyObjectByType<SuperhotPlaytestRigSelector>(FindObjectsInactive.Include) != null)
                return;

            var xrOrigin = Object.FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (xrOrigin == null || !xrOrigin.gameObject.activeInHierarchy)
                return;

            EnsureCombatStackOn(xrOrigin);
        }

        public static void EnsureCombatStackOn(XROrigin xrOrigin)
        {
            if (xrOrigin == null)
                return;

            var root = xrOrigin.gameObject;

            var snapInput = root.GetComponent<VrSceneWeaponSnapInput>();
            if (snapInput == null)
                snapInput = root.AddComponent<VrSceneWeaponSnapInput>();
            snapInput.Bind(xrOrigin);

            if (root.GetComponent<VrHk416GripHoldController>() == null)
                root.AddComponent<VrHk416GripHoldController>();

            var fire = root.GetComponent<VrHk416TriggerFire>();
            if (fire == null)
                fire = root.AddComponent<VrHk416TriggerFire>();
            VrHk416FireVisualDefaults.ApplyTo(fire);
        }
    }
}
