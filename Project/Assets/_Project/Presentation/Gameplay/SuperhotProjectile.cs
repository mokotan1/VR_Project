using UnityEngine;
using Unity.XR.CoreUtils;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Moves in a fixed direction using <see cref="IGameplayClock.SimulationDeltaTime"/>.
    /// 수명도 동일한 시뮬레이션 시간으로 누적합니다(슬로모에서 비행 거리와 일치).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotProjectile : MonoBehaviour
    {
        [SerializeField] float _speed = 12f;

        [Tooltip("시뮬레이션 기준 초입니다(이동에 쓰는 SimulationDeltaTime과 동일한 시계).")]
        [SerializeField] float _lifetimeSeconds = 12f;

        Vector3 _direction = Vector3.forward;
        float _age;
        IGameplayClock _clock;

        void OnEnable()
        {
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

            _age += dt;
            if (_age >= _lifetimeSeconds)
                Destroy(gameObject);
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
