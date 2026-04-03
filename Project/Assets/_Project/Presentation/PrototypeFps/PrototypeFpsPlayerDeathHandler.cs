using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

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
            var motor = GetComponent<PrototypeThirdPersonPlayer>();
            if (motor != null)
                motor.SetControlsEnabled(false);
            var w = GetComponent<OsFpsInspiredWeapon>();
            if (w != null)
                w.enabled = false;
        }
    }
}
