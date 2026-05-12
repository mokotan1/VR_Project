using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 플레이어가 총을 들고 있을 때 총구(또는 조준 기준) Transform을 등록해
    /// <see cref="SuperhotEnemyBrain"/> 등 AI가 몸통 right 대신 실제 총 방향 기준으로 횡이동할 수 있게 합니다.
    /// </summary>
    public static class PlayerWeaponFirePointForAi
    {
        static Object _owner;
        static Transform _muzzle;

        public static Transform ActiveMuzzle => _muzzle;

        /// <summary>무기가 장착되어 총구가 있을 때 호출합니다.</summary>
        public static void Publish(Object owner, Transform muzzleOrNull)
        {
            _owner = owner;
            _muzzle = muzzleOrNull;
        }

        /// <summary>해당 소유자가 등록한 값만 제거합니다(다른 무기가 덮어쓴 경우는 유지).</summary>
        public static void ClearIfOwner(Object owner)
        {
            if (_owner != owner)
                return;
            _owner = null;
            _muzzle = null;
        }
    }
}
