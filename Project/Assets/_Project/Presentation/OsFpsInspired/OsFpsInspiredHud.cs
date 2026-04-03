using UnityEngine;
using UnityEngine.UI;

namespace VRProject.Presentation.OsFpsInspired
{
    public sealed class OsFpsInspiredHud : MonoBehaviour
    {
        [SerializeField] OsFpsInspiredWeapon _weapon;
        [SerializeField] Text _ammoText;

        void Update()
        {
            if (_ammoText == null || _weapon == null)
                return;
            _ammoText.text = _weapon.IsReloading
                ? "Reloading..."
                : $"{_weapon.AmmoInMag} / {_weapon.MagSize}";
        }
    }
}
