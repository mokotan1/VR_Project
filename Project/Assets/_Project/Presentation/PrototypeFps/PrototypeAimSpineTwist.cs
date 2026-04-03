using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Procedural upper-spine twist toward camera while aiming (UnityChan has no dedicated ADS parameter on Locomotion).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PrototypeAimSpineTwist : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] Transform _spine;
        [SerializeField] float _aimYawWeight = 0.38f;
        [SerializeField] float _aimPitchWeight = 0.15f;
        [SerializeField] float _maxYaw = 52f;
        [SerializeField] float _maxPitch = 22f;
        [SerializeField] float _smooth = 14f;

        Quaternion _baseLocal;
        PrototypeThirdPersonPlayer _player;

        void Awake()
        {
            _player = GetComponent<PrototypeThirdPersonPlayer>();
            if (_spine == null)
                _spine = FindChildRecursive(transform, "Character1_Spine1");
            if (_spine == null)
                _spine = FindChildRecursive(transform, "Character1_Spine");
            if (_spine != null)
                _baseLocal = _spine.localRotation;
        }

        void LateUpdate()
        {
            if (_spine == null || _camera == null || _player == null)
                return;

            if (!_player.IsAiming)
            {
                _spine.localRotation = Quaternion.Slerp(_spine.localRotation, _baseLocal, Time.deltaTime * _smooth);
                return;
            }

            var camF = _camera.transform.forward;
            var bodyF = transform.forward;
            var flatBody = new Vector3(bodyF.x, 0f, bodyF.z);
            var flatCam = new Vector3(camF.x, 0f, camF.z);
            if (flatBody.sqrMagnitude < 0.0001f || flatCam.sqrMagnitude < 0.0001f)
                return;

            var yaw = Vector3.SignedAngle(flatBody.normalized, flatCam.normalized, Vector3.up);
            yaw = Mathf.Clamp(yaw, -_maxYaw, _maxYaw) * _aimYawWeight;

            var pitch = -Mathf.Asin(Mathf.Clamp(camF.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -_maxPitch, _maxPitch) * _aimPitchWeight;

            var twist = Quaternion.Euler(pitch, yaw, 0f);
            _spine.localRotation = Quaternion.Slerp(_spine.localRotation, _baseLocal * twist, Time.deltaTime * _smooth);
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
