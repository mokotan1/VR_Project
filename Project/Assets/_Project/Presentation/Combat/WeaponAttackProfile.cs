using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    [CreateAssetMenu(menuName = "VR Project/Combat/Weapon Attack Profile", fileName = "WeaponAttackProfile")]
    public sealed class WeaponAttackProfile : ScriptableObject
    {
        [Header("Family")]
        [SerializeField] WeaponFamily _family = WeaponFamily.Hybrid;

        [Header("Session Thresholds")]
        [SerializeField] float _enterLinearSpeed = 1.5f;
        [SerializeField] float _exitLinearSpeed = 0.8f;
        [SerializeField] float _enterAngularSpeed = 120f;
        [SerializeField] float _exitAngularSpeed = 60f;
        [SerializeField] int _exitIdleFramesRequired = 3;
        [SerializeField] float _maxSessionDurationSeconds = 0.55f;

        [Header("Kind Classification")]
        [SerializeField] float _stabForwardDotMin = 0.6f;
        [SerializeField] float _slashSideDotMin = 0.5f;
        [SerializeField] float _bluntMaxAngularSpeed = 90f;
        [SerializeField] float _bluntMinLinearSpeed = 1.5f;

        [Header("Qualifying Hit")]
        [SerializeField] float _minLinearSpeed = 1f;
        [SerializeField] float _referenceLinearSpeed = 3f;
        [SerializeField] float _minQualifyingScore = 0.35f;
        [SerializeField] float _perTargetCooldownSeconds = 0.2f;

        [Header("Feedback")]
        [SerializeField, Range(0f, 1f)] float _hitHapticAmplitude = 0.55f;
        [SerializeField] float _hitHapticDurationSeconds = 0.08f;
        [SerializeField, Range(0f, 1f)] float _blockHapticAmplitude = 0.35f;
        [SerializeField] float _parryWindowSeconds = 0.35f;

        public WeaponFamily Family => _family;
        public float EnterLinearSpeed => _enterLinearSpeed;
        public float ExitLinearSpeed => _exitLinearSpeed;
        public float EnterAngularSpeed => _enterAngularSpeed;
        public float ExitAngularSpeed => _exitAngularSpeed;
        public int ExitIdleFramesRequired => _exitIdleFramesRequired;
        public float MaxSessionDurationSeconds => _maxSessionDurationSeconds;
        public float StabForwardDotMin => _stabForwardDotMin;
        public float SlashSideDotMin => _slashSideDotMin;
        public float BluntMaxAngularSpeed => _bluntMaxAngularSpeed;
        public float BluntMinLinearSpeed => _bluntMinLinearSpeed;
        public float MinLinearSpeed => _minLinearSpeed;
        public float ReferenceLinearSpeed => _referenceLinearSpeed;
        public float MinQualifyingScore => _minQualifyingScore;
        public float PerTargetCooldownSeconds => _perTargetCooldownSeconds;
        public float HitHapticAmplitude => _hitHapticAmplitude;
        public float HitHapticDurationSeconds => _hitHapticDurationSeconds;
        public float BlockHapticAmplitude => _blockHapticAmplitude;
        public float ParryWindowSeconds => _parryWindowSeconds;

#if UNITY_EDITOR
        void OnValidate()
        {
            _enterLinearSpeed = Mathf.Max(0f, _enterLinearSpeed);
            _exitLinearSpeed = Mathf.Max(0f, _exitLinearSpeed);
            _enterAngularSpeed = Mathf.Max(0f, _enterAngularSpeed);
            _exitAngularSpeed = Mathf.Max(0f, _exitAngularSpeed);
            _exitIdleFramesRequired = Mathf.Max(1, _exitIdleFramesRequired);
            _maxSessionDurationSeconds = Mathf.Max(0.05f, _maxSessionDurationSeconds);
            _referenceLinearSpeed = Mathf.Max(_minLinearSpeed + 0.01f, _referenceLinearSpeed);
            _perTargetCooldownSeconds = Mathf.Max(0f, _perTargetCooldownSeconds);
            _parryWindowSeconds = Mathf.Max(0.01f, _parryWindowSeconds);
        }
#endif
    }
}
