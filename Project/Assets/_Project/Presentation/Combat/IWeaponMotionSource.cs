using UnityEngine;

namespace VRProject.Presentation.Combat
{
    public readonly struct WeaponMotionPose
    {
        public WeaponMotionPose(
            Vector3 tipWorldPosition,
            Vector3 handleWorldPosition,
            Vector3 weaponForward,
            Vector3 weaponRight)
        {
            TipWorldPosition = tipWorldPosition;
            HandleWorldPosition = handleWorldPosition;
            WeaponForward = weaponForward;
            WeaponRight = weaponRight;
        }

        public Vector3 TipWorldPosition { get; }
        public Vector3 HandleWorldPosition { get; }
        public Vector3 WeaponForward { get; }
        public Vector3 WeaponRight { get; }
    }

    public interface IWeaponMotionSource
    {
        bool IsActive { get; }
        WeaponMotionPose SamplePose();
    }
}
