using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    public sealed class PrototypeFpsHud : MonoBehaviour
    {
        [SerializeField] OsFpsInspiredWeapon _weapon;
        [SerializeField] PrototypeFpsPlayerHealth _health;
        [SerializeField] Text _statusText;
        [SerializeField] GameObject _crosshairRoot;

        void Update()
        {
            if (_crosshairRoot != null)
                _crosshairRoot.SetActive(_weapon == null || _weapon.IsEquipped);

            if (_statusText == null)
                return;

            var ammo = _weapon != null
                ? (_weapon.IsReloading ? "Reloading" : $"{_weapon.AmmoInMag}/{_weapon.MagSize}")
                : "--";

            var hp = _health != null
                ? $"{Mathf.CeilToInt(_health.Health)}/{Mathf.CeilToInt(_health.MaxHealth)}"
                : "--";

            var pickup = _weapon != null && !_weapon.IsEquipped
                ? "\nWalk into the HK416 on the ground to pick it up."
                : string.Empty;

            _statusText.text =
                $"HP {hp}   Ammo {ammo}\nWASD move   Mouse look   RMB aim   LMB fire   R reload   Esc cursor{pickup}";
        }
    }
}
