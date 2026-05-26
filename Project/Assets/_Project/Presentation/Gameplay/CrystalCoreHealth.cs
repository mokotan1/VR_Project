using System;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 크리스탈 코어의 체력/피해/파괴 이벤트 모델.
    /// 외부에서 데미지를 받고, Damaged/Destroyed 이벤트를 통해 UI/VFX/오디오/햅틱이 반응한다.
    /// MonoBehaviour 라이프사이클과 무관하게 ResetHealth()로 라운드를 재시작할 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalCoreHealth : MonoBehaviour
    {
        const float MinHealth = 1f;

        [SerializeField] float _maxHealth = 300f;
        [SerializeField] bool _resetOnAwake = true;

        float _health;
        bool _destroyed;

        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public bool IsDestroyed => _destroyed;

        /// <summary>(damageAmount, remainingHealth, hitPoint)</summary>
        public event Action<float, float, Vector3> Damaged;

        /// <summary>(hitPoint)</summary>
        public event Action<Vector3> Destroyed;

        void Awake()
        {
            if (_resetOnAwake)
                ResetHealth(_maxHealth);
        }

        public void ResetHealth(float maxHealth)
        {
            _maxHealth = Mathf.Max(MinHealth, maxHealth);
            _health = _maxHealth;
            _destroyed = false;
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (amount <= 0f || _destroyed)
                return;

            _health = Mathf.Max(0f, _health - amount);
            Damaged?.Invoke(amount, _health, hitPoint);

            if (_health > 0f)
                return;

            _destroyed = true;
            Destroyed?.Invoke(hitPoint);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _maxHealth = Mathf.Max(MinHealth, _maxHealth);
        }
#endif
    }
}
