using UnityEngine;
using UnityEngine.XR;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Periodically spawns projectiles toward the HMD. Interval uses simulation time when clock is available.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemyShooter : MonoBehaviour
    {
        [SerializeField] SuperhotProjectile _projectilePrefab;

        [SerializeField] Transform _muzzle;

        [SerializeField] Transform _hmd;

        [SerializeField] float _cooldownSeconds = 2.5f;

        [Tooltip("켜면 쿨다운이 시뮬레이션 시간이 아니라 실시간(슬로모와 무관)으로 누적됩니다.")]
        [SerializeField] bool _cooldownUsesRealTime;

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

        void Update()
        {
            RefreshPlayerTarget();

            if (_projectilePrefab == null || _hmd == null)
                return;

            float dt;
            if (_cooldownUsesRealTime)
                dt = Time.unscaledDeltaTime;
            else
                dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;

            _accumulator += dt;
            if (_accumulator < _cooldownSeconds)
                return;

            _accumulator = 0f;
            var origin = _muzzle.position;
            var to = _hmd.position - origin;
            if (to.sqrMagnitude < 1e-4f)
                return;

            var proj = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(to.normalized));
            proj.Launch(to.normalized);
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
