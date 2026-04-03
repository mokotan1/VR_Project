using UnityEngine;
using UnityEngine.InputSystem;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.OsFpsInspired
{
    public sealed class OsFpsInspiredWeapon : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        /// <summary>When false, use a world pickup (e.g. HK416 on the ground) before firing.</summary>
        [SerializeField] bool _startEquipped = false;
        [SerializeField] GameObject _handGunVisual;
        [SerializeField] GameObject _bulletVisualPrefab;
        [SerializeField] float _bulletVisualScale = 10f;
        [SerializeField] float _bulletSpeed = 95f;
        [SerializeField] float _bulletMuzzleForwardOffset = 0.45f;
        [SerializeField] Vector3 _bulletVisualEulerOffset = new Vector3(90f, 0f, 0f);
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
        bool _equipped;
        PrototypeThirdPersonPlayer _motor;

        public int AmmoInMag => _ammoInMag;
        public int MagSize => _magSize;
        public bool IsReloading => Time.unscaledTime < _reloadEnds;
        public bool IsEquipped => _equipped;

        void Awake()
        {
            if (_camera == null)
                _camera = GetComponentInChildren<Camera>();
            _motor = GetComponent<PrototypeThirdPersonPlayer>();
            _equipped = _startEquipped;
            if (_handGunVisual != null)
                _handGunVisual.SetActive(_equipped);
            _ammoInMag = _equipped ? _magSize : 0;
        }

        public void SetEquipped(bool equipped)
        {
            _equipped = equipped;
            if (_handGunVisual != null)
                _handGunVisual.SetActive(equipped);
            if (equipped)
            {
                _ammoInMag = _magSize;
                _reloadEnds = 0f;
            }
            else
            {
                _ammoInMag = 0;
                _reloadEnds = 0f;
            }
        }

        void Update()
        {
            if (!_equipped)
                return;

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

            var muzzle = cam.transform.position + cam.transform.forward * _bulletMuzzleForwardOffset;
            var aimDir = cam.transform.forward;
            SpawnBulletVisual(muzzle, aimDir);

            var ray = new Ray(cam.transform.position, aimDir);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _hitMask, QueryTriggerInteraction.Ignore))
                return;

            var dmg = hit.collider.GetComponentInParent<OsFpsInspiredDamageable>();
            if (dmg != null)
                dmg.ApplyDamage(_damage, hit.point);
        }

        void SpawnBulletVisual(Vector3 position, Vector3 direction)
        {
            if (_bulletVisualPrefab == null)
                return;

            var go = Instantiate(_bulletVisualPrefab, position, Quaternion.identity);
            go.transform.localScale = Vector3.one * _bulletVisualScale;
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Destroy(col);

            var proj = go.GetComponent<PrototypeFpsBulletProjectile>();
            if (proj == null)
                proj = go.AddComponent<PrototypeFpsBulletProjectile>();
            proj.Launch(direction, _bulletSpeed, _maxDistance, _bulletVisualEulerOffset);
        }
    }
}
