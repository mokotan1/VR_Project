using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    public static class OsFpsInspiredDamageReceiver
    {
        public static IOsFpsInspiredDamageReceiver FindInParents(Collider collider)
        {
            if (collider == null)
                return null;

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IOsFpsInspiredDamageReceiver receiver)
                    return receiver;
            }

            return null;
        }
    }
}
