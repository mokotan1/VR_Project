using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SuperhotEnemyBrain : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] float _hearingRadius = 12f;
        [SerializeField] float _losRange = 20f;
        [SerializeField] LayerMask _obstacleMask = ~0;

        [Header("Movement Speed")]
        [SerializeField] float _flankSpeed = 2.5f;
        [SerializeField] float _strafeSpeed = 2f;
        [SerializeField] float _closeSpeed = 3f;

        [Header("Flanking")]
        [SerializeField] float _cornerSearchRadius = 5f;
        [SerializeField] int _cornerCandidateCount = 8;

        [Header("Close Range")]
        [SerializeField] float _takedownRange = 1.5f;
        [SerializeField] float _takedownSpeedPenalty = 0.25f;

        [Header("Player References")]
        [Tooltip("플레이어 총의 FirePoint Transform. 비우면 장착 중인 무기가 브로드캐스트한 총구를 사용하고, 그것도 없으면 플레이어 몸통 right")]
        [SerializeField] Transform _playerFirePoint;

        enum EnemyState { Idle, Investigating, FlankToCorner, Engaging, CloseRange }
        EnemyState _state = EnemyState.Idle;

        NavMeshAgent _agent;
        IGameplayClock _clock;
        SuperhotEnemyMover _legacyMover;
        SuperhotFlatFpsController _flatPlayer;
        Transform _playerTransform;
        Vector3 _lastSoundOrigin;
        float _losConfirmTimer;
        float _losLostTimer;
        float _flankSearchCooldown;
        Vector3? _cachedFlankCorner;

        CrystalDefenseEnemyObjective _defenseObjective;
        CrystalDefenseEnemyAttack _defenseAttack;

        bool _navWarningLogged;
        const float NavMeshSampleRadius = 5f;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = false;
            _agent.updateRotation = false;

            _legacyMover = GetComponent<SuperhotEnemyMover>();
            if (_legacyMover != null)
                _legacyMover.enabled = false;

            _defenseObjective = GetComponent<CrystalDefenseEnemyObjective>();
            _defenseAttack = GetComponent<CrystalDefenseEnemyAttack>();
        }

        void Start()
        {
            TryPlaceOnNavMesh();
        }

        void OnEnable()
        {
            SuperhotSoundChannel.OnSoundEmitted += OnSoundHeard;

            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        void OnDisable()
        {
            SuperhotSoundChannel.OnSoundEmitted -= OnSoundHeard;
            ReleaseTakedown();
        }

        void Update()
        {
            RefreshPlayerRef();
            _agent.nextPosition = transform.position;

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            var playerVisible = HasLOS();

            if (TryTickCrystalDefense(dt, playerVisible))
                return;

            switch (_state)
            {
                case EnemyState.Idle:
                    if (playerVisible)
                        SetState(EnemyState.Engaging);
                    break;
                case EnemyState.Investigating:
                    Tick_Investigating();
                    break;
                case EnemyState.FlankToCorner:
                    Tick_FlankToCorner(dt);
                    break;
                case EnemyState.Engaging:
                    Tick_Engaging(dt);
                    break;
                case EnemyState.CloseRange:
                    Tick_CloseRange(dt);
                    break;
            }
        }

        bool TryTickCrystalDefense(float dt, bool playerVisible)
        {
            if (_defenseObjective == null || _defenseAttack == null)
                return false;

            var targetKind = _defenseObjective.RefreshTarget(playerVisible);
            if (targetKind != CrystalDefenseTargetKind.Crystal)
                return false;

            var crystal = _defenseObjective.Crystal;
            if (crystal == null)
                return false;

            TrySetDestination(crystal.transform.position);
            MoveAlongPath(dt, _closeSpeed);
            FaceTarget(crystal.transform);
            _defenseAttack.TryAttackCrystal(crystal, transform.position + Vector3.up);
            return true;
        }

        void OnSoundHeard(SuperhotSoundEvent e)
        {
            if (_state == EnemyState.CloseRange)
                return;

            if (Vector3.Distance(transform.position, e.Origin) > _hearingRadius)
                return;

            _lastSoundOrigin = e.Origin;
            SetState(EnemyState.Investigating);
        }

        void Tick_Investigating()
        {
            if (_playerTransform == null)
                return;

            if (HasLOS())
            {
                _losConfirmTimer += Time.deltaTime;
                if (_losConfirmTimer >= 0.2f)
                {
                    _losConfirmTimer = 0f;
                    SetState(EnemyState.Engaging);
                }
                return;
            }

            _losConfirmTimer = 0f;

            var corner = FindFlankCorner();
            if (corner.HasValue)
            {
                if (TrySetDestination(corner.Value))
                    SetState(EnemyState.FlankToCorner);
            }
            else
            {
                if (TrySetDestination(_lastSoundOrigin))
                    SetState(EnemyState.Engaging);
            }
        }

        void Tick_FlankToCorner(float dt)
        {
            if (CheckAndEnterCloseRange())
                return;

            if (HasLOS())
            {
                SetState(EnemyState.Engaging);
                return;
            }

            MoveAlongPath(dt, _flankSpeed);
        }

        void Tick_Engaging(float dt)
        {
            if (CheckAndEnterCloseRange())
                return;

            if (!HasLOS())
            {
                _losLostTimer += Time.deltaTime;
                if (_losLostTimer >= 0.3f)
                {
                    _losLostTimer = 0f;
                    SetState(EnemyState.Investigating);
                }
                return;
            }

            _losLostTimer = 0f;

            if (_defenseObjective != null && _defenseAttack != null)
            {
                var targetKind = _defenseObjective.RefreshTarget(playerVisible: true);
                if (targetKind == CrystalDefenseTargetKind.Crystal)
                {
                    var crystal = _defenseObjective.Crystal;
                    if (crystal != null)
                    {
                        TrySetDestination(crystal.transform.position);
                        MoveAlongPath(dt, _closeSpeed);
                        FaceTarget(crystal.transform);
                        _defenseAttack.TryAttackCrystal(crystal, transform.position + Vector3.up);
                        return;
                    }
                }
            }

            var strafeDir = ComputeStrafeDir();
            if (strafeDir.sqrMagnitude > 1e-4f)
            {
                var targetPos = transform.position + strafeDir * (_strafeSpeed * dt * 5f);
                if (NavMesh.SamplePosition(targetPos, out var hit, 1f, NavMesh.AllAreas))
                    TrySetDestination(hit.position);
            }

            MoveAlongPath(dt, _strafeSpeed);
            FacePlayer();
        }

        void Tick_CloseRange(float dt)
        {
            if (_playerTransform == null || !HasLOS())
            {
                SetState(EnemyState.Idle);
                return;
            }

            TrySetDestination(_playerTransform.position);
            MoveAlongPath(dt, _closeSpeed);
            FacePlayer();
        }

        void MoveAlongPath(float dt, float speed)
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            if (_agent.pathPending || !_agent.hasPath || _agent.remainingDistance < 0.05f)
                return;

            var desiredVel = _agent.desiredVelocity;
            if (desiredVel.sqrMagnitude < 1e-4f)
                return;

            transform.position += desiredVel.normalized * (speed * dt);
        }

        bool TrySetDestination(Vector3 destination)
        {
            if (_agent == null || !_agent.isActiveAndEnabled)
                return false;

            if (!_agent.isOnNavMesh && !TryPlaceOnNavMesh())
            {
                WarnNavMeshMissingOnce();
                return false;
            }

            return _agent.SetDestination(destination);
        }

        bool TryPlaceOnNavMesh()
        {
            if (_agent == null || !_agent.isActiveAndEnabled)
                return false;

            if (_agent.isOnNavMesh)
                return true;

            if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
                return _agent.Warp(hit.position);

            return false;
        }

        void WarnNavMeshMissingOnce()
        {
            if (_navWarningLogged)
                return;

            _navWarningLogged = true;
            Debug.LogWarning(
                $"[SuperhotEnemyBrain] '{name}' is not on a baked NavMesh (sample radius {NavMeshSampleRadius}m). " +
                "AI movement disabled until a NavMesh is baked for this scene. " +
                "(Window → AI → Navigation → Bake, or add a NavMeshSurface component.)",
                this);
        }

        void FacePlayer()
        {
            FaceTarget(_playerTransform);
        }

        void FaceTarget(Transform target)
        {
            if (target == null)
                return;

            var dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        bool HasLOS()
        {
            if (_playerTransform == null)
                return false;

            var from = transform.position + Vector3.up * 1.5f;
            var to = _playerTransform.position + Vector3.up * 1.5f;
            var diff = to - from;

            if (diff.sqrMagnitude > _losRange * _losRange)
                return false;

            return !Physics.Raycast(from, diff.normalized, diff.magnitude, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        Vector3? FindFlankCorner()
        {
            _flankSearchCooldown -= Time.deltaTime;
            if (_flankSearchCooldown > 0f)
                return _cachedFlankCorner;

            _flankSearchCooldown = 0.2f;

            if (_playerTransform == null)
            {
                _cachedFlankCorner = null;
                return null;
            }

            Vector3? best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _cornerCandidateCount; i++)
            {
                var angle = i * (360f / _cornerCandidateCount);
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var candidate = transform.position + dir * _cornerSearchRadius;

                if (!NavMesh.SamplePosition(candidate, out var navHit, 1.5f, NavMesh.AllAreas))
                    continue;

                var pos = navHit.position;

                var fromPlayer = _playerTransform.position + Vector3.up * 1.5f;
                var toPos = pos + Vector3.up * 1.5f - fromPlayer;
                bool playerCanSeeSpot = !Physics.Raycast(fromPlayer, toPos.normalized, toPos.magnitude, _obstacleMask, QueryTriggerInteraction.Ignore);
                if (playerCanSeeSpot)
                    continue;

                var distFromSelf = Vector3.Distance(transform.position, pos);
                if (distFromSelf < bestDist)
                {
                    bestDist = distFromSelf;
                    best = pos;
                }
            }

            _cachedFlankCorner = best;
            return best;
        }

        Vector3 ComputeStrafeDir()
        {
            Vector3 gunRight;

            var firePoint = _playerFirePoint != null ? _playerFirePoint : PlayerWeaponFirePointForAi.ActiveMuzzle;
            if (firePoint != null)
                gunRight = firePoint.right;
            else if (_playerTransform != null)
                gunRight = _playerTransform.right;
            else
                return Vector3.zero;

            gunRight.y = 0f;
            return gunRight.sqrMagnitude > 1e-4f ? -gunRight.normalized : Vector3.zero;
        }

        bool CheckAndEnterCloseRange()
        {
            if (_playerTransform == null)
                return false;

            if (Vector3.Distance(transform.position, _playerTransform.position) > _takedownRange)
                return false;

            SetState(EnemyState.CloseRange);
            return true;
        }

        void ApplyTakedown()
        {
            if (_flatPlayer != null)
                _flatPlayer.SpeedMultiplier = _takedownSpeedPenalty;

            if (XRSettings.isDeviceActive)
            {
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand)
                    .SendHapticImpulse(0, 0.8f, 0.3f);
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
                    .SendHapticImpulse(0, 0.8f, 0.3f);
            }
        }

        void ReleaseTakedown()
        {
            if (_flatPlayer != null)
                _flatPlayer.SpeedMultiplier = 1f;
        }

        void SetState(EnemyState next)
        {
            if (_state == next)
                return;

            if (_state == EnemyState.CloseRange)
                ReleaseTakedown();

            _state = next;

            if (_state == EnemyState.CloseRange)
                ApplyTakedown();
        }

        /// <summary>개발자 HUD용 — 적 이동·내비 상태.</summary>
        public string DebugStateName => _state.ToString();

        public float DebugRemainingDistance => NavMeshAgentIsQueryable ? _agent.remainingDistance : 0f;

        public bool DebugHasPath => NavMeshAgentIsQueryable && _agent.hasPath;

        public bool DebugPathPending => NavMeshAgentIsQueryable && _agent.pathPending;

        public bool DebugAgentStopped => NavMeshAgentIsQueryable && _agent.isStopped;

        public Vector3 DebugDesiredVelocity => NavMeshAgentIsQueryable ? _agent.desiredVelocity : Vector3.zero;

        public Vector3 DebugNavDestination => NavMeshAgentIsQueryable ? _agent.destination : Vector3.zero;

        bool NavMeshAgentIsQueryable =>
            _agent != null && _agent.enabled && _agent.isOnNavMesh;

        void RefreshPlayerRef()
        {
            if (_playerTransform != null)
                return;

            if (XRSettings.isDeviceActive)
            {
                var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.Camera != null)
                    _playerTransform = origin.Camera.transform;
            }
            else
            {
                var flat = FindAnyObjectByType<SuperhotFlatFpsController>();
                if (flat != null)
                {
                    _playerTransform = flat.transform;
                    _flatPlayer = flat;
                }
                else
                {
                    var playerGo = GameObject.FindGameObjectWithTag("Player");
                    if (playerGo != null)
                        _playerTransform = playerGo.transform;
                }
            }
        }
    }
}
