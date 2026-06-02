using System;
using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Forward-moving bullet visual. Optional per-frame segment callback for deferred hit tests (e.g. sphere sweep).
    /// </summary>
    public sealed class PrototypeFpsBulletProjectile : MonoBehaviour
    {
        Vector3 _direction;
        float _speed;
        float _maxDistance;
        Vector3 _spawnPosition;
        Func<Vector3, Vector3, bool> _tryHitMoveSegment;

        public void Launch(
            Vector3 worldDirection,
            float speed,
            float maxDistance,
            Vector3 visualEulerOffset = default,
            Func<Vector3, Vector3, bool> tryHitMoveSegment = null)
        {
            _direction = worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector3.forward;
            _speed = speed;
            _maxDistance = maxDistance;
            _spawnPosition = transform.position;
            _tryHitMoveSegment = tryHitMoveSegment;

            transform.rotation = RotationForFlight(_direction, visualEulerOffset);
        }

        public static Quaternion RotationForFlight(Vector3 worldDirection, Vector3 visualEulerOffset)
        {
            var dir = worldDirection.sqrMagnitude > 1e-6f ? worldDirection.normalized : Vector3.forward;
            var up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.98f ? Vector3.right : Vector3.up;
            var align = Quaternion.LookRotation(dir, up);
            return visualEulerOffset == Vector3.zero
                ? align
                : align * Quaternion.Euler(visualEulerOffset);
        }

        void Update()
        {
            var step = _direction * (_speed * Time.deltaTime);
            var from = transform.position;
            var to = from + step;

            if (_tryHitMoveSegment != null && step.sqrMagnitude > 1e-8f && _tryHitMoveSegment(from, to))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = to;
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistance * _maxDistance)
                Destroy(gameObject);
        }
    }
}
