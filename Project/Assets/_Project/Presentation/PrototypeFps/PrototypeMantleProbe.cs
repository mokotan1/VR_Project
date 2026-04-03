using System.Collections;
using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PrototypeThirdPersonPlayer))]
    public sealed class PrototypeMantleProbe : MonoBehaviour
    {
        [SerializeField] float _forwardProbe = 0.55f;
        [SerializeField] float _wallRayHeight = 0.85f;
        [SerializeField] float _maxLedgeSearchUp = 2.1f;
        [SerializeField] float _downSearch = 2.4f;
        [SerializeField] float _stepMax = 0.38f;
        [SerializeField] float _jumpBandMax = 1.15f;
        [SerializeField] float _mantleBandMax = 1.95f;
        [SerializeField] float _jumpAssistVertical = 5.2f;
        [SerializeField] float _jumpAssistForward = 0.32f;
        [SerializeField] float _mantleDuration = 0.38f;
        [SerializeField] float _mantleForwardNudge = 0.22f;
        [SerializeField] LayerMask _probeMask = Physics.DefaultRaycastLayers;

        CharacterController _cc;
        PrototypeThirdPersonPlayer _player;
        Coroutine _active;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _player = GetComponent<PrototypeThirdPersonPlayer>();
        }

        public bool TryBeginFromSpace()
        {
            if (_active != null || _player == null || !_cc.isGrounded)
                return false;

            if (!TryEvaluateLedge(out var deltaY, out var ledgeTop, out var forwardFlat))
                return false;

            var band = MantleHeightBands.Classify(deltaY, _stepMax, _jumpBandMax, _mantleBandMax);
            switch (band)
            {
                case MantleBand.StepOrBelow:
                case MantleBand.None:
                    return false;
                case MantleBand.JumpBand:
                    _player.ApplyLedgeAssist(_jumpAssistVertical, forwardFlat * _jumpAssistForward);
                    _player.ArmAnimationJumpSignal();
                    return true;
                case MantleBand.MantleBand:
                    _active = StartCoroutine(MantleRoutine(ledgeTop, forwardFlat));
                    return true;
                default:
                    return false;
            }
        }

        bool TryEvaluateLedge(out float deltaY, out Vector3 ledgeTop, out Vector3 forwardFlat)
        {
            forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z);
            if (forwardFlat.sqrMagnitude < 0.0001f)
            {
                deltaY = 0f;
                ledgeTop = default;
                return false;
            }

            forwardFlat.Normalize();
            var origin = transform.position + Vector3.up * _wallRayHeight;
            if (!Physics.Raycast(origin, forwardFlat, out var wallHit, _forwardProbe, _probeMask,
                    QueryTriggerInteraction.Ignore))
            {
                deltaY = 0f;
                ledgeTop = default;
                return false;
            }

            if (Vector3.Dot(-forwardFlat, wallHit.normal) < 0.25f)
            {
                deltaY = 0f;
                ledgeTop = default;
                return false;
            }

            var scanStart = wallHit.point + forwardFlat * 0.1f + Vector3.up * _maxLedgeSearchUp;
            if (!Physics.Raycast(scanStart, Vector3.down, out var downHit, _downSearch, _probeMask,
                    QueryTriggerInteraction.Ignore))
            {
                deltaY = 0f;
                ledgeTop = default;
                return false;
            }

            ledgeTop = downHit.point;
            var feetY = _cc.bounds.min.y;
            deltaY = ledgeTop.y - feetY;
            return deltaY > 0.05f;
        }

        IEnumerator MantleRoutine(Vector3 ledgeTop, Vector3 forwardFlat)
        {
            _player.SetMotorLocked(true);
            var start = transform.position;
            var end = start;
            end.y = ledgeTop.y + _cc.skinWidth * 2f;
            end += forwardFlat.normalized * _mantleForwardNudge;
            var elapsed = 0f;
            while (elapsed < _mantleDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _mantleDuration);
                var p = Vector3.Lerp(start, end, t);
                var delta = p - transform.position;
                _cc.Move(delta);
                yield return null;
            }

            _player.SetMotorLocked(false);
            _active = null;
        }
    }
}
