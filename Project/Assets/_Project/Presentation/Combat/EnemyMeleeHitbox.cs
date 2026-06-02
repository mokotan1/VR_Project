using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyMeleeHitbox : MonoBehaviour
    {
        EnemyMeleeAttackController _controller;
        Collider _collider;

        public void Bind(EnemyMeleeAttackController controller) => _controller = controller;

        void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;

            SetActive(false);
        }

        public void SetActive(bool enabled)
        {
            if (_collider != null)
                _collider.enabled = enabled;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_controller == null || !_controller.IsHitboxActive || _controller.PlayerHitRegistered)
                return;

            if (TryBlock(other))
                return;

            var health = other.GetComponentInParent<SuperhotPlaytestPlayerHealth>();
            if (health != null)
                _controller.RegisterPlayerHit(health);
        }

        bool TryBlock(Collider other)
        {
            var shield = other.GetComponentInParent<ShieldBlocker>();
            if (shield == null)
                return false;

            var hitPoint = other.ClosestPoint(transform.position);
            if (!shield.TryBlock(_controller.ApproachDirection, hitPoint, out _))
                return false;

            _controller.RegisterBlocked();
            return true;
        }
    }
}
