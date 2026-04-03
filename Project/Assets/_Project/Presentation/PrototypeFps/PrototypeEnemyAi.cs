using System;
using UnityEngine;
using UnityEngine.AI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// NavMesh enemy: chase, hitscan, cover on damage. Uses NavMeshLink in scene for vertical parkour.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public sealed class PrototypeEnemyAi : MonoBehaviour
    {
        [SerializeField] Transform _muzzle;
        [SerializeField] float _sightRange = 32f;
        [SerializeField] float _shootInterval = 1.65f;
        [SerializeField] float _burstDamage = 11f;
        [SerializeField] float _coverDuration = 2.8f;
        [SerializeField] LayerMask _raycastMask = Physics.DefaultRaycastLayers;

        NavMeshAgent _agent;
        OsFpsInspiredDamageable _health;
        Transform _player;
        Transform[] _coverPoints = Array.Empty<Transform>();
        float _nextShot;
        float _coverUntil;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<OsFpsInspiredDamageable>();
            var enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                _raycastMask &= ~(1 << enemyLayer);
        }

        void Start()
        {
            _nextShot = UnityEngine.Random.Range(0.35f, 1.1f);
        }

        void OnEnable()
        {
            if (_health != null)
                _health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
        }

        public void SetCoverPoints(Transform[] points)
        {
            _coverPoints = points ?? Array.Empty<Transform>();
        }

        void OnDamaged(float _, Vector3 __)
        {
            _coverUntil = Time.time + _coverDuration;
            _agent.isStopped = false;
            var dest = PickCoverPoint();
            if (dest != null)
                _agent.SetDestination(dest.position);
        }

        Transform PickCoverPoint()
        {
            if (_player == null || _coverPoints.Length == 0)
                return null;

            Transform best = null;
            var bestScore = float.NegativeInfinity;
            var away = (transform.position - _player.position).normalized;

            foreach (var c in _coverPoints)
            {
                if (c == null)
                    continue;
                var toCover = (c.position - transform.position).normalized;
                var score = Vector3.Dot(toCover, away);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        void Update()
        {
            if (_player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                    _player = go.transform;
                return;
            }

            var inCover = Time.time < _coverUntil;
            if (inCover)
            {
                if (!_agent.pathPending && _agent.remainingDistance < 0.45f)
                    _agent.isStopped = true;
            }
            else
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }

            _nextShot -= Time.deltaTime;
            if (_nextShot > 0f)
                return;

            if (!HasLineOfSight())
                return;

            _nextShot = _shootInterval;
            FireHitscan();
        }

        bool HasLineOfSight()
        {
            if (_muzzle == null)
                return false;

            var eye = _player.position + Vector3.up * 1.25f;
            var raw = _muzzle.position;
            var to = eye - raw;
            var dist = to.magnitude;
            if (dist > _sightRange || dist < 0.01f)
                return false;

            var dir = to / dist;
            var origin = raw + dir * 0.35f;
            dist = (eye - origin).magnitude;
            if (!Physics.Raycast(origin, dir, out var hit, dist, _raycastMask, QueryTriggerInteraction.Ignore))
                return true;

            return hit.collider.GetComponentInParent<PrototypeFpsPlayerHealth>() != null;
        }

        void FireHitscan()
        {
            if (_muzzle == null || _player == null)
                return;

            var eye = _player.position + Vector3.up * 1.25f;
            var raw = _muzzle.position;
            var to = eye - raw;
            if (to.sqrMagnitude < 1e-4f)
                return;
            var dir = to.normalized;
            var origin = raw + dir * 0.35f;
            if (!Physics.Raycast(origin, dir, out var hit, _sightRange, _raycastMask, QueryTriggerInteraction.Ignore))
                return;

            var hp = hit.collider.GetComponentInParent<PrototypeFpsPlayerHealth>();
            if (hp != null && hp.IsAlive)
                hp.ApplyDamage(_burstDamage);
        }
    }
}
