using UnityEngine;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Wires <see cref="VrHk416TriggerFire"/> to the same bullet prefab/settings as the Unity-Chan prototype weapon.
    /// </summary>
    public static class VrHk416FireVisualDefaults
    {
        public static void ApplyTo(VrHk416TriggerFire fire)
        {
            if (fire == null)
                return;

            fire.ApplySharedDefaults(ResolveBulletVisualPrefab());
        }

        static GameObject ResolveBulletVisualPrefab()
        {
            foreach (var weapon in Object.FindObjectsByType<OsFpsInspiredWeapon>(FindObjectsInactive.Include))
            {
                var prefab = weapon.SharedBulletVisualPrefab;
                if (prefab != null)
                    return prefab;
            }

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                UnityChanPrototypeWeaponMuzzleDefaults.DefaultBulletPrefabPath);
#else
            return null;
#endif
        }
    }
}
