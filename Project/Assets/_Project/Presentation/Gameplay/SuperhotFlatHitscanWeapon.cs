using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest: left-click hitscan from the main camera; kills <see cref="SuperhotEnemy"/> on impact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotFlatHitscanWeapon : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] float _maxDistance = 80f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;

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

            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _hitMask, QueryTriggerInteraction.Ignore))
                return;

            var enemy = hit.collider.GetComponentInParent<SuperhotEnemy>();
            if (enemy != null)
                enemy.Kill();
        }
    }
}
