using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SuperhotEnemyBrain : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] float _hearingRadius = 12f;
        [SerializeField] float _losRange = 20f;
        [SerializeField] float _movementReactionRadius = 32f;
        [SerializeField] float _playerMoveReactionThreshold = 0.04f;
        [SerializeField] LayerMask _obstacleMask = ~0;

        [Header("Movement Speed")]
        [SerializeField] float _flankSpeed = 2.5f;
        [SerializeField] float _strafeSpeed = 2f;
        [SerializeField] float _closeSpeed = 3f;

        [Header("Flanking")]
        [SerializeField] float _cornerSearchRadius = 5f;
        [SerializeField] int _cornerCandidateCount = 8;

        [Header("Close Range")]
        [SerializeField] float _closePressureRange = 6f;
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
        IUnityChanLocomotionMotor _unityChanMotor;
        Transform _playerTransform;
        Vector3 _lastSoundOrigin;
        Vector3 _lastPlayerPosition;
        bool _hasLastPlayerPosition;
        float _losConfirmTimer;
        float _losLostTimer;
        float _flankSearchCooldown;
        Vector3? _cachedFlankCorner;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = false;
            _agent.updateRotation = false;

            _legacyMover = GetComponent<SuperhotEnemyMover>();
            if (_legacyMover != null)
                _legacyMover.enabled = false;
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
            TrackPlayerMovement();

            if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
                return;

            _agent.nextPosition = transform.position;

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            Tick_PassiveAwareness();

            switch (_state)
            {
                case EnemyState.Idle:
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

        void OnSoundHeard(SuperhotSoundEvent e)
        {
            if (_state == EnemyState.CloseRange)
                return;

            var radius = Mathf.Max(_hearingRadius, e.Radius);
            if (Vector3.Distance(transform.position, e.Origin) > radius)
                return;

            _lastSoundOrigin = e.Origin;
            SetState(EnemyState.Investigating);
        }

        void Tick_PassiveAwareness()
        {
            if (_state != EnemyState.Idle || _playerTransform == null)
                return;

            if (CheckAndEnterCloseRange())
                return;

            if (HasLOS())
            {
                SetState(EnemyState.Engaging);
                return;
            }

            if (!PlayerMovedWithinReactionRadius())
                return;

            _lastSoundOrigin = _playerTransform.position;
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
                _agent.SetDestination(corner.Value);
                SetState(EnemyState.FlankToCorner);
            }
            else
            {
                _agent.SetDestination(_lastSoundOrigin);
                SetState(EnemyState.FlankToCorner);
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

            if (DistanceToPlayer() <= _closePressureRange)
            {
                _agent.SetDestination(_playerTransform.position);
                MoveAlongPath(dt, _closeSpeed);
                FacePlayer();
                return;
            }

            var strafeDir = ComputeStrafeDir();
            if (strafeDir.sqrMagnitude > 1e-4f)
            {
                var targetPos = transform.position + strafeDir * (_strafeSpeed * dt * 5f);
                if (NavMesh.SamplePosition(targetPos, out var hit, 1f, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);
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

            _agent.SetDestination(_playerTransform.position);
            MoveAlongPath(dt, _closeSpeed);
            FacePlayer();
        }

        void MoveAlongPath(float dt, float speed)
        {
            if (_agent.pathPending || !_agent.hasPath || _agent.remainingDistance < 0.05f)
                return;

            var desiredVel = _agent.desiredVelocity;
            if (desiredVel.sqrMagnitude < 1e-4f)
            {
                desiredVel = _agent.steeringTarget - transform.position;
                desiredVel.y = 0f;
            }

            if (desiredVel.sqrMagnitude < 1e-4f)
                desiredVel = _agent.destination - transform.position;
            desiredVel.y = 0f;
            if (desiredVel.sqrMagnitude < 1e-4f)
                return;
            transform.position += desiredVel.normalized * (speed * dt);
            _agent.nextPosition = transform.position;
        }

        void FacePlayer()
        {
            if (_playerTransform == null)
                return;

            var dir = _playerTransform.position - transform.position;
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

        float DistanceToPlayer()
        {
            if (_playerTransform == null)
                return float.MaxValue;

            return Vector3.Distance(transform.position, _playerTransform.position);
        }

        void TrackPlayerMovement()
        {
            if (_playerTransform == null)
                return;

            if (!_hasLastPlayerPosition)
            {
                _lastPlayerPosition = _playerTransform.position;
                _hasLastPlayerPosition = true;
            }
        }

        bool PlayerMovedWithinReactionRadius()
        {
            if (_playerTransform == null || !_hasLastPlayerPosition)
                return false;

            var moved = Vector3.Distance(_playerTransform.position, _lastPlayerPosition);
            _lastPlayerPosition = _playerTransform.position;
            if (moved < _playerMoveReactionThreshold)
                return false;

            return Vector3.Distance(transform.position, _playerTransform.position) <= _movementReactionRadius;
        }

        void ApplyTakedown()
        {
            if (_flatPlayer != null)
                _flatPlayer.SpeedMultiplier = _takedownSpeedPenalty;
            _unityChanMotor?.SetMotorLocked(true);

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
            _unityChanMotor?.SetMotorLocked(false);
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

        public float DebugRemainingDistance => DebugAgentIsUsable ? DebugAgent.remainingDistance : 0f;

        public bool DebugHasPath => DebugAgentIsUsable && DebugAgent.hasPath;

        public bool DebugPathPending => DebugAgentIsUsable && DebugAgent.pathPending;

        public bool DebugAgentStopped => DebugAgentIsUsable && DebugAgent.isStopped;

        public Vector3 DebugDesiredVelocity => DebugAgentIsUsable ? DebugAgent.desiredVelocity : Vector3.zero;

        public Vector3 DebugNavDestination => DebugAgentIsUsable ? DebugAgent.destination : Vector3.zero;

        public bool DebugAgentIsUsable => DebugAgent != null && DebugAgent.isActiveAndEnabled && DebugAgent.isOnNavMesh;

        NavMeshAgent DebugAgent
        {
            get
            {
                if (_agent == null)
                    TryGetComponent(out _agent);
                return _agent;
            }
        }

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
                    {
                        _playerTransform = playerGo.transform;
                        _unityChanMotor = FindUnityChanMotor(playerGo);
                    }
                }
            }
        }

        static IUnityChanLocomotionMotor FindUnityChanMotor(GameObject playerGo)
        {
            if (playerGo == null)
                return null;

            foreach (var mb in playerGo.GetComponents<MonoBehaviour>())
            {
                if (mb is IUnityChanLocomotionMotor motor)
                    return motor;
            }

            return null;
        }
    }
}
