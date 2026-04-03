using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Equip the player's <see cref="OsFpsInspiredWeapon"/> when they enter range.
    /// Uses distance to the tagged player root because <see cref="CharacterController"/> often does not
    /// deliver <see cref="OnTriggerEnter"/> to nearby trigger volumes (no Rigidbody/Collider pair).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PrototypeFpsWeaponPickup : MonoBehaviour
    {
        [SerializeField] bool _destroyRootWhenTaken = true;
        [Tooltip("Max distance from this pickup to the player root (CharacterController is on that root).")]
        [SerializeField] float _pickupRadius = 1.35f;

        bool _consumed;
        OsFpsInspiredWeapon _cachedPlayerWeapon;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void Start()
        {
            RefreshPlayerWeaponCache();
        }

        void Update()
        {
            if (_consumed)
                return;

            if (_cachedPlayerWeapon == null)
                RefreshPlayerWeaponCache();
            if (_cachedPlayerWeapon == null)
                return;

            if (Vector3.Distance(transform.position, _cachedPlayerWeapon.transform.position) > _pickupRadius)
                return;

            TryConsume(_cachedPlayerWeapon);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_consumed)
                return;

            var weapon = other.GetComponentInParent<OsFpsInspiredWeapon>();
            if (weapon == null)
                return;

            TryConsume(weapon);
        }

        void RefreshPlayerWeaponCache()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            _cachedPlayerWeapon = p != null ? p.GetComponent<OsFpsInspiredWeapon>() : null;
        }

        void TryConsume(OsFpsInspiredWeapon weapon)
        {
            if (weapon == null || weapon.IsEquipped)
                return;
            if (!weapon.gameObject.CompareTag("Player"))
                return;

            weapon.SetEquipped(true);
            _consumed = true;

            if (_destroyRootWhenTaken)
                Destroy(gameObject);
        }
    }
}
