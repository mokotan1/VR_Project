using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest locomotion: WASD, mouse look, Esc to unlock cursor, E to activate exit portal in view.
    /// <see cref="SuperhotGameplayDriver"/>보다 나중에 실행되어 같은 프레임에서 갱신된 시계를 읽습니다(-80 먼음, 이 스크립트 -50).
    /// </summary>
    [DefaultExecutionOrder(-50)]
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

        [Tooltip("SimulationDeltaTime이 거의 0일 때 unscaledDeltaTime으로 이동해 시간을 다시 풀 수 있게 합니다. 끄면 시간이 멈출 때 플레이어도 멈춥니다(적·시계와 정합).")]
        [SerializeField] bool _allowUnscaledMoveWhenSimStopped = true;

        [Tooltip("켜면 마우스 시점 입력에 LastTimeFactor를 곱해 슬로모에서 회전을 몸 이동에 맞춥니다.")]
        [SerializeField] bool _scaleMouseLookByTimeFactor = true;

        [Tooltip("시간 배율이 낮아도 시점이 너무 둔해지지 않게 하는 최소 배율(×).")]
        [Range(0.02f, 1f)]
        [SerializeField] float _mouseLookTimeFactorMin = 0.12f;

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
            // timeScale=0이면 SimulationDeltaTime이 0 — 폴백 켬: 실시간으로만 살짝 움직여 드라이버가 입력을 받게 함. 끔: 적·시계와 동일하게 정지.
            float dt;
            if (simDt > 1e-9f)
                dt = simDt;
            else if (_allowUnscaledMoveWhenSimStopped)
                dt = Time.unscaledDeltaTime;
            else
                dt = 0f;

            var lookScale = 1f;
            if (_scaleMouseLookByTimeFactor && _clock != null)
                lookScale = Mathf.Max(_mouseLookTimeFactorMin, _clock.LastTimeFactor);

            var mx = 0f;
            var my = 0f;
            if (Cursor.lockState == CursorLockMode.Locked && _cameraTransform != null)
            {
                mx = Input.GetAxis("Mouse X") * _mouseSensitivity * lookScale;
                my = Input.GetAxis("Mouse Y") * _mouseSensitivity * lookScale;
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
            var denomForLook = dt > 1e-9f ? dt : Time.unscaledDeltaTime;
            LastLookIntensityPerSecond =
                Cursor.lockState == CursorLockMode.Locked
                    ? new Vector2(mx, my).magnitude / Mathf.Max(1e-6f, denomForLook)
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
