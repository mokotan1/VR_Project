using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Simple drift toward the player's HMD using simulation time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemyMover : MonoBehaviour
    {
        [SerializeField] Transform _hmd;

        [SerializeField] float _moveSpeed = 1.5f;

        Unity.XR.CoreUtils.XROrigin _origin;
        IGameplayClock _clock;

        void Awake()
        {
            _origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (_hmd == null && _origin != null && _origin.Camera != null)
                _hmd = _origin.Camera.transform;
        }

        void OnEnable()
        {
            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        void Update()
        {
            if (_hmd == null)
                return;

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;

            var flat = _hmd.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f)
                return;

            transform.position += flat.normalized * (_moveSpeed * dt);
        }
    }
}
