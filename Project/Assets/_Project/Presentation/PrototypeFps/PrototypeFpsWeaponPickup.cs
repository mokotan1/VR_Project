using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Walk into trigger to equip the player's <see cref="OsFpsInspiredWeapon"/> (prototype flow).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PrototypeFpsWeaponPickup : MonoBehaviour
    {
        [SerializeField] bool _destroyRootWhenTaken = true;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var weapon = other.GetComponentInParent<OsFpsInspiredWeapon>();
            if (weapon == null || weapon.IsEquipped)
                return;

            weapon.SetEquipped(true);

            if (_destroyRootWhenTaken)
                Destroy(gameObject);
        }
    }
}
