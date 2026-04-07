using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Runs after <see cref="PrototypeThirdPersonPlayer"/> / <see cref="DyrdaFirstPersonMotorAdapter"/> so
    /// <see cref="IUnityChanLocomotionMotor.LocomotionAxes"/> is current for this frame, then Mecanim reads parameters on the same frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class UnityChanLocomotionAnimatorBridge : MonoBehaviour
    {
        const string WeaponUpperLayerName = "WeaponUpper";

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int DirectionId = Animator.StringToHash("Direction");
        static readonly int JumpId = Animator.StringToHash("Jump");
        static readonly int WeaponLocomotionBlendId = Animator.StringToHash("WeaponLocomotionBlend");
        static readonly int WeaponFireId = Animator.StringToHash("WeaponFire");
        static readonly int WeaponEquippedId = Animator.StringToHash("WeaponEquipped");

        [SerializeField] float _animatorSpeed = 1.35f;
        [SerializeField] Animator _animator;
        [Tooltip("장착 시 WeaponUpper 레이어 가중치. 1 미만이면 마스크 본에 기본 로코 팔 스윙이 섞입니다.")]
        [SerializeField] float _weaponEquippedLayerWeight = 1f;
        [Tooltip("조준(RMB) 시 상체 레이어 가중치.")]
        [SerializeField] float _weaponAimLayerWeight = 1f;
        [Tooltip("장착 상태에서 조준↔비조준 사이만 부드럽게. 장착/해제는 즉시 끊김.")]
        [SerializeField] float _weaponLayerBlendSpeed = 14f;
        [Tooltip(
            "Mecanim Speed(로코모션) 절댓값이 이 구간을 지나며 WeaponLocomotionBlend가 0→1로 올라가 ARpose1↔ARpose2로 섞입니다. " +
            "WalkBack(음수 Speed)도 절댓값으로 반영합니다.")]
        [SerializeField] float _weaponRunBlendSpeedStart = 0f;
        [SerializeField] float _weaponRunBlendSpeedEnd = 0.38f;
        [SerializeField] float _weaponLocomotionBlendSmoothHz = 8f;
        [Tooltip("켜면 AnimatorCullingMode.AlwaysAnimate — Scene 뷰·카메라 각도에 따라 애니가 멈춘 것처럼 보이는 현상을 줄입니다.")]
        [SerializeField] bool _alwaysAnimate = true;

        IUnityChanLocomotionMotor _motor;
        OsFpsInspiredWeapon _weapon;
        int _weaponUpperLayerIndex = -1;
        float _weaponUpperWeight;
        float _weaponLocomotionBlendSmoothed;
        float _lastConsumedWeaponFireUnscaledTime = -9999f;
        bool _wasWeaponEquipped;
        bool _warnedMissingWeaponUpperLayer;

        void Awake()
        {
            _motor = UnityChanLocomotionMotorResolver.ResolveOn(gameObject);
            _weapon = GetComponent<OsFpsInspiredWeapon>();
            RefreshWeaponUpperLayerIndex();
            if (_weapon != null)
                _lastConsumedWeaponFireUnscaledTime = _weapon.LastFireUnscaledTime;
        }

        void OnEnable()
        {
            RefreshWeaponUpperLayerIndex();
            SyncWeaponUpperLayerFromEquippedState(force: true);
        }

        void Start()
        {
            RefreshWeaponUpperLayerIndex();
            SyncWeaponUpperLayerFromEquippedState(force: true);
            if (_alwaysAnimate && _animator != null)
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        void RefreshWeaponUpperLayerIndex()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                return;

            _weaponUpperLayerIndex = _animator.GetLayerIndex(WeaponUpperLayerName);
            if (_weaponUpperLayerIndex < 0 && !_warnedMissingWeaponUpperLayer)
            {
                _warnedMissingWeaponUpperLayer = true;
                Debug.LogWarning(
                    "[UnityChanLocomotionAnimatorBridge] Animator에 '" + WeaponUpperLayerName +
                    "' 레이어가 없습니다. 컨트롤러에 레이어·이름·Mask(weapon upper body mask)를 확인하세요.",
                    _animator);
            }
        }

        /// <summary>레이어 인덱스가 늦게 잡히거나 비활성화 후 재활성화될 때 가중치·파라미터를 즉시 맞춥니다.</summary>
        void SyncWeaponUpperLayerFromEquippedState(bool force)
        {
            if (_animator == null)
                return;
            if (_weapon == null)
                _weapon = GetComponent<OsFpsInspiredWeapon>();

            var equipped = _weapon != null && _weapon.IsEquipped;
            if (force)
                _wasWeaponEquipped = equipped;

            _animator.SetBool(WeaponEquippedId, equipped);

            if (_weaponUpperLayerIndex < 0)
                return;

            if (equipped)
            {
                var aim = _motor != null && _motor.IsAiming;
                _weaponUpperWeight = aim ? _weaponAimLayerWeight : _weaponEquippedLayerWeight;
                _animator.SetLayerWeight(_weaponUpperLayerIndex, _weaponUpperWeight);
            }
            else
            {
                _weaponUpperWeight = 0f;
                _weaponLocomotionBlendSmoothed = 0f;
                _animator.SetLayerWeight(_weaponUpperLayerIndex, 0f);
                _animator.SetFloat(WeaponLocomotionBlendId, 0f);
            }

            _animator.Update(0f);
        }

        void Update()
        {
            if (_animator == null || _motor == null)
                return;

            _animator.speed = _animatorSpeed;
            var axes = _motor.LocomotionAxes;
            _animator.SetFloat(DirectionId, axes.x);
            _animator.SetFloat(SpeedId, axes.y);

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            // Official controller originally only transitioned Locomotion→Jump with a long exit time;
            // Idle/WalkBack had no jump entry. Controller was extended; allow jump from these states.
            var jumpReady = !_animator.IsInTransition(0) &&
                            (info.IsName("Idle") || info.IsName("Locomotion") || info.IsName("WalkBack"));
            if (_motor.TryCommitJumpToAnimator(jumpReady))
                _animator.SetBool(JumpId, true);

            if (info.IsName("Jump") && !_animator.IsInTransition(0))
                _animator.SetBool(JumpId, false);

            _animator.SetBool(WeaponEquippedId, _weapon != null && _weapon.IsEquipped);

            var equippedNow = _weapon != null && _weapon.IsEquipped;
            if (equippedNow != _wasWeaponEquipped)
            {
                _wasWeaponEquipped = equippedNow;
                if (!equippedNow)
                {
                    _weaponUpperWeight = 0f;
                    _weaponLocomotionBlendSmoothed = 0f;
                    if (_weaponUpperLayerIndex >= 0)
                    {
                        _animator.SetLayerWeight(_weaponUpperLayerIndex, 0f);
                        _animator.SetFloat(WeaponLocomotionBlendId, 0f);
                    }
                }
                else if (_weaponUpperLayerIndex >= 0)
                {
                    _weaponUpperWeight = _motor.IsAiming ? _weaponAimLayerWeight : _weaponEquippedLayerWeight;
                    _animator.SetLayerWeight(_weaponUpperLayerIndex, _weaponUpperWeight);
                }

                if (_animator != null && _weaponUpperLayerIndex >= 0)
                    _animator.Update(0f);
            }

            UpdateWeaponUpperLayer();
            UpdateWeaponUpperMotionBlend(axes.y);
            TryFireWeaponUpperAnimation();
        }

        void UpdateWeaponUpperMotionBlend(float locomotionAnimatorSpeed)
        {
            if (_animator == null || _weaponUpperLayerIndex < 0)
                return;

            var target = 0f;
            if (_weapon != null && _weapon.IsEquipped)
            {
                var a = _weaponRunBlendSpeedStart;
                var b = Mathf.Max(a + 0.01f, _weaponRunBlendSpeedEnd);
                var speedMag = Mathf.Abs(locomotionAnimatorSpeed);
                target = Mathf.Clamp01((speedMag - a) / (b - a));
            }

            if (_weapon == null || !_weapon.IsEquipped)
            {
                _weaponLocomotionBlendSmoothed = 0f;
                _animator.SetFloat(WeaponLocomotionBlendId, 0f);
                return;
            }

            var k = 1f - Mathf.Exp(-_weaponLocomotionBlendSmoothHz * Time.deltaTime);
            _weaponLocomotionBlendSmoothed = Mathf.Lerp(_weaponLocomotionBlendSmoothed, target, k);
            _animator.SetFloat(WeaponLocomotionBlendId, _weaponLocomotionBlendSmoothed);
        }

        void TryFireWeaponUpperAnimation()
        {
            if (_animator == null || _weaponUpperLayerIndex < 0 || _weapon == null || !_weapon.IsEquipped)
                return;

            var t = _weapon.LastFireUnscaledTime;
            if (Mathf.Approximately(t, _lastConsumedWeaponFireUnscaledTime))
                return;
            _lastConsumedWeaponFireUnscaledTime = t;
            _animator.SetTrigger(WeaponFireId);
        }

        void UpdateWeaponUpperLayer()
        {
            if (_weaponUpperLayerIndex < 0)
                return;

            var target = 0f;
            if (_weapon != null && _weapon.IsEquipped)
                target = _motor.IsAiming ? _weaponAimLayerWeight : _weaponEquippedLayerWeight;

            _weaponUpperWeight = Mathf.MoveTowards(
                _weaponUpperWeight,
                target,
                Time.deltaTime * _weaponLayerBlendSpeed);
            _animator.SetLayerWeight(_weaponUpperLayerIndex, _weaponUpperWeight);
        }
    }
}
