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
            if (visualEulerOffset == Vector3.zero)
                transform.forward = _direction;
            else
                transform.rotation = Quaternion.LookRotation(_direction) * Quaternion.Euler(visualEulerOffset);
        }

        void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
            if ((transform.position - _spawnPosition).sqrMagnitude > _maxDistance * _maxDistance)
                Destroy(gameObject);
        }
    }
}
