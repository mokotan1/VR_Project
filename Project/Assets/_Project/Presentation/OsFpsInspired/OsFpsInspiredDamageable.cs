using System;
using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    public sealed class OsFpsInspiredDamageable : MonoBehaviour
    {
        [SerializeField] float _maxHealth = 100f;
        float _health;

        public float Health => _health;
        public float MaxHealth => _maxHealth;

        public event Action<float, Vector3> Damaged;

        void Awake()
        {
            _health = _maxHealth;
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (amount <= 0f || _health <= 0f)
                return;
            _health -= amount;
            Damaged?.Invoke(amount, hitPoint);
            if (_health <= 0f)
                Destroy(gameObject);
        }
    }
}
