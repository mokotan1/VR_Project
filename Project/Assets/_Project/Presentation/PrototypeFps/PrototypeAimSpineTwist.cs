using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Procedural upper-spine twist toward camera while aiming (UnityChan has no dedicated ADS parameter on Locomotion).
    /// LateUpdate + 높은 순서: Mecanim이 본을 쓴 뒤에만 척추를 덮어씁니다.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PrototypeAimSpineTwist : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] Transform _spine;
        [SerializeField] float _aimYawWeight = 0.38f;
        [SerializeField] float _aimPitchWeight = 0.15f;
        [Tooltip("Scale twist while rifle equipped so AR upper-body layer stays primary.")]
        [SerializeField] float _twistScaleWhenWeaponEquipped = 0.42f;
        [SerializeField] float _maxYaw = 52f;
        [SerializeField] float _maxPitch = 22f;
        [SerializeField] float _smooth = 14f;

        Quaternion _smoothedTwist = Quaternion.identity;
        IUnityChanLocomotionMotor _motor;
        OsFpsInspiredWeapon _weapon;

        void Awake()
        {
            _motor = UnityChanLocomotionMotorResolver.ResolveOn(gameObject);
            _weapon = GetComponent<OsFpsInspiredWeapon>();
            if (_camera == null)
                _camera = GetComponentInChildren<Camera>(true);
            if (_spine == null)
                _spine = FindChildRecursive(transform, "Character1_Spine1");
            if (_spine == null)
                _spine = FindChildRecursive(transform, "Character1_Spine");
        }

        void LateUpdate()
        {
            if (_spine == null || _camera == null || _motor == null)
                return;

            // Mecanim(베이스 Idle/Loco + WeaponUpper)이 이미 적용된 뒤 호출됨.
            var animSpineLocal = _spine.localRotation;

            if (!_motor.IsAiming)
            {
                _smoothedTwist = Quaternion.Slerp(
                    _smoothedTwist,
                    Quaternion.identity,
                    Time.deltaTime * _smooth);
                return;
            }

            var camF = _camera.transform.forward;
            var bodyF = transform.forward;
            var flatBody = new Vector3(bodyF.x, 0f, bodyF.z);
            var flatCam = new Vector3(camF.x, 0f, camF.z);
            if (flatBody.sqrMagnitude < 0.0001f || flatCam.sqrMagnitude < 0.0001f)
                return;

            var twistMul = _weapon != null && _weapon.IsEquipped ? _twistScaleWhenWeaponEquipped : 1f;

            var yaw = Vector3.SignedAngle(flatBody.normalized, flatCam.normalized, Vector3.up);
            yaw = Mathf.Clamp(yaw, -_maxYaw, _maxYaw) * (_aimYawWeight * twistMul);

            var pitch = -Mathf.Asin(Mathf.Clamp(camF.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -_maxPitch, _maxPitch) * (_aimPitchWeight * twistMul);

            var targetTwist = Quaternion.Euler(pitch, yaw, 0f);
            _smoothedTwist = Quaternion.Slerp(_smoothedTwist, targetTwist, Time.deltaTime * _smooth);
            _spine.localRotation = animSpineLocal * _smoothedTwist;
        }

        static Transform FindChildRecursive(Transform root, string exactName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == exactName)
                    return t;
            }

            return null;
        }
    }
}
