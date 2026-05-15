using UnityEngine;

namespace VRProject.Presentation.OsFpsInspired
{
    /// <summary>
    /// 던진 총 클론이 충돌 시 <see cref="OsFpsInspiredDamageable"/>에 피해를 줍니다. 플레이어 리그는 제외합니다.
    /// </summary>
    public sealed class OsFpsInspiredThrownGunDamage : MonoBehaviour
    {
        float _damage;
        Transform _shooterExclusionRoot;
        bool _oneHitOnly;
        bool _configured;

        public void Configure(
            float damage,
            Transform shooterExclusionRoot,
            float destroyAfterSeconds,
            bool oneHitOnly)
        {
            _damage = damage;
            _shooterExclusionRoot = shooterExclusionRoot;
            _oneHitOnly = oneHitOnly;
            _configured = true;
            if (destroyAfterSeconds > 0f)
                Destroy(gameObject, destroyAfterSeconds);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!_configured || _damage <= 0f || collision == null)
                return;

            var col = collision.collider;
            if (col == null)
                return;

            if (OsFpsInspiredHitscanExclusion.IsColliderUnderExclusionRoot(col, _shooterExclusionRoot))
                return;

            var dmg = col.GetComponentInParent<OsFpsInspiredDamageable>();
            if (dmg == null)
                return;

            var point = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            dmg.ApplyDamage(_damage, point);

            if (_oneHitOnly)
                Destroy(gameObject);
        }
    }
}
