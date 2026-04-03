using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    public sealed class PrototypeFpsHud : MonoBehaviour
    {
        [SerializeField] OsFpsInspiredWeapon _weapon;
        [SerializeField] PrototypeFpsPlayerHealth _health;
        [SerializeField] GameObject _crosshairRoot;
        [Tooltip("Bottom-right: ammo / mag (battle royale style).")]
        [SerializeField] Text _ammoText;
        [Tooltip("Bottom-left: HP.")]
        [SerializeField] Text _healthText;
        [Tooltip("Bottom-center: control hints.")]
        [SerializeField] Text _hintText;

        void Update()
        {
            if (_crosshairRoot != null)
                _crosshairRoot.SetActive(_weapon == null || _weapon.IsEquipped);

            if (_ammoText != null && _weapon != null)
            {
                if (!_weapon.IsEquipped)
                    _ammoText.text = "—";
                else if (_weapon.IsReloading)
                    _ammoText.text = "RELOAD";
                else
                    _ammoText.text = $"{_weapon.AmmoInMag} / {_weapon.MagSize}";
            }

            if (_healthText != null)
            {
                var hp = _health != null
                    ? $"{Mathf.CeilToInt(_health.Health)}"
                    : "—";
                _healthText.text = "HP " + hp;
            }

            if (_hintText == null)
                return;

            var pickup = _weapon != null && !_weapon.IsEquipped
                ? "Walk into the HK416 to pick up.\n"
                : string.Empty;

            _hintText.text =
                pickup +
                "WASD   Mouse look   RMB aim   LMB fire   R reload   Esc cursor";
        }
    }
}
