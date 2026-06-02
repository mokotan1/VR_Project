using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// HK416 hitscan while the right controller grip is held; fires on trigger press (not scene snap).
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class VrHk416TriggerFire : MonoBehaviour
    {
        [SerializeField] XROrigin _xrOrigin;
        [SerializeField] VrSceneWeaponSnapInput _snapInput;
        [SerializeField] XRNode _fireHand = XRNode.RightHand;
        [SerializeField, Range(0.01f, 1f)] float _analogTriggerThreshold = 0.55f;
        [SerializeField] float _maxDistance = 80f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] float _fireCooldownSeconds = 0.22f;
        [SerializeField] float _fullTimeScaleHoldSeconds = 0.15f;
        [SerializeField] GameObject _bulletVisualPrefab;
        [SerializeField] float _bulletVisualScale = 1f;
        [SerializeField] float _bulletSpeed = 95f;
        [SerializeField] Vector3 _bulletVisualEulerOffset = new(90f, 0f, 0f);

        VrTriggerPressDetector _triggerEdgeDetector;
        float _nextFireUnscaledTime;
        float _lastShootUnscaledTime = -1e9f;

        public bool IsInShootTimeScaleHold =>
            Time.unscaledTime - _lastShootUnscaledTime < Mathf.Max(0f, _fullTimeScaleHoldSeconds);

        void OnDestroy()
        {
            PlayerWeaponFirePointForAi.ClearIfOwner(this);
        }

        void Awake()
        {
            if (_xrOrigin == null)
                _xrOrigin = GetComponent<XROrigin>();
            if (_snapInput == null)
                _snapInput = GetComponent<VrSceneWeaponSnapInput>();
            VrHk416FireVisualDefaults.ApplyTo(this);
        }

        public void ApplySharedDefaults(GameObject bulletVisualPrefab)
        {
            if (bulletVisualPrefab != null && _bulletVisualPrefab == null)
                _bulletVisualPrefab = bulletVisualPrefab;

            _bulletVisualScale = UnityChanPrototypeWeaponMuzzleDefaults.BulletVisualScale;
            if (_bulletSpeed <= 0f)
                _bulletSpeed = UnityChanPrototypeWeaponMuzzleDefaults.BulletSpeed;
            _bulletVisualEulerOffset = UnityChanPrototypeWeaponMuzzleDefaults.BulletVisualEulerOffset;
        }

        void Update()
        {
            if (!VrPlaytestControllerInput.TryReadGripHeld(_fireHand, _analogTriggerThreshold, out var gripHeld) ||
                !gripHeld)
                return;

            if (!VrPlaytestControllerInput.TryReadTriggerEdge(
                    _fireHand,
                    _analogTriggerThreshold,
                    ref _triggerEdgeDetector,
                    out var triggerEdge) ||
                !triggerEdge)
                return;

            if (!SuperhotVrHk416FireLogic.ShouldFireThisFrame(gripHeld, triggerEdge))
                return;

            TryFire();
        }

        public static bool ShouldFireThisFrame(bool gripHeld, bool triggerPressedThisFrame) =>
            SuperhotVrHk416FireLogic.ShouldFireThisFrame(gripHeld, triggerPressedThisFrame);

        public bool TryFire()
        {
            if (Time.unscaledTime < _nextFireUnscaledTime)
                return false;

            if (_xrOrigin == null || _xrOrigin.Camera == null)
                return false;

            var anchor = _snapInput != null
                ? _snapInput.ResolveRightHandAnchor()
                : ResolveFallbackHandAnchor(_xrOrigin);
            if (anchor == null)
                return false;
            if (!SuperhotVrHk416FireLogic.TryGetHk416OnHandAnchor(anchor, out var weaponRoot))
                return false;

            var cam = _xrOrigin.Camera;
            var aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var gunVisual = SuperhotVrHk416FireLogic.ResolveGunVisual(weaponRoot);
            if (gunVisual == null)
                return false;

            UnityChanPrototypeWeaponMuzzleDefaults.TryGetMuzzleWorldPose(
                gunVisual,
                out var spawnPos,
                out _);
            var aimDir = UnityChanPrototypeWeaponMuzzleDefaults.ComputeAimDirectionFromViewport(
                cam,
                spawnPos,
                _maxDistance);
            var muzzleTransform = UnityChanPrototypeWeaponMuzzleDefaults.EnsureFirePoint(gunVisual);

            SpawnBulletVisual(spawnPos, aimDir);
            PlayerWeaponFirePointForAi.Publish(this, muzzleTransform);
            SuperhotVrHk416FireLogic.TryRaycastKillEnemy(
                aimRay,
                _maxDistance,
                _hitMask,
                _xrOrigin.transform,
                out _);

            _nextFireUnscaledTime = Time.unscaledTime + Mathf.Max(0.05f, _fireCooldownSeconds);
            _lastShootUnscaledTime = Time.unscaledTime;
            return true;
        }

        static Transform ResolveFallbackHandAnchor(XROrigin xrOrigin)
        {
            if (xrOrigin == null)
                return null;

            foreach (var child in xrOrigin.transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Right Controller")
                    return child;
            }

            return xrOrigin.Camera != null ? xrOrigin.Camera.transform : xrOrigin.transform;
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
                go = new GameObject("Hk416BulletTracer");
                go.transform.position = position;
                var meshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshGo.name = "TracerMesh";
                meshGo.transform.SetParent(go.transform, false);
                meshGo.transform.localPosition = new Vector3(0f, 0f, 0.22f);
                meshGo.transform.localScale = new Vector3(0.06f, 0.06f, 0.5f);
                Object.Destroy(meshGo.GetComponent<Collider>());
                eulerForLaunch = Vector3.zero;
            }

            var proj = go.GetComponent<PrototypeFpsBulletProjectile>();
            if (proj == null)
                proj = go.AddComponent<PrototypeFpsBulletProjectile>();
            proj.Launch(direction, _bulletSpeed, _maxDistance, eulerForLaunch);
        }
    }
}
