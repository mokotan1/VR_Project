using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    [RequireComponent(typeof(PrototypeThirdPersonPlayer))]
    public sealed class UnityChanLocomotionAnimatorBridge : MonoBehaviour
    {
        const string WeaponUpperLayerName = "WeaponUpper";

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int DirectionId = Animator.StringToHash("Direction");
        static readonly int JumpId = Animator.StringToHash("Jump");

        [SerializeField] float _animatorSpeed = 1.35f;
        [SerializeField] Animator _animator;
        [Tooltip("Upper-body layer weight while rifle equipped (not ADS). Uses AR pose clip as rifle-ready proxy.")]
        [SerializeField] float _weaponEquippedLayerWeight = 0.58f;
        [Tooltip("Upper-body layer weight while aiming (RMB). Stronger AR pose for ADS feel.")]
        [SerializeField] float _weaponAimLayerWeight = 0.94f;
        [SerializeField] float _weaponLayerBlendSpeed = 5f;

        PrototypeThirdPersonPlayer _player;
        OsFpsInspiredWeapon _weapon;
        int _weaponUpperLayerIndex = -1;
        float _weaponUpperWeight;

        void Awake()
        {
            _player = GetComponent<PrototypeThirdPersonPlayer>();
            _weapon = GetComponent<OsFpsInspiredWeapon>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
                _weaponUpperLayerIndex = _animator.GetLayerIndex(WeaponUpperLayerName);
        }

        void LateUpdate()
        {
            if (_animator == null)
                return;

            _animator.speed = _animatorSpeed;
            var axes = _player.LocomotionAxes;
            _animator.SetFloat(DirectionId, axes.x);
            _animator.SetFloat(SpeedId, axes.y);

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            // Official controller originally only transitioned Locomotion→Jump with a long exit time;
            // Idle/WalkBack had no jump entry. Controller was extended; allow jump from these states.
            var jumpReady = !_animator.IsInTransition(0) &&
                            (info.IsName("Idle") || info.IsName("Locomotion") || info.IsName("WalkBack"));
            if (_player.TryCommitJumpToAnimator(jumpReady))
                _animator.SetBool(JumpId, true);

            if (info.IsName("Jump") && !_animator.IsInTransition(0))
                _animator.SetBool(JumpId, false);

            UpdateWeaponUpperLayer();
        }

        void UpdateWeaponUpperLayer()
        {
            if (_weaponUpperLayerIndex < 0)
                return;

            var target = 0f;
            if (_weapon != null && _weapon.IsEquipped)
                target = _player.IsAiming ? _weaponAimLayerWeight : _weaponEquippedLayerWeight;

            _weaponUpperWeight = Mathf.MoveTowards(
                _weaponUpperWeight,
                target,
                Time.deltaTime * _weaponLayerBlendSpeed);
            _animator.SetLayerWeight(_weaponUpperLayerIndex, _weaponUpperWeight);
        }
    }
}
