using UnityEngine;
using UnityEngine.InputSystem;

namespace VRProject.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class GazeRaycaster : MonoBehaviour
    {
        [SerializeField] Transform _rayOrigin;
        [SerializeField] float _maxDistance = 25f;
        [SerializeField] LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] InputActionProperty _clickAction;
        [SerializeField] bool _allowMouseFallback = true;
        [SerializeField] bool _showDebugRay;

        GazeInteractable _current;

        void OnEnable()
        {
            if (_clickAction.action != null)
                _clickAction.action.Enable();
        }

        void OnDisable()
        {
            if (_clickAction.action != null)
                _clickAction.action.Disable();

            if (_current != null)
                _current.NotifyGazeExit();
            _current = null;
        }

        void Update()
        {
            var origin = _rayOrigin != null ? _rayOrigin : transform;
            var ray = new Ray(origin.position, origin.forward);
            if (_showDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * _maxDistance, Color.cyan);

            var next = ResolveHit(ray);
            var transition = GazeRaycastState.EvaluateTransition(_current, next);
            if (transition.Exit != null)
                transition.Exit.NotifyGazeExit();
            if (transition.Enter != null)
                transition.Enter.NotifyGazeEnter();
            _current = next;

            if (_current != null && WasClickPressedThisFrame())
                _current.NotifyClick();
        }

        GazeInteractable ResolveHit(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _hitMask, QueryTriggerInteraction.Collide))
                return null;

            return hit.collider.GetComponentInParent<GazeInteractable>();
        }

        bool WasClickPressedThisFrame()
        {
            if (_clickAction.action != null && _clickAction.action.WasPressedThisFrame())
                return true;

            return _allowMouseFallback &&
                   Mouse.current != null &&
                   Mouse.current.leftButton.wasPressedThisFrame;
        }
    }
}
