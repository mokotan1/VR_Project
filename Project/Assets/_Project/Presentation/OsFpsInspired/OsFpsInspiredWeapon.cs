using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.OsFpsInspired
{
    public sealed class OsFpsInspiredWeapon : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        /// <summary>When false, use a world pickup (e.g. HK416 on the ground) before firing.</summary>
        [SerializeField] bool _startEquipped = false;
        [SerializeField] GameObject _handGunVisual;
        [Tooltip("Barrel tip / muzzle. If unset, searches common names under the hand gun or creates a proxy from mesh bounds.")]
        [SerializeField] Transform _muzzleTransform;
        [SerializeField] GameObject _bulletVisualPrefab;
        [Tooltip("Bullets Pack 메시는 매우 작음; 너무 작으면 트레일만 보일 수 있음.")]
        [SerializeField] float _bulletVisualScale = 56f;
        [SerializeField] float _bulletSpeed = 95f;
        [Tooltip("Fallback when no muzzle transform (camera-forward offset).")]
        [SerializeField] float _bulletMuzzleForwardOffset = 0.45f;
        [Tooltip("DuNguyn 탄환 메시 등은 길이축이 +Z가 아닐 때 많음. 기본은 Y축 총알을 비행 방향(+Z)에 맞춤.")]
        [SerializeField] Vector3 _bulletVisualEulerOffset = new Vector3(90f, 0f, 0f);
        [SerializeField] bool _requireRightMouseAimToFire = true;
        [Tooltip("조준(RMB) 필수일 때, 이동 입력(로코모션 축)이 있으면 힙 파이어 허용. WASD 사용 시 RMB 없이도 발사 가능.")]
        [SerializeField] bool _allowHipFireWhileLocomoting = true;
        [Tooltip("로코모션 축 크기 제곱이 이 값보다 크면 ‘이동 중’으로 간주합니다.")]
        [SerializeField] float _locomotionHipFireAxesSqrThreshold = 1e-5f;
        [SerializeField] float _maxDistance = 120f;
        [SerializeField] int _magSize = 24;
        [SerializeField] float _fireCooldown = 0.12f;
        [SerializeField] float _reloadDuration = 1.35f;
        [SerializeField] float _damage = 28f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [Tooltip("시야 레이가 머리·캡슐 내부에서 시작할 때 자기 몸 콜라이더에 막히지 않도록 조준 방향으로 레이 원점을 밀어 냅니다.")]
        [SerializeField] float _hitscanRayStartForwardOffset = 0.12f;
        [Tooltip("비우면 CharacterController가 붙은 트랜스폼(없으면 이 오브젝트) 아래 콜라이더를 히트스캔에서 무시합니다.")]
        [SerializeField] Transform _hitscanExclusionRootOverride;

        int _ammoInMag;
        float _nextFire;
        float _reloadEnds;
        bool _equipped;
        IUnityChanLocomotionMotor _motor;
        float _lastFireUnscaledTime = -999f;
        Transform _runtimeMuzzleProxy;
        Transform _hitscanExclusionRoot;

        public int AmmoInMag => _ammoInMag;
        public int MagSize => _magSize;
        public bool IsReloading => Time.unscaledTime < _reloadEnds;
        public bool IsEquipped => _equipped;
        /// <summary>For HUD crosshair spread kick (hip-fire feedback).</summary>
        public float LastFireUnscaledTime => _lastFireUnscaledTime;

        void Awake()
        {
            if (_camera == null)
                _camera = GetComponentInChildren<Camera>();
            _motor = UnityChanLocomotionMotorResolver.ResolveOn(gameObject);
            _hitscanExclusionRoot = _hitscanExclusionRootOverride != null
                ? _hitscanExclusionRootOverride
                : OsFpsInspiredHitscanExclusion.ResolveExclusionRoot(this);
            _equipped = _startEquipped;
            if (_handGunVisual != null)
                _handGunVisual.SetActive(_equipped);
            _ammoInMag = _equipped ? _magSize : 0;
        }

        public void SetEquipped(bool equipped)
        {
            _equipped = equipped;
            if (_handGunVisual != null)
            {
                _handGunVisual.SetActive(equipped);
                if (equipped)
                {
                    foreach (var r in _handGunVisual.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != null)
                            r.enabled = true;
                    }
                }
            }

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

        Transform ResolveMuzzleTransform()
        {
            if (_muzzleTransform != null)
                return _muzzleTransform;
            if (_handGunVisual == null || !_handGunVisual.activeInHierarchy)
                return null;
            if (_runtimeMuzzleProxy != null)
                return _runtimeMuzzleProxy;

            var t = _handGunVisual.transform.Find("WeaponFirePoint");
            if (t != null)
            {
                _runtimeMuzzleProxy = t;
                return t;
            }

            var found = FindMuzzleByName(_handGunVisual.transform);
            if (found != null)
            {
                _runtimeMuzzleProxy = found;
                return found;
            }

            _runtimeMuzzleProxy = CreateBoundsMuzzleProxy(_handGunVisual.transform);
            return _runtimeMuzzleProxy;
        }

        static Transform FindMuzzleByName(Transform root)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                var n = tr.name.ToLowerInvariant();
                if (n.Contains("muzzle") || n.Contains("firepoint") || n.Contains("fire_point") ||
                    n.Contains("barrel_tip") || n.Contains("tip") && n.Contains("barrel"))
                    return tr;
            }

            return null;
        }

        static Transform CreateBoundsMuzzleProxy(Transform gunRoot)
        {
            var renderers = gunRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                var empty = new GameObject("WeaponFirePoint").transform;
                empty.SetParent(gunRoot, false);
                empty.localPosition = new Vector3(0f, 0f, 0.35f);
                empty.localRotation = Quaternion.identity;
                return empty;
            }

            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            var forward = gunRoot.forward;
            var corners = new Vector3[8];
            var c = b.center;
            var e = b.extents;
            var idx = 0;
            for (var x = -1f; x <= 1f; x += 2f)
            for (var y = -1f; y <= 1f; y += 2f)
            for (var z = -1f; z <= 1f; z += 2f)
                corners[idx++] = c + new Vector3(e.x * x, e.y * y, e.z * z);

            var best = corners[0];
            var bestDot = float.MinValue;
            foreach (var p in corners)
            {
                var d = Vector3.Dot(p - b.center, forward);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = p;
                }
            }

            var go = new GameObject("WeaponFirePoint");
            go.transform.position = best;
            go.transform.rotation = Quaternion.LookRotation(forward, gunRoot.up);
            go.transform.SetParent(gunRoot, true);
            return go.transform;
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

            if (!OsFpsInspiredAimFireGate.PassesFireAimGate(
                    _requireRightMouseAimToFire,
                    _allowHipFireWhileLocomoting,
                    _locomotionHipFireAxesSqrThreshold,
                    _motor == null,
                    _motor != null && _motor.IsAiming,
                    _motor != null ? _motor.LocomotionAxes : Vector2.zero))
                return;

            var fire = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!fire || Time.unscaledTime < _nextFire || _ammoInMag <= 0)
                return;

            _nextFire = Time.unscaledTime + _fireCooldown;
            _ammoInMag--;
            _lastFireUnscaledTime = Time.unscaledTime;

            // 총구 forward는 손 본 애니 때문에 아래/옆으로 틀어질 수 있음 → 조준은 항상 카메라 십자(뷰포트 중앙) 방향.
            var aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var aimDir = aimRay.direction.normalized;

            var muzzleTr = ResolveMuzzleTransform();
            var spawnPos = muzzleTr != null
                ? muzzleTr.position
                : aimRay.origin + aimDir * Mathf.Clamp(_bulletMuzzleForwardOffset, 0.05f, 3f);

            SpawnBulletVisual(spawnPos, aimDir);

            if (!TryFirstWorldHitExcludingSelf(aimRay, out var hit))
                return;

            var dmg = hit.collider.GetComponentInParent<OsFpsInspiredDamageable>();
            if (dmg != null)
                dmg.ApplyDamage(_damage, hit.point);
        }

        bool IsPartOfShooterRig(Collider c) =>
            OsFpsInspiredHitscanExclusion.IsColliderUnderExclusionRoot(c, _hitscanExclusionRoot);

        bool TryFirstWorldHitExcludingSelf(Ray viewportAimRay, out RaycastHit bestHit)
        {
            var ray = OsFpsInspiredHitscanExclusion.BuildBiasedAimRay(
                viewportAimRay, _hitscanRayStartForwardOffset, _maxDistance, out var castDistance);
            var hits = Physics.RaycastAll(ray, castDistance, _hitMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                bestHit = default;
                return false;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (IsPartOfShooterRig(h.collider))
                    continue;
                bestHit = h;
                return true;
            }

            bestHit = default;
            return false;
        }

        void SpawnBulletVisual(Vector3 position, Vector3 direction)
        {
            GameObject go;
            Vector3 eulerForLaunch;

            if (_bulletVisualPrefab != null)
            {
                go = Instantiate(_bulletVisualPrefab, position, Quaternion.identity);
                go.transform.localScale = Vector3.one * _bulletVisualScale;
                foreach (var col in go.GetComponentsInChildren<Collider>())
                    Destroy(col);
                eulerForLaunch = _bulletVisualEulerOffset;
            }
            else
            {
                go = new GameObject("BulletTracer_Fallback");
                go.transform.position = position;
                var meshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshGo.name = "TracerMesh";
                meshGo.transform.SetParent(go.transform, false);
                meshGo.transform.localPosition = new Vector3(0f, 0f, 0.22f);
                meshGo.transform.localScale = new Vector3(0.06f, 0.06f, 0.5f);
                Destroy(meshGo.GetComponent<Collider>());
                ApplyTracerMaterial(meshGo.GetComponent<MeshRenderer>());
                eulerForLaunch = Vector3.zero;
            }

            var proj = go.GetComponent<PrototypeFpsBulletProjectile>();
            if (proj == null)
                proj = go.AddComponent<PrototypeFpsBulletProjectile>();
            proj.Launch(direction, _bulletSpeed, _maxDistance, eulerForLaunch);
            AddBulletTrail(go);
        }

        static void ApplyTracerMaterial(MeshRenderer renderer)
        {
            if (renderer == null)
                return;
            var sh = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
            if (sh == null)
                return;
            var m = new Material(sh);
            var c = new Color(1f, 0.88f, 0.2f);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.1f) * 3f);
                m.EnableKeyword("_EMISSION");
            }

            renderer.material = m;
        }

        static void AddBulletTrail(GameObject root)
        {
            var trail = root.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = root.AddComponent<TrailRenderer>();
            trail.emitting = true;
            trail.time = 0.2f;
            trail.minVertexDistance = 0.008f;
            trail.startWidth = 0.09f;
            trail.endWidth = 0.03f;
            trail.numCapVertices = 4;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
            if (sh == null)
                return;
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(1f, 0.95f, 0.4f, 1f));
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", new Color(1f, 0.95f, 0.4f, 1f));
            trail.material = m;
        }
    }
}
