using UnityEngine;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Keeps a serialized melee profile on scene-placed weapons for device builds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneMeleeWeaponProfileSource : MonoBehaviour
    {
        [SerializeField] WeaponAttackProfile _profile;

        public WeaponAttackProfile Profile => _profile;

        public void SetProfile(WeaponAttackProfile profile)
        {
            _profile = profile;
        }
    }
}
