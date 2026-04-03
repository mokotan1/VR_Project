using System;
using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    public sealed class PrototypeFpsPlayerHealth : MonoBehaviour
    {
        [SerializeField] float _maxHealth = 100f;
        float _health;

        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _health > 0f;

        public event Action Defeated;

        void Awake()
        {
            _health = _maxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
                return;
            _health -= amount;
            if (_health <= 0f)
            {
                _health = 0f;
                Defeated?.Invoke();
            }
        }
    }
}
