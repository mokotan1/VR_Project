using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// Simple forward-moving visual for hitscan weapons; damage stays on the weapon raycast.
    /// </summary>
    public sealed class PrototypeFpsBulletProjectile : MonoBehaviour
    {
        Vector3 _direction;
        float _speed;
        float _maxDistance;
        Vector3 _spawnPosition;

        public void Launch(Vector3 worldDirection, float speed, float maxDistance, Vector3 visualEulerOffset = default)
        {
            _direction = worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector3.forward;
            _speed = speed;
            _maxDistance = maxDistance;
            _spawnPosition = transform.position;

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
            transform.position += _direction * (_speed * Time.deltaTime);
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistance * _maxDistance)
                Destroy(gameObject);
        }
    }
}
