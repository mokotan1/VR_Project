using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponAttackSession : MonoBehaviour
    {
        [SerializeField] WeaponMotion _motion;
        [SerializeField] WeaponAttackProfile _profile;

        WeaponAttackSessionState _state = new WeaponAttackSessionState(false, 0, 0, 0f);

        public bool IsActive => _state.IsActive;
        public int CurrentSessionId => _state.SessionId;
        public WeaponAttackKind ActiveKind => _motion != null ? _motion.Current.ActiveKind : WeaponAttackKind.None;

        void Awake()
        {
            if (_motion == null)
                _motion = GetComponent<WeaponMotion>();
        }

        void FixedUpdate()
        {
            if (_profile == null || _motion == null)
                return;

            var motion = _motion.Current;
            var result = WeaponAttackSessionLogic.Tick(
                _state,
                motion.LinearSpeedMps,
                motion.AngularSpeedDps,
                _profile.EnterLinearSpeed,
                _profile.EnterAngularSpeed,
                _profile.ExitLinearSpeed,
                _profile.ExitAngularSpeed,
                _profile.ExitIdleFramesRequired,
                _profile.MaxSessionDurationSeconds,
                Time.fixedDeltaTime);

            _state = result.NextState;
        }
    }
}
