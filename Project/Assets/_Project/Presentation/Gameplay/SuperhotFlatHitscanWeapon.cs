using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest: left-click hitscan from the main camera; kills <see cref="SuperhotEnemy"/> on impact.
    /// </summary>
    [DefaultExecutionOrder(-85)]
    [DisallowMultipleComponent]
    public sealed class SuperhotFlatHitscanWeapon : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] float _maxDistance = 80f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;

        [Tooltip("발사 직후 이 시간(실시간 초) 동안 시간 배율 목표를 1로 고정하는 데 쓰입니다.")]
        [SerializeField] float _fullTimeScaleHoldSeconds = 0.15f;

        float _lastShootUnscaledTime = -1e9f;

        /// <summary><see cref="_fullTimeScaleHoldSeconds"/> 구간 안이면 true.</summary>
        public bool IsInShootTimeScaleHold =>
            Time.unscaledTime - _lastShootUnscaledTime < Mathf.Max(0f, _fullTimeScaleHoldSeconds);

        void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            _lastShootUnscaledTime = Time.unscaledTime;

            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _hitMask, QueryTriggerInteraction.Ignore))
                return;

            var enemy = hit.collider.GetComponentInParent<SuperhotEnemy>();
            if (enemy != null)
                enemy.Kill(hit);
        }
    }
}
