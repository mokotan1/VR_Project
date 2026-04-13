using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.OsFpsInspired
{
    /// <summary>다른 컴포넌트(Awake)보다 먼저 실행되어 <see cref="IsEquipped"/>가 브리지에서 안정적으로 읽힙니다.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class OsFpsInspiredWeapon : MonoBehaviour
    {
        [Header("연결 (보통 플레이어 프리팹에 이미 설정됨)")]
        [Tooltip("총을 쏠 때 십자선 방향을 잡는 카메라. 비우면 이 오브젝트 자식에서 자동으로 찾습니다.")]
        [SerializeField] Camera _camera;
        [Tooltip("손에 들린 총 모델(메시). 던지기 시 이 모양을 복제해 날립니다.")]
        [SerializeField] GameObject _handGunVisual;
        [Tooltip("총구 위치. 비우면 총 모델 안에서 이름으로 찾거나, 메시 크기로 대략 잡습니다.")]
        [SerializeField] Transform _muzzleTransform;

        [Header("시작 상태")]
        [Tooltip("켜면 게임 시작부터 총을 들고 탄창이 가득 찬 상태입니다. 끄면 바닥 픽업 등으로 줍기 전엔 발사할 수 없습니다.")]
        [SerializeField] bool _startEquipped = false;

        [Header("발사 · 탄약 (기획에서 자주 조정)")]
        [Tooltip("탄창에 들어 있는 총알 수.")]
        [Range(1, 200)]
        [SerializeField] int _magSize = 24;
        [Tooltip("한 발 쏜 뒤 다음 발까지 기다리는 시간(초). 작을수록 연사가 빠릅니다.")]
        [Range(0.02f, 1f)]
        [SerializeField] float _fireCooldown = 0.12f;
        [Tooltip("R 키로 재장전할 때 걸리는 시간(초).")]
        [Range(0.2f, 5f)]
        [SerializeField] float _reloadDuration = 1.35f;
        [Tooltip("한 발이 적에게 줄 피해량.")]
        [Range(1f, 200f)]
        [SerializeField] float _damage = 28f;
        [Tooltip("총알(레이)이 맞출 수 있는 최대 거리. 멀리 있는 적도 맞추려면 키웁니다.")]
        [Range(5f, 250f)]
        [SerializeField] float _maxDistance = 120f;

        [Header("조준 · 발사 조건")]
        [Tooltip("켜면 오른쪽 마우스로 조준할 때만 발사·던지기가 됩니다. (이동 중 힙 파이어 옵션과 함께 씁니다.)")]
        [SerializeField] bool _requireRightMouseAimToFire = true;
        [Tooltip("위 조준이 필수일 때, WASD로 움직이는 동안에는 조준 없이도 발사할 수 있게 합니다.")]
        [SerializeField] bool _allowHipFireWhileLocomoting = true;
        [Tooltip("얼마나 움직여야 ‘이동 중’으로 볼지(미세 값). 보통 그대로 두면 됩니다.")]
        [Range(0f, 0.01f)]
        [SerializeField] float _locomotionHipFireAxesSqrThreshold = 1e-5f;

        [Header("총알 이펙트 (보이는 모양만)")]
        [Tooltip("탄환 궤적에 쓸 프리팹. 비우면 간단한 큐브 트레이서를 만듭니다.")]
        [SerializeField] GameObject _bulletVisualPrefab;
        [Tooltip("탄환 모델이 너무 작게 보이면 크기를 키웁니다.")]
        [Range(0.1f, 200f)]
        [SerializeField] float _bulletVisualScale = 56f;
        [Tooltip("이펙트가 날아가는 속도(보여 주기용). 실제 맞춤 판정은 즉시 레이캐스트입니다.")]
        [Range(5f, 200f)]
        [SerializeField] float _bulletSpeed = 95f;
        [Tooltip("총구 위치를 자동으로 잡을 때, 카메라 앞쪽으로 얼마나 밀어서 쏠지.")]
        [Range(0.05f, 3f)]
        [SerializeField] float _bulletMuzzleForwardOffset = 0.45f;
        [Tooltip("탄환 프리팹이 눕혀 보이면 각도를 조절합니다. (고급)")]
        [SerializeField] Vector3 _bulletVisualEulerOffset = new Vector3(90f, 0f, 0f);

        [Header("맞춤 판정 (고급)")]
        [Tooltip("레이가 맞출 레이어. 비어 있지 않게 두는 것이 일반적입니다.")]
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [Tooltip("눈 위치가 몸 콜라이더 안에 있을 때, 레이 시작점을 앞으로 살짝 밀어 자기 몸에 안 막히게 합니다.")]
        [Range(0f, 1f)]
        [SerializeField] float _hitscanRayStartForwardOffset = 0.12f;
        [Tooltip("‘자기 몸’으로 칠 트랜스폼. 비우면 캐릭터 컨트롤러 기준으로 자동 설정됩니다.")]
        [SerializeField] Transform _hitscanExclusionRootOverride;

        [Header("탄창이 비었을 때 — 총 던지기")]
        [Tooltip("얼마나 세게 던지는지. 크면 멀리·빨리 날아가 총알처럼 느껴질 수 있고, 작으면 바로 앞 적에게 툭 던지는 느낌에 가깝습니다.")]
        [Range(0.5f, 30f)]
        [SerializeField] float _throwImpulse = 4.2f;
        [Tooltip("위로 조금 더 실어서 포물선처럼 던집니다. 0에 가깝게 하면 더 직선에 가깝습니다.")]
        [Range(0f, 1f)]
        [SerializeField] float _throwUpwardBias = 0.22f;
        [Tooltip("총이 빙글 도는 정도. 0이면 거의 안 돕니다.")]
        [Range(0f, 10f)]
        [SerializeField] float _throwTorque = 1.1f;
        [Tooltip("던진 총이 적에게 부딪혔을 때 줄 피해량.")]
        [Range(0f, 200f)]
        [SerializeField] float _thrownGunDamage = 22f;
        [Tooltip("던진 총이 몇 초 뒤에 사라질지. 0이면 시간으로 지우지 않습니다.")]
        [Range(0f, 60f)]
        [SerializeField] float _thrownGunLifetimeSeconds = 12f;
        [Tooltip("던진 총의 무게. 무거울수록 같은 힘으로는 덜 빨리 날아갑니다.")]
        [Range(0.1f, 10f)]
        [SerializeField] float _thrownRigidbodyMass = 1.65f;
        [Tooltip("공기 저항. 클수록 금방 느려져 가까운 거리 투척 느낌에 좋습니다.")]
        [Range(0f, 5f)]
        [SerializeField] float _thrownLinearDrag = 1.15f;
        [Tooltip("회전이 멈추는 빠르기. 클수록 덜 빙글돕니다.")]
        [Range(0f, 5f)]
        [SerializeField] float _thrownAngularDrag = 0.85f;

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
            if (!fire)
                return;

            if (OsFpsInspiredWeaponThrowGate.ShouldThrowOnFire(_ammoInMag, IsReloading))
            {
                TryThrowWeapon(cam);
                return;
            }

            if (Time.unscaledTime < _nextFire || _ammoInMag <= 0)
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

        void TryThrowWeapon(Camera cam)
        {
            if (_handGunVisual == null || cam == null)
                return;

            var tr = _handGunVisual.transform;
            var thrown = Instantiate(_handGunVisual, tr.position, tr.rotation);
            thrown.name = "ThrownGun";
            thrown.SetActive(true);

            foreach (var anim in thrown.GetComponentsInChildren<Animator>(true))
            {
                if (anim != null)
                    anim.enabled = false;
            }

            EnsureThrownGunColliders(thrown);
            var rb = thrown.GetComponent<Rigidbody>();
            if (rb == null)
                rb = thrown.AddComponent<Rigidbody>();
            rb.mass = _thrownRigidbodyMass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearDamping = Mathf.Max(0f, _thrownLinearDrag);
            rb.angularDamping = Mathf.Max(0f, _thrownAngularDrag);

            var aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var dir = aimRay.direction.normalized;
            var throwDir = (dir + Vector3.up * _throwUpwardBias).normalized;
            rb.AddForce(throwDir * _throwImpulse, ForceMode.Impulse);
            if (_throwTorque > 0f)
                rb.AddTorque(Random.onUnitSphere * _throwTorque, ForceMode.Impulse);

            var thrownDmg = thrown.GetComponent<OsFpsInspiredThrownGunDamage>();
            if (thrownDmg == null)
                thrownDmg = thrown.AddComponent<OsFpsInspiredThrownGunDamage>();
            thrownDmg.Configure(
                _thrownGunDamage,
                _hitscanExclusionRoot,
                _thrownGunLifetimeSeconds,
                oneHitOnly: true);

            SetEquipped(false);
        }

        static void EnsureThrownGunColliders(GameObject root)
        {
            if (root == null)
                return;
            if (root.GetComponentsInChildren<Collider>(true).Length > 0)
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                var fallback = root.GetComponent<BoxCollider>();
                if (fallback == null)
                    fallback = root.AddComponent<BoxCollider>();
                fallback.size = new Vector3(0.08f, 0.12f, 0.45f);
                fallback.center = new Vector3(0f, 0f, 0.18f);
                return;
            }

            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    b.Encapsulate(renderers[i].bounds);
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();
            var t = root.transform;
            box.center = t.InverseTransformPoint(b.center);
            var sx = Mathf.Max(Mathf.Abs(t.lossyScale.x), 1e-4f);
            var sy = Mathf.Max(Mathf.Abs(t.lossyScale.y), 1e-4f);
            var sz = Mathf.Max(Mathf.Abs(t.lossyScale.z), 1e-4f);
            box.size = new Vector3(b.size.x / sx, b.size.y / sy, b.size.z / sz);
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
