using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Wires camera references for flat/mobile motion sources at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponRuntimeBinder : MonoBehaviour
    {
        [SerializeField] bool _startPickedUp;
        [SerializeField] bool _unequipGunsOnPickup = false;
        [SerializeField] float _pickupRadius = 1.35f;

        bool _pickedUp;
        Camera _camera;
        OsFpsInspiredWeapon _cachedPlayerWeapon;

        void Start()
        {
            _camera = Camera.main;
            _pickedUp = _startPickedUp;

            if (_pickedUp)
                BindFlatAndMobileSources();
        }

        void Update()
        {
            if (_pickedUp)
                return;

            if (_cachedPlayerWeapon == null)
                RefreshPlayerWeaponCache();
            if (_cachedPlayerWeapon == null)
                return;

            if (Vector3.Distance(transform.position, _cachedPlayerWeapon.transform.position) > _pickupRadius)
                return;

            PickUpForFlatOrMobile();
        }

        public void PickUpForFlatOrMobile()
        {
            if (_pickedUp)
                return;

            _pickedUp = true;
            BindFlatAndMobileSources();

            if (!_unequipGunsOnPickup)
                return;

            foreach (var weapon in FindObjectsByType<OsFpsInspiredWeapon>(FindObjectsInactive.Include))
                weapon.SetEquipped(false);
        }

        public void MarkPickedUpForVrSnap()
        {
            _pickedUp = true;
        }

        void BindFlatAndMobileSources()
        {
            var router = GetComponent<WeaponMotionSourceRouter>();
            if (router != null)
                router.ActivatePickedUpSource();

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            foreach (var flat in GetComponentsInChildren<FlatMouseWeaponMotionSource>(true))
                flat.BindCamera(_camera.transform);

            foreach (var mobile in GetComponentsInChildren<MobileTouchWeaponMotionSource>(true))
                mobile.BindCamera(_camera.transform);
        }

        void RefreshPlayerWeaponCache()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            _cachedPlayerWeapon = p != null ? p.GetComponent<OsFpsInspiredWeapon>() : null;
        }
    }
}
