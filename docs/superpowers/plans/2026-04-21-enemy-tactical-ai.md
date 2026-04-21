# Enemy Tactical AI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 소리 기반 지각 → LOS 판단 → 코너 숨기/전술 사격 이동 → 근접 제압으로 이어지는 적 AI 상태 머신 구현

**Architecture:** `SuperhotSoundChannel` 정적 이벤트 버스로 플레이어 발소리를 브로드캐스트하고, `SuperhotEnemyBrain`이 상태 머신을 통해 Mover를 대체하여 NavMeshAgent를 SimulationDeltaTime으로 수동 구동한다. 근접 제압은 `SuperhotFlatFpsController`에 `SpeedMultiplier`를 추가해 반영한다.

**Tech Stack:** Unity NavMesh (NavMeshAgent, NavMesh.SamplePosition), Physics.Raycast, IGameplayClock, C# static events

**Prerequisites:**
- 씬에 NavMesh가 베이크되어 있어야 함 (Window → AI → Navigation → Bake)
- 적 GameObject에 NavMeshAgent 컴포넌트 추가 필요

---

## 파일 구조

| 파일 | 역할 |
|------|------|
| **신규** `SuperhotSoundChannel.cs` | 정적 이벤트 버스 + SuperhotSoundEvent 구조체 |
| **신규** `SuperhotPlayerSoundEmitter.cs` | 플레이어 이동 감지 → 소리 이벤트 발송 |
| **신규** `SuperhotEnemyBrain.cs` | 5상태 적 AI 상태 머신 (Mover 대체) |
| **수정** `SuperhotFlatFpsController.cs` | `SpeedMultiplier` 프로퍼티 추가 |

---

## Task 1: 소리 이벤트 시스템

**Files:**
- Create: `Project/Assets/_Project/Presentation/Gameplay/SuperhotSoundChannel.cs`
- Create: `Project/Assets/_Project/Presentation/Gameplay/SuperhotPlayerSoundEmitter.cs`

- [ ] **Step 1: SuperhotSoundChannel.cs 생성**

```csharp
using System;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public readonly struct SuperhotSoundEvent
    {
        public readonly Vector3 Origin;
        public readonly float Radius;

        public SuperhotSoundEvent(Vector3 origin, float radius)
        {
            Origin = origin;
            Radius = radius;
        }
    }

    public static class SuperhotSoundChannel
    {
        public static event Action<SuperhotSoundEvent> OnSoundEmitted;

        public static void Emit(SuperhotSoundEvent e) => OnSoundEmitted?.Invoke(e);
    }
}
```

- [ ] **Step 2: SuperhotPlayerSoundEmitter.cs 생성**

```csharp
using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SuperhotPlayerSoundEmitter : MonoBehaviour
    {
        [SerializeField] float _soundRadius = 12f;
        [SerializeField] float _moveSpeedThreshold = 0.15f;
        [SerializeField] float _emitInterval = 0.15f;

        SuperhotFlatFpsController _flatController;
        float _emitTimer;
        Vector3 _lastHmdPos;
        Transform _hmd;

        void Awake()
        {
            _flatController = GetComponent<SuperhotFlatFpsController>();
        }

        void OnEnable()
        {
            if (XRSettings.isDeviceActive)
            {
                var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.Camera != null)
                {
                    _hmd = origin.Camera.transform;
                    _lastHmdPos = _hmd.position;
                }
            }
        }

        void Update()
        {
            _emitTimer -= Time.unscaledDeltaTime;
            if (_emitTimer > 0f)
                return;

            var speed = ComputeSpeed();
            if (speed < _moveSpeedThreshold)
                return;

            _emitTimer = _emitInterval;
            SuperhotSoundChannel.Emit(new SuperhotSoundEvent(transform.position, _soundRadius));
        }

        float ComputeSpeed()
        {
            if (_flatController != null)
                return _flatController.LastPlanarSpeedMetersPerSecond;

            if (_hmd != null)
            {
                var udt = Mathf.Max(Time.unscaledDeltaTime, 1e-6f);
                var speed = (_hmd.position - _lastHmdPos).magnitude / udt;
                _lastHmdPos = _hmd.position;
                return speed;
            }

            return 0f;
        }
    }
}
```

- [ ] **Step 3: 플레이어 GameObject에 컴포넌트 추가**

  - 플레이어 루트 GameObject를 선택
  - Inspector에서 `SuperhotPlayerSoundEmitter` 추가
  - `SuperhotFlatFpsController`와 같은 오브젝트에 있으면 자동 연결됨

- [ ] **Step 4: 동작 확인 (에디터)**

  Play Mode 진입 → WASD 이동 → Console에 디버그 출력 확인용 임시 수신자를 붙여봄:
  ```csharp
  // 임시 테스트: 씬 내 빈 GO에 이 컴포넌트 부착
  void OnEnable() => SuperhotSoundChannel.OnSoundEmitted += e => Debug.Log($"[Sound] {e.Origin} r={e.Radius}");
  void OnDisable() => SuperhotSoundChannel.OnSoundEmitted -= e => Debug.Log($"[Sound] {e.Origin} r={e.Radius}");
  ```

- [ ] **Step 5: 커밋**

  ```bash
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotSoundChannel.cs
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotSoundChannel.cs.meta
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotPlayerSoundEmitter.cs
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotPlayerSoundEmitter.cs.meta
  git commit -m "feat(AI): 소리 이벤트 채널 및 플레이어 발소리 에미터 추가"
  ```

---

## Task 2: 근접 제압을 위한 플레이어 속도 변조

**Files:**
- Modify: `Project/Assets/_Project/Presentation/Gameplay/SuperhotFlatFpsController.cs:53-53` (필드 추가)
- Modify: `Project/Assets/_Project/Presentation/Gameplay/SuperhotFlatFpsController.cs:122-126` (Move 호출 수정)

- [ ] **Step 1: SpeedMultiplier 필드 추가**

  `SuperhotFlatFpsController.cs`에서 `float _pitch;` 선언 바로 아래에 추가:

  ```csharp
  float _pitch;
  Vector3 _velocity;
  IGameplayClock _clock;

  public float SpeedMultiplier { get; set; } = 1f;
  ```

- [ ] **Step 2: Move 호출에 SpeedMultiplier 반영**

  기존:
  ```csharp
  var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
  var move = transform.TransformDirection(input) * _moveSpeed;
  move.y = _velocity.y;
  _characterController.Move(move * dt);
  ```

  변경:
  ```csharp
  var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
  var move = transform.TransformDirection(input) * (_moveSpeed * SpeedMultiplier);
  move.y = _velocity.y;
  _characterController.Move(move * dt);
  ```

- [ ] **Step 3: 커밋**

  ```bash
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotFlatFpsController.cs
  git commit -m "feat(AI): FlatFpsController에 SpeedMultiplier 추가 (근접 제압용)"
  ```

---

## Task 3: 적 AI 상태 머신 (SuperhotEnemyBrain)

**Files:**
- Create: `Project/Assets/_Project/Presentation/Gameplay/SuperhotEnemyBrain.cs`

5개 상태:
- `Idle` — 감지 없음
- `Investigating` — 소리 감지 후 LOS 레이캐스트 판단
- `FlankToCorner` — LOS 없음 → 코너로 이동해 매복
- `Engaging` — LOS 있음 → 전술 사격 이동
- `CloseRange` — 근접 → 테이크다운

- [ ] **Step 1: SuperhotEnemyBrain.cs 생성**

```csharp
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
        // ── Inspector ──────────────────────────────────────────
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

        // ── State ──────────────────────────────────────────────
        enum EnemyState { Idle, Investigating, FlankToCorner, Engaging, CloseRange }
        EnemyState _state = EnemyState.Idle;

        // ── Refs ───────────────────────────────────────────────
        NavMeshAgent _agent;
        IGameplayClock _clock;
        SuperhotEnemyMover _legacyMover;
        SuperhotFlatFpsController _flatPlayer;
        Transform _playerTransform;
        Vector3 _lastSoundOrigin;

        // ── Unity ──────────────────────────────────────────────
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = false; // 수동으로 SimulationDeltaTime 기반 이동
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

            // NavMeshAgent 위치를 transform과 동기화 (updatePosition=false이므로 필수)
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

        // ── 소리 감지 ──────────────────────────────────────────
        void OnSoundHeard(SuperhotSoundEvent e)
        {
            if (_state == EnemyState.CloseRange)
                return;

            if (Vector3.Distance(transform.position, e.Origin) > _hearingRadius)
                return;

            _lastSoundOrigin = e.Origin;
            SetState(EnemyState.Investigating);
        }

        // ── 상태 틱 ───────────────────────────────────────────
        void Tick_Investigating()
        {
            if (_playerTransform == null)
                return;

            if (HasLOS())
            {
                SetState(EnemyState.Engaging);
                return;
            }

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
                SetState(EnemyState.Investigating);
                return;
            }

            // 전술 사격 이동: 플레이어 파이어포인트 X 반대 방향으로 이동
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
                ReleaseTakedown();
                SetState(EnemyState.Idle);
                return;
            }

            // 클린치 유지: 플레이어 위치로 계속 이동
            _agent.SetDestination(_playerTransform.position);
            MoveAlongPath(dt, _closeSpeed);
            FacePlayer();
        }

        // ── 이동 헬퍼 ─────────────────────────────────────────
        void MoveAlongPath(float dt, float speed)
        {
            if (_agent.pathPending || _agent.remainingDistance < 0.05f)
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

        // ── LOS 판단 ──────────────────────────────────────────
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

        // ── 코너 탐색 ─────────────────────────────────────────
        Vector3? FindFlankCorner()
        {
            if (_playerTransform == null)
                return null;

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

                // 이 위치에서 플레이어가 적을 볼 수 없어야 함 (코너 뒤에 숨기)
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

            return best;
        }

        // ── 전술 이동 방향 ────────────────────────────────────
        Vector3 ComputeStrafeDir()
        {
            // 파이어포인트 X(right) 반대 방향으로 이동
            // → 플레이어 조준 방향의 맹점으로 이동
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

        // ── 근접 제압 ─────────────────────────────────────────
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

            // VR: 진동 피드백 (향후 확장)
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

        // ── 상태 전환 ─────────────────────────────────────────
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

        // ── 플레이어 참조 갱신 ────────────────────────────────
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
            }
        }
    }
}
```

- [ ] **Step 2: 적 GameObject 씬 설정**

  1. 씬에서 적 GameObject 선택
  2. `NavMeshAgent` 컴포넌트 추가:
     - Speed: 3 (Brain이 런타임에 덮어씀)
     - Angular Speed: 360
     - Stopping Distance: 0.3
     - Auto Braking: true
  3. `SuperhotEnemyBrain` 컴포넌트 추가
  4. `SuperhotEnemyMover` 컴포넌트는 그대로 두어도 됨 — Brain이 자동으로 disable함
  5. `ObstacleMask`: Default + 벽 레이어 선택
  6. `PlayerFirePoint`: 플레이어 총의 FirePoint Transform 할당 (없으면 비워도 fallback 작동)

- [ ] **Step 3: NavMesh 베이크 확인**

  ```
  Window → AI → Navigation → Bake 탭 → Bake 버튼 클릭
  ```
  씬에 파란 NavMesh 오버레이가 표시되어야 함.

- [ ] **Step 4: 동작 테스트**

  Play Mode 진입 후 순서대로 확인:

  | 시나리오 | 기대 결과 |
  |---------|-----------|
  | 벽 뒤에서 WASD 이동 | 적이 코너로 이동 후 대기 |
  | 벽 없는 열린 공간에서 이동 | 적이 전술 사격 이동(사이드스텝) 시작 |
  | 플레이어와 1.5m 이내 접근 | 플레이어 이동 속도 25%로 감소 |
  | 근접 후 멀어지면 | 속도 정상화 |

- [ ] **Step 5: 커밋**

  ```bash
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotEnemyBrain.cs
  git add Project/Assets/_Project/Presentation/Gameplay/SuperhotEnemyBrain.cs.meta
  git commit -m "feat(AI): 적 전술 AI 상태 머신 추가 — 소리 감지/코너숨기/전술사격/근접제압"
  ```

---

## Task 4: 전체 통합 확인 및 정리

- [ ] **Step 1: 컴파일 오류 없음 확인**

  Unity Console에서 빨간 오류 없음 확인.

- [ ] **Step 2: 시뮬레이션 시간 스케일 반응 확인**

  SUPERHOT 슬로우모 상태 (거의 정지)에서:
  - 플레이어가 천천히 이동하면 → 적도 천천히 이동
  - 빠르게 이동하면 → 적도 빠르게 반응

  `MoveAlongPath`에서 `SimulationDeltaTime`을 사용하므로 자동 반영됨.

- [ ] **Step 3: 멀티 에너미 확인**

  적 여러 명을 씬에 배치 → 각자 독립적으로 상태 머신이 동작하는지 확인.
  (SuperhotSoundChannel이 정적 이벤트이므로 모든 적이 동시에 반응함 — 의도된 동작)

- [ ] **Step 4: 최종 커밋**

  ```bash
  git add -u
  git commit -m "feat(AI): 적 전술 AI 통합 — 소리 감지·코너 숨기·전술이동·근접제압"
  ```

---

## 알려진 한계 및 향후 확장

| 항목 | 현재 | 향후 |
|------|------|------|
| VR 클린치 | 진동만 | 손 고정 애니메이션 + IK |
| 코너 탐색 | 방사형 8방향 샘플링 | NavMesh edges 분석으로 진짜 코너 탐색 |
| 스트레이프 방향 | 파이어포인트 right 반전 | 플레이어 전방과 교차 계산으로 정교화 |
| 슬로우모 중 NavMesh | desiredVelocity 기반 근사 | NavMeshAgent.velocity 직접 설정 |
