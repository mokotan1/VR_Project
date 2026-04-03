using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// 공통 조작·조준·점프 시그널. 내장 <see cref="PrototypeThirdPersonPlayer"/> 또는
    /// Dyrda <c>FirstPersonController</c> + <see cref="DyrdaFirstPersonMotorAdapter"/>에서 구현합니다.
    /// </summary>
    public interface IUnityChanLocomotionMotor
    {
        Vector2 LocomotionAxes { get; }
        bool IsGrounded { get; }
        bool IsAiming { get; }
        float VerticalVelocity { get; }

        bool TryCommitJumpToAnimator(bool locomotionLayerReady);
        void ApplyLedgeAssist(float verticalVelocity, Vector3 horizontalWorldDelta);
        void ArmAnimationJumpSignal();
        void SetControlsEnabled(bool enabled);
        void SetMotorLocked(bool locked);
    }
}
