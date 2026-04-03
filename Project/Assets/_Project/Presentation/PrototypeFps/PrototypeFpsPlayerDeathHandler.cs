using UnityEngine;
using VRProject.Presentation.OsFpsInspired;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.PrototypeFps
{
    public sealed class PrototypeFpsPlayerDeathHandler : MonoBehaviour
    {
        void OnEnable()
        {
            var hp = GetComponent<PrototypeFpsPlayerHealth>();
            if (hp != null)
                hp.Defeated += OnDefeated;
        }

        void OnDisable()
        {
            var hp = GetComponent<PrototypeFpsPlayerHealth>();
            if (hp != null)
                hp.Defeated -= OnDefeated;
        }

        void OnDefeated()
        {
            var motor = UnityChanLocomotionMotorResolver.ResolveOn(gameObject);
            if (motor != null)
                motor.SetControlsEnabled(false);
            var w = GetComponent<OsFpsInspiredWeapon>();
            if (w != null)
                w.enabled = false;
        }
    }
}
