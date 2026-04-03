using UnityEngine;
using UnityEngine.InputSystem;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.OsFpsInspired
{
    [RequireComponent(typeof(CharacterController))]
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

        void Awake()
        {
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

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            if (dt <= 0f)
                return;

            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && _cameraTransform != null)
            {
                var d = Mouse.current.delta.ReadValue();
                transform.Rotate(0f, d.x * _mouseSensitivity, 0f);
                _pitch -= d.y * _mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -88f, 88f);
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            var cc = GetComponent<CharacterController>();
            if (cc == null)
                return;

            if (cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;
            else
                _velocity.y += _gravity * dt;

            if (cc.isGrounded && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

            var kb = Keyboard.current;
            var input = Vector3.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            }

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var move = transform.TransformDirection(input) * _moveSpeed;
            move.y = _velocity.y;
            cc.Move(move * dt);
        }
    }
}
