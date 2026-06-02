using UnityEngine;
using UnityEngine.XR;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Periodically spawns projectiles toward the HMD. Cadence follows <see cref="IGameplayClock.SimulationDeltaTime"/>
    /// so fire rate and in-flight bullets respect SUPERHOT time smoothing when the player is still.
    /// </summary>
    [DefaultExecutionOrder(0)]
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemyShooter : MonoBehaviour
    {
        [SerializeField] SuperhotProjectile _projectilePrefab;

        [SerializeField] Transform _muzzle;

        [SerializeField] Transform _hmd;

        [SerializeField] float _cooldownSeconds = 2.5f;

        [SerializeField] float _minEngagementDistance = 10f;

        bool _rangedEngagementActive;
        float _accumulator;
        Unity.XR.CoreUtils.XROrigin _origin;
        IGameplayClock _clock;

        void Awake()
        {
            _origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (_hmd == null && _origin != null && _origin.Camera != null)
                _hmd = _origin.Camera.transform;
            if (_muzzle == null)
                _muzzle = transform;
        }

        void OnEnable()
        {
            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        public void SetRangedEngagementActive(bool active) => _rangedEngagementActive = active;

        void Update()
        {
            if (!_rangedEngagementActive)
                return;

            RefreshPlayerTarget();

            if (_projectilePrefab == null || _hmd == null)
                return;

            var to = _hmd.position - (_muzzle != null ? _muzzle.position : transform.position);
            if (to.sqrMagnitude < _minEngagementDistance * _minEngagementDistance)
                return;

            var dt = ResolveSimulationDeltaTime();
            _accumulator += dt;
            if (_accumulator < _cooldownSeconds)
                return;

            _accumulator = 0f;
            var origin = _muzzle != null ? _muzzle.position : transform.position;
            if (to.sqrMagnitude < 1e-4f)
                return;

            var proj = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(to.normalized));
            proj.Launch(to.normalized);
        }

        float ResolveSimulationDeltaTime()
        {
            if (_clock == null)
            {
                var locator = ServiceLocator.Instance;
                if (locator.IsRegistered<IGameplayClock>())
                    _clock = locator.Resolve<IGameplayClock>();
            }

            return _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
        }

        void RefreshPlayerTarget()
        {
            if (XRSettings.isDeviceActive)
            {
                if (_origin == null || !_origin.gameObject.activeInHierarchy)
                    _origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();

                if (_origin != null && _origin.Camera != null)
                    _hmd = _origin.Camera.transform;
            }
            else
            {
                var main = Camera.main;
                if (main != null)
                    _hmd = main.transform;
            }
        }
    }
}
