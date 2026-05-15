using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class OsFpsInspiredFireCameraImpulse : MonoBehaviour
    {
        Vector3 _localOffset;
        Quaternion _localRotation = Quaternion.identity;
        float _returnSpeed = 18f;

        public void AddImpulse(float kickBack, float pitchKickDegrees, float returnSpeed)
        {
            _returnSpeed = Mathf.Max(1f, returnSpeed);
            _localOffset += Vector3.back * Mathf.Max(0f, kickBack);
            _localRotation = Quaternion.Euler(-Mathf.Max(0f, pitchKickDegrees), 0f, 0f) * _localRotation;
        }

        void LateUpdate()
        {
            var t = 1f - Mathf.Exp(-_returnSpeed * Time.unscaledDeltaTime);
            _localOffset = Vector3.Lerp(_localOffset, Vector3.zero, t);
            _localRotation = Quaternion.Slerp(_localRotation, Quaternion.identity, t);

            transform.localPosition += _localOffset;
            transform.localRotation *= _localRotation;
        }
    }
}
