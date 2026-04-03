using UnityEngine;
using UnityEngine.InputSystem;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// 이동·시점: 몸통 Yaw + Pitch. 1인칭(눈·뷰모델 총) 또는 어깨 너머(캐릭터 전신이 보이도록) 선택.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeThirdPersonPlayer : MonoBehaviour, IUnityChanLocomotionMotor
    {
        [SerializeField] Camera _camera;
        [SerializeField] Transform _cameraPivot;
        [Tooltip("끄면 순수 1인칭(CS:GO처럼 카메라에 붙은 뷰모델 총). 켜면 어깨 너머로 캐릭터·손 총이 보입니다.")]
        [SerializeField] bool _overShoulderCamera = false;
        [Tooltip("캐릭터 로컬 기준 카메라 위치(대략 오른쪽 어깨 뒤).")]
        [SerializeField] Vector3 _shoulderCameraLocalOffset = new Vector3(0.38f, 1.42f, -2.35f);
        [SerializeField] float _moveSpeed = 4.2f;
        [SerializeField] float _mouseSensitivity = 0.09f;
        [SerializeField] float _aimMouseSensitivityMultiplier = 0.78f;
        [Tooltip("카메라(눈)를 얼굴 메시 안쪽이 아니라 약간 앞·위로 밀어 클리핑을 줄입니다.")]
        [SerializeField] Vector3 _firstPersonCameraLocalOffset = new Vector3(0f, 0.03f, 0.1f);
        [SerializeField] float _defaultFieldOfView = 72f;
        [SerializeField] float _aimFieldOfView = 55f;
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

        /// <summary>Mecanim axes: x = Direction (strafe), y = Speed (Unity-Chan Locomotions).</summary>
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
            {
                _camera.fieldOfView = _defaultFieldOfView;
                if (_overShoulderCamera)
                    _camera.nearClipPlane = Mathf.Max(_camera.nearClipPlane, 0.12f);
                else
                    _camera.nearClipPlane = Mathf.Min(_camera.nearClipPlane, 0.06f);
            }
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
                _pitch = Mathf.Clamp(_pitch, -88f, 88f);
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

            _lastLocomotionAxes = LocomotionInputMapper.ToUnityChanAnimatorAxes(input);

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
            if (_camera == null)
                return;

            if (_overShoulderCamera)
            {
                var shoulderWorld = transform.position + transform.rotation * _shoulderCameraLocalOffset;
                var viewRot = transform.rotation * Quaternion.Euler(_pitch, 0f, 0f);
                _camera.transform.SetPositionAndRotation(shoulderWorld, viewRot);
                return;
            }

            if (_cameraPivot == null)
                return;

            _camera.transform.localPosition = _firstPersonCameraLocalOffset;
            _camera.transform.localRotation = Quaternion.identity;
        }
    }
}
