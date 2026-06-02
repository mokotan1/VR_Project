using UnityEngine;
using VRProject.Application.Combat;

namespace VRProject.Presentation.Combat
{
    public readonly struct WeaponMotionState
    {
        public WeaponMotionState(
            float linearSpeedMps,
            float angularSpeedDps,
            Vector3 tipVelocity,
            Vector3 swingDirection,
            Vector3 weaponForward,
            WeaponAttackKind activeKind)
        {
            LinearSpeedMps = linearSpeedMps;
            AngularSpeedDps = angularSpeedDps;
            TipVelocity = tipVelocity;
            SwingDirection = swingDirection;
            WeaponForward = weaponForward;
            ActiveKind = activeKind;
        }

        public float LinearSpeedMps { get; }
        public float AngularSpeedDps { get; }
        public Vector3 TipVelocity { get; }
        public Vector3 SwingDirection { get; }
        public Vector3 WeaponForward { get; }
        public WeaponAttackKind ActiveKind { get; }
    }

    [DisallowMultipleComponent]
    public sealed class WeaponMotion : MonoBehaviour
    {
        [SerializeField] WeaponMotionSourceRouter _router;
        [SerializeField] WeaponAttackProfile _profile;
        [SerializeField] Transform _rotationReference;

        Vector3 _previousTip;
        Quaternion _previousRotation;
        bool _hasPreviousSample;
        WeaponMotionState _current;

        public WeaponMotionState Current => _current;

        public void BindSetup(WeaponMotionSourceRouter router, WeaponAttackProfile profile, Transform rotationReference)
        {
            if (router != null)
                _router = router;
            if (profile != null)
                _profile = profile;
            if (rotationReference != null)
                _rotationReference = rotationReference;
        }

        void Awake()
        {
            if (_router == null)
                _router = GetComponent<WeaponMotionSourceRouter>();
            if (_rotationReference == null)
                _rotationReference = transform;
        }

        void FixedUpdate()
        {
            var source = _router != null ? _router.ActiveSource : null;
            if (source == null || !source.IsActive)
            {
                _hasPreviousSample = false;
                _current = default;
                return;
            }

            var pose = source.SamplePose();
            var deltaTime = Time.fixedDeltaTime;
            if (!_hasPreviousSample)
            {
                _previousTip = pose.TipWorldPosition;
                _previousRotation = _rotationReference.rotation;
                _hasPreviousSample = true;
                _current = new WeaponMotionState(0f, 0f, Vector3.zero, Vector3.forward, pose.WeaponForward, WeaponAttackKind.None);
                return;
            }

            var tipDelta = pose.TipWorldPosition - _previousTip;
            var linearSpeed = WeaponMotionSampleLogic.LinearSpeedMetersPerSecond(
                CombatMath.FromUnity(_previousTip),
                CombatMath.FromUnity(pose.TipWorldPosition),
                deltaTime);

            var angle = Quaternion.Angle(_previousRotation, _rotationReference.rotation);
            var angularSpeed = WeaponMotionSampleLogic.AngularSpeedDegreesPerSecond(angle, deltaTime);

            var tipVelocity = deltaTime > 0f ? tipDelta / deltaTime : Vector3.zero;
            var swingDirection = tipDelta.sqrMagnitude > 1e-8f ? tipDelta.normalized : pose.WeaponForward;

            WeaponAttackKind kind = WeaponAttackKind.None;
            if (_profile != null)
            {
                kind = WeaponAttackKindClassifier.Classify(
                    CombatMath.FromUnity(tipVelocity),
                    CombatMath.FromUnity(pose.WeaponForward),
                    CombatMath.FromUnity(pose.WeaponRight),
                    _profile.Family,
                    linearSpeed,
                    angularSpeed,
                    _profile.StabForwardDotMin,
                    _profile.SlashSideDotMin,
                    _profile.BluntMaxAngularSpeed,
                    _profile.BluntMinLinearSpeed);
            }

            _current = new WeaponMotionState(
                linearSpeed,
                angularSpeed,
                tipVelocity,
                swingDirection,
                pose.WeaponForward,
                kind);

            _previousTip = pose.TipWorldPosition;
            _previousRotation = _rotationReference.rotation;
        }
    }
}
