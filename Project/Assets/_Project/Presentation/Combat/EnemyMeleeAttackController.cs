using System;
using UnityEngine;
using VRProject.Application.Combat;
using VRProject.Presentation.Gameplay;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeAttackController : MonoBehaviour
    {
        const float MinAttackRange = 0.1f;
        const float MinPhaseSeconds = 0.01f;

        [SerializeField] float _attackRange = 1.6f;
        [SerializeField] float _windUpSeconds = 0.6f;
        [SerializeField] float _activeSeconds = 0.15f;
        [SerializeField] float _recoverySeconds = 0.8f;
        [SerializeField] float _blockStunSeconds = 1.5f;
        [SerializeField] EnemyMeleeHitbox _hitbox;

        Transform _target;
        EnemyAttackState _state = EnemyAttackState.Idle;
        float _stunnedUntil;
        bool _playerHitThisActive;

        public EnemyAttackPhase Phase => _state.Phase;

        public bool IsIdle => _state.Phase == EnemyAttackPhase.Idle && !IsStunned;

        public bool IsAttacking => EnemyAttackSessionLogic.IsAttacking(_state.Phase);

        public bool IsHitboxActive => EnemyAttackSessionLogic.IsHitboxActive(_state.Phase);

        public bool PlayerHitRegistered => _playerHitThisActive;

        public Vector3 ApproachDirection
        {
            get
            {
                var forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            }
        }

        public event Action WindUpStarted;
        public event Action ActiveStarted;
        public event Action AttackCompleted;
        public event Action Blocked;

        EnemyMeleeAttackTimings Timings =>
            new EnemyMeleeAttackTimings(_windUpSeconds, _activeSeconds, _recoverySeconds);

        void Awake()
        {
            if (_hitbox == null)
                _hitbox = GetComponentInChildren<EnemyMeleeHitbox>(true);

            if (_hitbox != null)
                _hitbox.Bind(this);

            EnsureKinematicRigidbody();
            SetHitboxEnabled(false);
        }

        static void EnsureKinematicRigidbodyOn(GameObject root)
        {
            if (root == null)
                return;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null)
                rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        void EnsureKinematicRigidbody() => EnsureKinematicRigidbodyOn(gameObject);

        public void SetTarget(Transform target) => _target = target;

        public bool IsTargetInRange()
        {
            if (_target == null)
                return false;

            var flatOffset = _target.position - transform.position;
            flatOffset.y = 0f;
            return flatOffset.magnitude <= _attackRange;
        }

        public bool TryBeginAttack()
        {
            if (IsStunned || _target == null)
                return false;

            var distance = Vector3.Distance(transform.position, _target.position);
            if (!EnemyAttackSessionLogic.CanBeginAttack(_state, distance, _attackRange, hasTarget: true))
                return false;

            _state = EnemyAttackSessionLogic.BeginAttack(_state);
            _playerHitThisActive = false;
            SetHitboxEnabled(false);
            WindUpStarted?.Invoke();
            return true;
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (IsStunned)
            {
                SetHitboxEnabled(false);
                return;
            }

            if (_state.Phase == EnemyAttackPhase.Idle)
                return;

            var previousPhase = _state.Phase;
            var result = EnemyAttackSessionLogic.Advance(_state, Timings, deltaTimeSeconds);
            _state = result.NextState;

            if (result.EnteredActive)
            {
                _playerHitThisActive = false;
                SetHitboxEnabled(true);
                ActiveStarted?.Invoke();
            }
            else if (previousPhase == EnemyAttackPhase.Active && _state.Phase != EnemyAttackPhase.Active)
            {
                SetHitboxEnabled(false);
            }

            if (result.AttackCompleted)
                AttackCompleted?.Invoke();
        }

        public void RegisterPlayerHit(SuperhotPlaytestPlayerHealth player)
        {
            if (!IsHitboxActive || _playerHitThisActive || player == null || !player.IsAlive)
                return;

            _playerHitThisActive = true;
            player.ApplyHit();
        }

        public void RegisterBlocked()
        {
            if (!IsAttacking)
                return;

            _playerHitThisActive = true;
            _state = EnemyAttackState.Idle;
            _stunnedUntil = Time.time + _blockStunSeconds;
            SetHitboxEnabled(false);
            Blocked?.Invoke();
        }

        bool IsStunned => Time.time < _stunnedUntil;

        void SetHitboxEnabled(bool enabled)
        {
            if (_hitbox != null)
                _hitbox.SetActive(enabled);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _attackRange = Mathf.Max(MinAttackRange, _attackRange);
            _windUpSeconds = Mathf.Max(MinPhaseSeconds, _windUpSeconds);
            _activeSeconds = Mathf.Max(MinPhaseSeconds, _activeSeconds);
            _recoverySeconds = Mathf.Max(MinPhaseSeconds, _recoverySeconds);
            _blockStunSeconds = Mathf.Max(MinPhaseSeconds, _blockStunSeconds);
        }
#endif
    }
}
