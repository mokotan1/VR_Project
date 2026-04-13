using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest locomotion: WASD, mouse look, Esc to unlock cursor, E to activate exit portal in view.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class SuperhotFlatFpsController : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;
        [SerializeField] Transform _cameraTransform;
        [SerializeField] float _moveSpeed = 4.5f;
        [SerializeField] float _mouseSensitivity = 2f;
        [SerializeField] float _gravity = -18f;
        [SerializeField] float _interactMaxDistance = 4f;

        float _pitch;
        Vector3 _velocity;
        IGameplayClock _clock;

        public float LastPlanarSpeedMetersPerSecond { get; private set; }

        public float LastLookIntensityPerSecond { get; private set; }

        /// <summary>Horizontal/Vertical 축 기준 평면 이동 의도 0~1. 시뮬레이션 dt와 무관하게 갱신됩니다.</summary>
        public float LastPlanarMoveIntent01 { get; private set; }

        /// <summary>CharacterController 기준 접지(컨트롤러 없으면 false).</summary>
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;

        /// <summary>공중 — CC가 없으면 false(미정으로 공중 취급 안 함).</summary>
        public bool IsAirborne => _characterController != null && !_characterController.isGrounded;

        public float MoveSpeed => _moveSpeed;

        void Awake()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ApplySyncedPitchDegrees(float pitchDegrees)
        {
            _pitch = pitchDegrees;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }

            RefreshPlanarIntentAndSpeed();

            var simDt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            // timeScale=0이면 SimulationDeltaTime이 0이 되어 이동이 막히고, 드라이버가 의도·속도를 못 받아 영구 정지됨.
            // 완전 정지 시에도 이 프레임의 이동만 실시간으로 적용해 시간을 다시 풀 수 있게 함.
            var dt = simDt > 1e-9f ? simDt : Time.unscaledDeltaTime;

            var mx = 0f;
            var my = 0f;
            if (Cursor.lockState == CursorLockMode.Locked && _cameraTransform != null)
            {
                mx = Input.GetAxis("Mouse X") * _mouseSensitivity;
                my = Input.GetAxis("Mouse Y") * _mouseSensitivity;
                transform.Rotate(0f, mx, 0f);
                _pitch -= my;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            if (_characterController == null)
                return;

            if (_characterController.isGrounded && _velocity.y < 0f)
                _velocity.y = -1f;
            else
                _velocity.y += _gravity * dt;

            var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            var move = transform.TransformDirection(input) * _moveSpeed;
            move.y = _velocity.y;
            _characterController.Move(move * dt);

            var vel = _characterController.velocity;
            LastPlanarSpeedMetersPerSecond = new Vector3(vel.x, 0f, vel.z).magnitude;
            LastLookIntensityPerSecond =
                Cursor.lockState == CursorLockMode.Locked
                    ? new Vector2(mx, my).magnitude / dt
                    : 0f;

            TryInteractExitPortal();
        }

        void RefreshPlanarIntentAndSpeed()
        {
            var ax = Input.GetAxis("Horizontal");
            var az = Input.GetAxis("Vertical");
            var m = new Vector3(ax, 0f, az).magnitude;
            LastPlanarMoveIntent01 = m > 1f ? 1f : m;

            if (_characterController != null)
            {
                var v = _characterController.velocity;
                LastPlanarSpeedMetersPerSecond = new Vector3(v.x, 0f, v.z).magnitude;
            }
            else
                LastPlanarSpeedMetersPerSecond = 0f;
        }

        void TryInteractExitPortal()
        {
            if (!Input.GetKeyDown(KeyCode.E))
                return;

            var cam = _cameraTransform != null ? _cameraTransform.GetComponent<Camera>() : null;
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _interactMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return;

            var portal = hit.collider.GetComponentInParent<SuperhotGrabExitPortal>();
            if (portal != null)
                portal.ActivateExit();
        }
    }
}
