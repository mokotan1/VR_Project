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

        void Update()
        {
            if (_statusText == null)
                return;

            var ammo = _weapon != null
                ? (_weapon.IsReloading ? "Reloading" : $"{_weapon.AmmoInMag}/{_weapon.MagSize}")
                : "--";

            var hp = _health != null
                ? $"{Mathf.CeilToInt(_health.Health)}/{Mathf.CeilToInt(_health.MaxHealth)}"
                : "--";

            _statusText.text = $"HP {hp}   Ammo {ammo}\nWASD move   Mouse look   LMB fire   R reload   Esc cursor";
        }
    }
}
