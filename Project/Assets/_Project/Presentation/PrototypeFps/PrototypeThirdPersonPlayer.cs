using UnityEngine;
using UnityEngine.InputSystem;

namespace VRProject.Presentation.PrototypeFps
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeThirdPersonPlayer : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] Transform _cameraPivot;
        [SerializeField] float _moveSpeed = 4.2f;
        [SerializeField] float _mouseSensitivity = 0.09f;
        [SerializeField] float _aimMouseSensitivityMultiplier = 0.78f;
        [SerializeField] float _cameraDistance = 3.4f;
        [SerializeField] float _lookTargetHeight = 1.35f;
        [SerializeField] float _cameraSideOffset = 0.28f;
        [SerializeField] float _defaultFieldOfView = 60f;
        [SerializeField] float _aimFieldOfView = 48f;
        [SerializeField] float _fovLerpSpeed = 10f;
        [SerializeField] float _jumpPower = 6.5f;
        [SerializeField] PrototypeMantleProbe _mantleProbe;

        float _yaw;
        float _pitch;
        float _verticalVelocity;
        Vector2 _lastLocomotionAxes;
        bool _jumpRequested;
        float _jumpRequestAge;
        bool _controlsEnabled = true;
        bool _motorLocked;
        CharacterController _characterController;

        public Vector2 LocomotionAxes => _lastLocomotionAxes;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsAiming =>
            Mouse.current != null && Mouse.current.rightButton.isPressed && Cursor.lockState == CursorLockMode.Locked;

        public float VerticalVelocity => _verticalVelocity;

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_mantleProbe == null)
                _mantleProbe = GetComponent<PrototypeMantleProbe>();
            if (_camera != null)
                _camera.fieldOfView = _defaultFieldOfView;
        }

        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
            if (!enabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void SetMotorLocked(bool locked)
        {
            _motorLocked = locked;
            if (locked)
                _verticalVelocity = 0f;
        }

        public bool TryCommitJumpToAnimator(bool locomotionLayerReady)
        {
            if (!_jumpRequested || !locomotionLayerReady)
                return false;
            _jumpRequested = false;
            _jumpRequestAge = 0f;
            return true;
        }

        /// <summary>
        /// Used by <see cref="PrototypeMantleProbe"/> for mid-height ledges in the same frame as the jump input.
        /// </summary>
        public void ApplyLedgeAssist(float verticalVelocity, Vector3 horizontalWorldDelta)
        {
            _verticalVelocity = verticalVelocity;
            _characterController.Move(horizontalWorldDelta);
        }

        public void ArmAnimationJumpSignal()
        {
            _jumpRequested = true;
            _jumpRequestAge = 0f;
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            if (!_controlsEnabled || _camera == null || _cameraPivot == null)
                return;

            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
            {
                var sens = IsAiming ? _mouseSensitivity * _aimMouseSensitivityMultiplier : _mouseSensitivity;
                var d = Mouse.current.delta.ReadValue();
                _yaw += d.x * sens;
                _pitch -= d.y * sens;
                _pitch = Mathf.Clamp(_pitch, -8f, 52f);
            }

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            var targetFov = IsAiming ? _aimFieldOfView : _defaultFieldOfView;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * _fovLerpSpeed);

            if (_motorLocked)
                return;

            var kb = Keyboard.current;
            var input = Vector3.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            }

            _lastLocomotionAxes = LocomotionInputMapper.ToUnityChanAxes(input);

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var horizontal = transform.TransformDirection(input) * _moveSpeed;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            if (_characterController.isGrounded && kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                var mantleHandled = _mantleProbe != null && _mantleProbe.TryBeginFromSpace();
                if (!mantleHandled)
                {
                    _jumpRequested = true;
                    _jumpRequestAge = 0f;
                    _verticalVelocity = _jumpPower;
                }
            }

            var move = horizontal + Vector3.up * _verticalVelocity;
            _characterController.Move(move * Time.deltaTime);

            if (_jumpRequested)
            {
                _jumpRequestAge += Time.deltaTime;
                if (_jumpRequestAge > 0.35f)
                {
                    _jumpRequested = false;
                    _jumpRequestAge = 0f;
                }
            }
            else
                _jumpRequestAge = 0f;
        }

        void LateUpdate()
        {
            if (_camera == null || _cameraPivot == null)
                return;

            var side = _cameraPivot.right * _cameraSideOffset;
            _camera.transform.position = _cameraPivot.position + side + _cameraPivot.forward * (-_cameraDistance);
            var lookAt = transform.position + Vector3.up * _lookTargetHeight;
            _camera.transform.LookAt(lookAt, Vector3.up);
        }
    }
}
