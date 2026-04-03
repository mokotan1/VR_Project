using UnityEngine;
using UnityEngine.InputSystem;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.OsFpsInspired
{
    public sealed class OsFpsInspiredWeapon : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] bool _requireRightMouseAimToFire = true;
        [SerializeField] float _maxDistance = 120f;
        [SerializeField] int _magSize = 24;
        [SerializeField] float _fireCooldown = 0.12f;
        [SerializeField] float _reloadDuration = 1.35f;
        [SerializeField] float _damage = 28f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;

        int _ammoInMag;
        float _nextFire;
        float _reloadEnds;
        PrototypeThirdPersonPlayer _motor;

        public int AmmoInMag => _ammoInMag;
        public int MagSize => _magSize;
        public bool IsReloading => Time.unscaledTime < _reloadEnds;

        void Awake()
        {
            if (_camera == null)
                _camera = GetComponentInChildren<Camera>();
            _motor = GetComponent<PrototypeThirdPersonPlayer>();
            _ammoInMag = _magSize;
        }

        void Update()
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return;

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame && !IsReloading && _ammoInMag < _magSize)
                _reloadEnds = Time.unscaledTime + _reloadDuration;

            if (IsReloading && Time.unscaledTime >= _reloadEnds)
            {
                _ammoInMag = _magSize;
                _reloadEnds = 0f;
            }

            if (IsReloading)
                return;

            if (_requireRightMouseAimToFire && _motor != null && !_motor.IsAiming)
                return;

            var fire = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!fire || Time.unscaledTime < _nextFire || _ammoInMag <= 0)
                return;

            _nextFire = Time.unscaledTime + _fireCooldown;
            _ammoInMag--;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _hitMask, QueryTriggerInteraction.Ignore))
                return;

            var dmg = hit.collider.GetComponentInParent<OsFpsInspiredDamageable>();
            if (dmg != null)
                dmg.ApplyDamage(_damage, hit.point);
        }
    }
}
