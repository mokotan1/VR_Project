using System;
using DyrdaDev.FirstPersonController;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// <a href="https://github.com/dyrdadev/first-person-controller-for-unity">dyrdadev/first-person-controller-for-unity</a>의
    /// <see cref="FirstPersonController"/>와 Uni-Chan 애니 브리지/무기를 연결합니다.
    /// 맨틀(PrototypeMantleProbe)은 물리 점프와 충돌할 수 있어 이 모터 조합에서는 넣지 않는 것을 권장합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FirstPersonController))]
    [RequireComponent(typeof(FirstPersonControllerInput))]
    public sealed class DyrdaFirstPersonMotorAdapter : MonoBehaviour, IUnityChanLocomotionMotor
    {
        CharacterController _cc;
        FirstPersonController _fpc;
        IDisposable _jumpSubscription;
        bool _pendingAnimatorJump;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _fpc = GetComponent<FirstPersonController>();
        }

        void Start()
        {
            // FirstPersonController creates _jumped in Awake; adapter Awake can run first (undefined order).
            if (_fpc == null)
                return;
            var jumped = _fpc.Jumped;
            if (jumped != null)
                _jumpSubscription = jumped.Subscribe(_ => _pendingAnimatorJump = true);
        }

        void OnDestroy()
        {
            _jumpSubscription?.Dispose();
        }

        public Vector2 LocomotionAxes => LocomotionInputMapper.ToUnityChanAnimatorAxes(ReadWasd());

        static Vector3 ReadWasd()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return Vector3.zero;

            var input = Vector3.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (input.sqrMagnitude > 1f)
                input.Normalize();
            return input;
        }

        public bool IsGrounded => _cc != null && _cc.isGrounded;

        public bool IsAiming =>
            Mouse.current != null && Mouse.current.rightButton.isPressed && Cursor.lockState == CursorLockMode.Locked;

        public float VerticalVelocity => _cc != null ? _cc.velocity.y : 0f;

        public bool TryCommitJumpToAnimator(bool locomotionLayerReady)
        {
            if (!_pendingAnimatorJump || !locomotionLayerReady)
                return false;
            _pendingAnimatorJump = false;
            return true;
        }

        public void ApplyLedgeAssist(float verticalVelocity, Vector3 horizontalWorldDelta)
        {
            if (_cc == null)
                return;
            _cc.Move(horizontalWorldDelta);
            if (Mathf.Abs(verticalVelocity) > 0.01f)
                _cc.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        public void ArmAnimationJumpSignal()
        {
            _pendingAnimatorJump = true;
        }

        public void SetControlsEnabled(bool enabled)
        {
            if (_fpc != null)
                _fpc.enabled = enabled;
            foreach (var inp in GetComponents<FirstPersonControllerInput>())
                inp.enabled = enabled;

            if (!enabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void SetMotorLocked(bool locked)
        {
            if (_fpc != null)
                _fpc.enabled = !locked;
        }
    }
}
