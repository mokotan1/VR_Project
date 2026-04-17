using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest locomotion: WASD, mouse look, Esc to unlock cursor, E to activate exit portal in view.
    /// 실행 순서: 이 스크립트(-50) → <see cref="SuperhotGameplayDriver"/>(-20) 순으로 실행됩니다.
    /// 따라서 이동에 쓰이는 <see cref="IGameplayClock.SimulationDeltaTime"/>은 전 프레임에 드라이버가 기록한 값입니다(1프레임 지연, 약 16ms — 의도된 한계).
    /// 이동·중력은 <see cref="IGameplayClock.SimulationDeltaTime"/>만 사용합니다(sim≈0이면 멈춤, unscaled 폴백 없음). 완전 정지에서 슬로모를 풀려면 UI 등 별도 경로가 필요할 수 있습니다.
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

        [Tooltip("끄면(권장·원작 느낌) 시점은 실시간 입력 그대로; 시간 배율은 드라이버가 LastLookIntensity로만 반영합니다.")]
        [SerializeField] bool _scaleMouseLookByTimeFactor;

        [Tooltip("시간 배율이 낮을 때도 시점이 완전히 굳지 않게 하는 하한(× LastTimeFactor).")]
        [Range(0.02f, 1f)]
        [SerializeField] float _mouseLookTimeFactorMin = 0.12f;

        [Tooltip("시점 배율 상한(보통 1). LastTimeFactor를 이 값으로 Clamp.")]
        [Range(0.1f, 2f)]
        [SerializeField] float _mouseLookTimeFactorMax = 1f;

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
            var dt = simDt > 1e-9f ? simDt : 0f;

            var lookScale = 1f;
            if (_scaleMouseLookByTimeFactor && _clock != null)
                lookScale = Mathf.Clamp(_clock.LastTimeFactor, _mouseLookTimeFactorMin, _mouseLookTimeFactorMax);

            var mx = 0f;
            var my = 0f;
            if (Cursor.lockState == CursorLockMode.Locked && _cameraTransform != null)
            {
                var rawMx = Input.GetAxis("Mouse X") * _mouseSensitivity;
                var rawMy = Input.GetAxis("Mouse Y") * _mouseSensitivity;
                mx = rawMx * lookScale;
                my = rawMy * lookScale;
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
            // 드라이버용: 시점 입력 강도는 실시간 초당으로(시점 회전은 위에서 이미 적용됨).
            var rawForIntensity = Cursor.lockState == CursorLockMode.Locked
                ? new Vector2(Input.GetAxis("Mouse X") * _mouseSensitivity, Input.GetAxis("Mouse Y") * _mouseSensitivity).magnitude
                : 0f;
            var udt = Mathf.Max(1e-6f, Time.unscaledDeltaTime);
            LastLookIntensityPerSecond = rawForIntensity / udt;

            TryInteractExitPortal();
        }

        void RefreshPlanarIntentAndSpeed()
        {
            var ax = Input.GetAxisRaw("Horizontal");
            var az = Input.GetAxisRaw("Vertical");
            var m = new Vector3(ax, 0f, az).magnitude;
            LastPlanarMoveIntent01 = m > 1f ? 1f : m;
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

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_mouseLookTimeFactorMin > _mouseLookTimeFactorMax)
                _mouseLookTimeFactorMin = _mouseLookTimeFactorMax;
        }
#endif
    }
}
