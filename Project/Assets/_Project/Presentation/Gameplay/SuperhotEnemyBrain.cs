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
        [Tooltip("플레이어 총의 FirePoint Transform. 없으면 플레이어 Transform.right 사용")]
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
            _agent.nextPosition = transform.position;

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;

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
                _agent.SetDestination(corner.Value);
                SetState(EnemyState.FlankToCorner);
            }
            else
            {
                _agent.SetDestination(_lastSoundOrigin);
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
                return;

            transform.position += desiredVel.normalized * (speed * dt);
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

            if (_playerFirePoint != null)
                gunRight = _playerFirePoint.right;
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
