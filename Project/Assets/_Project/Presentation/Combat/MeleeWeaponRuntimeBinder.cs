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
        [SerializeField] bool _unequipGunsOnStart = true;

        void Start()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            foreach (var flat in GetComponentsInChildren<FlatMouseWeaponMotionSource>(true))
                flat.BindCamera(camera.transform);

            foreach (var mobile in GetComponentsInChildren<MobileTouchWeaponMotionSource>(true))
                mobile.BindCamera(camera.transform);

            if (!_unequipGunsOnStart)
                return;

            foreach (var weapon in FindObjectsByType<OsFpsInspiredWeapon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                weapon.SetEquipped(false);
        }
    }
}
