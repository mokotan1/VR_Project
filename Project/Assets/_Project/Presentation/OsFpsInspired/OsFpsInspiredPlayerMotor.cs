using UnityEngine;
using UnityEngine.InputSystem;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.OsFpsInspired
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-100)]
    public sealed class OsFpsInspiredPlayerMotor : MonoBehaviour
    {
        [SerializeField] Transform _cameraTransform;
        [SerializeField] float _moveSpeed = 5.5f;
        [SerializeField] float _mouseSensitivity = 0.12f;
        [SerializeField] float _gravity = -20f;
        [SerializeField] float _jumpHeight = 1.2f;

        float _pitch;
        Vector3 _velocity;
        IGameplayClock _clock;
        CharacterController _characterController;

        /// <summary>WASD 등 평면 이동 의도 0~1. 시뮬레이션 dt와 무관하게 매 프레임 갱신됩니다.</summary>
        public float LastPlanarMoveIntent01 { get; private set; }

        /// <summary>직전 프레임 기준 수평 속도 크기(m/s).</summary>
        public float LastPlanarSpeedMetersPerSecond { get; private set; }

        public float MoveSpeed => _moveSpeed;

        /// <summary>CharacterController 기준 접지.</summary>
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;

        /// <summary>공중 — CC가 없으면 false.</summary>
        public bool IsAirborne => _characterController != null && !_characterController.isGrounded;

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            var planarInput = SampleKeyboardPlanarInput();
            LastPlanarMoveIntent01 = Mathf.Clamp01(planarInput.magnitude);
            RefreshPlanarSpeedFromController();

            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && _cameraTransform != null)
            {
                var d = Mouse.current.delta.ReadValue();
                transform.Rotate(0f, d.x * _mouseSensitivity, 0f);
                _pitch -= d.y * _mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -88f, 88f);
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            var simDt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            var dt = simDt > 1e-9f ? simDt : Time.unscaledDeltaTime;

            var cc = _characterController != null ? _characterController : GetComponent<CharacterController>();
            _characterController = cc;
            if (cc == null)
                return;

            if (cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;
            else
                _velocity.y += _gravity * dt;

            if (cc.isGrounded && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

            if (planarInput.sqrMagnitude > 1f)
                planarInput.Normalize();

            var move = transform.TransformDirection(planarInput) * _moveSpeed;
            move.y = _velocity.y;
            cc.Move(move * dt);
        }

        static Vector3 SampleKeyboardPlanarInput()
        {
            var kb = Keyboard.current;
            var input = Vector3.zero;
            if (kb == null)
                return input;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            return input;
        }

        void RefreshPlanarSpeedFromController()
        {
            var cc = _characterController != null ? _characterController : GetComponent<CharacterController>();
            if (cc != null)
            {
                var v = cc.velocity;
                LastPlanarSpeedMetersPerSecond = new Vector3(v.x, 0f, v.z).magnitude;
            }
            else
                LastPlanarSpeedMetersPerSecond = 0f;
        }
    }
}

