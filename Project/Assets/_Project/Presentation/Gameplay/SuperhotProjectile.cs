using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Moves in a fixed direction using <see cref="IGameplayClock.SimulationDeltaTime"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotProjectile : MonoBehaviour
    {
        [SerializeField] float _speed = 12f;

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

            _age += Time.unscaledDeltaTime;
            if (_age >= _lifetimeSeconds)
                Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null || other.CompareTag("MainCamera"))
                Destroy(gameObject);
        }
    }
}
