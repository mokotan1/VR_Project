using UnityEngine;
using Unity.XR.CoreUtils;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Moves in a fixed direction using <see cref="IGameplayClock.SimulationDeltaTime"/>.
    /// 수명 옵션을 켠 경우 시뮬레이션 시간으로 누적합니다. 끄면 플레이어/카메라 등 기존 트리거에만 반응하며 벽 처리는 씬 콜라이더·태그 확장으로 추가할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotProjectile : MonoBehaviour
    {
        [SerializeField] float _speed = 12f;

        [Tooltip("켜면 시뮬 시간 _lifetimeSeconds 후 파괴. 끄면 충돌(트리거)로만 제거 — 원작에 가깝습니다.")]
        [SerializeField] bool _useLifetimeLimit;

        [Tooltip("시뮬레이션 기준 초(_useLifetimeLimit일 때만).")]
        [SerializeField] float _lifetimeSeconds = 12f;

        Vector3 _direction = Vector3.forward;
        float _age;
        IGameplayClock _clock;

        void OnEnable()
        {
            _age = 0f;
            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        public void Launch(Vector3 worldDirection)
        {
            _direction = worldDirection.sqrMagnitude > 1e-6f ? worldDirection.normalized : Vector3.forward;
            transform.forward = _direction;
        }

        void Update()
        {
            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            transform.position += _direction * (_speed * dt);

            if (_useLifetimeLimit)
            {
                _age += dt;
                if (_age >= _lifetimeSeconds)
                    Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<SuperhotPlaytestPlayerHealth>();
            if (health != null && health.IsAlive)
            {
                health.ApplyHit();
                Destroy(gameObject);
                return;
            }

            if (other.GetComponentInParent<XROrigin>() != null || other.CompareTag("MainCamera"))
                Destroy(gameObject);
        }
    }
}
