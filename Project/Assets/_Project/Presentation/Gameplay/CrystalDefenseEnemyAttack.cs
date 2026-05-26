using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 적의 근접 공격 컴포넌트. 크리스탈 또는 플레이어가 사거리 내에 있을 때
    /// 쿨다운 검사 후 데미지를 적용한다.
    /// SuperhotPlaytestPlayerHealth는 hit-count 기반이므로 ApplyHit() 1회를 호출한다
    /// (플랜의 ApplyDamage(float) 가정과 다른 실제 API에 맞춘 적응).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseEnemyAttack : MonoBehaviour
    {
        const float MinAttackRange = 0.1f;
        const float MinCooldown = 0.05f;

        [SerializeField] float _attackRange = 1.6f;
        [SerializeField] float _damage = 15f;
        [SerializeField] float _cooldownSeconds = 1.1f;

        float _nextAttackTime;
        bool _consumedByCrystalHit;

        public float AttackRange => _attackRange;
        public float Damage => _damage;

        public bool IsInRange(Transform target)
        {
            if (target == null)
                return false;
            return Vector3.Distance(transform.position, target.position) <= _attackRange;
        }

        public bool TryAttackCrystal(CrystalCoreHealth crystal, Vector3 hitPoint)
        {
            if (_consumedByCrystalHit || crystal == null || crystal.IsDestroyed || !IsInRange(crystal.transform))
                return false;
            if (Time.time < _nextAttackTime)
                return false;

            _consumedByCrystalHit = true;
            _nextAttackTime = Time.time + _cooldownSeconds;
            crystal.ApplyDamage(_damage, hitPoint);
            ConsumeEnemy();
            return true;
        }

        public bool TryAttackPlayer(SuperhotPlaytestPlayerHealth player)
        {
            if (player == null || !player.IsAlive || !IsInRange(player.transform))
                return false;
            if (Time.time < _nextAttackTime)
                return false;

            _nextAttackTime = Time.time + _cooldownSeconds;
            player.ApplyHit();
            return true;
        }

        void ConsumeEnemy()
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _attackRange = Mathf.Max(MinAttackRange, _attackRange);
            _damage = Mathf.Max(0f, _damage);
            _cooldownSeconds = Mathf.Max(MinCooldown, _cooldownSeconds);
        }
#endif
    }
}
