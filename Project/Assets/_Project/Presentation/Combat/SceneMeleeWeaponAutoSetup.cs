using UnityEngine;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Ensures scene-placed weapons (e.g. HK416 floor pickup) get the melee combat stack at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneMeleeWeaponAutoSetup : MonoBehaviour
    {
        [SerializeField] WeaponAttackProfile _profile;

        void Awake()
        {
            if (!SceneMeleeWeaponSetup.IsAllowedMeleeWeaponRoot(gameObject))
            {
                Debug.LogError(
                    $"[SceneMeleeWeaponAutoSetup] Misplaced on '{name}'. Remove this component or run " +
                    "VR Project → Combat → Repair Miswired HK416 Melee Stack In Open Scene.",
                    this);
                enabled = false;
                return;
            }

            SceneMeleeWeaponSetup.Ensure(gameObject, ResolveProfile());
        }

        WeaponAttackProfile ResolveProfile()
        {
            if (_profile != null)
                return _profile;

            var source = GetComponent<SceneMeleeWeaponProfileSource>();
            return source != null ? source.Profile : null;
        }
    }
}
