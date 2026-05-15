using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    public interface IOsFpsInspiredDamageReceiver
    {
        void ApplyDamage(float amount, Vector3 hitPoint);
    }
}
