using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// XRGrabInteractable로 잡아서 던진 물체가 적에게 충돌하면 속도 기반 데미지를 가한다.
    /// 같은 타겟에 대한 짧은 시간 내 다단 히트를 쿨다운으로 차단해 물리 글리치를 방지.
    /// 데미지 = clamp(speed * damagePerMeterPerSecond, 0, maxDamage).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrystalDefenseGrabbableDamage : MonoBehaviour
    {
        const float MinSpeedFloor = 0.1f;

        [SerializeField] float _minSpeedForDamage = 2.5f;
        [SerializeField] float _damagePerMeterPerSecond = 8f;
        [SerializeField] float _maxDamage = 80f;
        [SerializeField] float _oneTargetCooldownSeconds = 0.2f;

        Rigidbody _body;
        OsFpsInspiredDamageable _lastTarget;
        float _lastHitTime;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (_body == null)
                return;

            var speed = _body.linearVelocity.magnitude;
            if (speed < _minSpeedForDamage)
                return;

            var target = collision.collider.GetComponentInParent<OsFpsInspiredDamageable>();
            if (target == null)
                return;

            if (target == _lastTarget && Time.time - _lastHitTime < _oneTargetCooldownSeconds)
                return;

            _lastTarget = target;
            _lastHitTime = Time.time;

            var damage = Mathf.Min(_maxDamage, speed * _damagePerMeterPerSecond);
            var hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            target.ApplyDamage(damage, hitPoint);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _minSpeedForDamage = Mathf.Max(MinSpeedFloor, _minSpeedForDamage);
            _damagePerMeterPerSecond = Mathf.Max(0f, _damagePerMeterPerSecond);
            _maxDamage = Mathf.Max(0f, _maxDamage);
            _oneTargetCooldownSeconds = Mathf.Max(0f, _oneTargetCooldownSeconds);
        }
#endif
    }
}
