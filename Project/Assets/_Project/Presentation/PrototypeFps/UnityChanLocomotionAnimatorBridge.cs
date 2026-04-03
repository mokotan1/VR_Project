using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    [RequireComponent(typeof(PrototypeThirdPersonPlayer))]
    public sealed class UnityChanLocomotionAnimatorBridge : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int DirectionId = Animator.StringToHash("Direction");
        static readonly int JumpId = Animator.StringToHash("Jump");

        [SerializeField] float _animatorSpeed = 1.35f;
        [SerializeField] Animator _animator;

        PrototypeThirdPersonPlayer _player;

        void Awake()
        {
            _player = GetComponent<PrototypeThirdPersonPlayer>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        void LateUpdate()
        {
            if (_animator == null)
                return;

            _animator.speed = _animatorSpeed;
            var axes = _player.LocomotionAxes;
            _animator.SetFloat(DirectionId, axes.x);
            _animator.SetFloat(SpeedId, axes.y);

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            var jumpReady = !_animator.IsInTransition(0) && info.IsName("Base Layer.Locomotion");
            if (_player.TryCommitJumpToAnimator(jumpReady))
                _animator.SetBool(JumpId, true);

            if (info.IsName("Base Layer.Jump") && !_animator.IsInTransition(0))
                _animator.SetBool(JumpId, false);
        }
    }
}
