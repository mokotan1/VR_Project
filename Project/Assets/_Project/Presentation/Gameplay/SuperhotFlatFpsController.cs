using UnityEngine;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Desktop playtest locomotion: WASD, mouse look, Esc to unlock cursor, E to activate exit portal in view.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class SuperhotFlatFpsController : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;
        [SerializeField] Transform _cameraTransform;
        [SerializeField] float _moveSpeed = 4.5f;
        [SerializeField] float _mouseSensitivity = 2f;
        [SerializeField] float _gravity = -18f;
        [SerializeField] float _interactMaxDistance = 4f;

        float _pitch;
        Vector3 _velocity;
        IGameplayClock _clock;

        void Awake()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var locator = ServiceLocator.Instance;
            _clock = locator.IsRegistered<IGameplayClock>() ? locator.Resolve<IGameplayClock>() : null;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Keeps vertical look state in sync after an external teleport (e.g. exit portal).
        /// </summary>
        public void ApplySyncedPitchDegrees(float pitchDegrees)
        {
            _pitch = pitchDegrees;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }

            var dt = _clock != null ? _clock.SimulationDeltaTime : Time.deltaTime;
            if (dt <= 0f)
                return;

            if (Cursor.lockState == CursorLockMode.Locked && _cameraTransform != null)
            {
                var mx = Input.GetAxis("Mouse X") * _mouseSensitivity;
                var my = Input.GetAxis("Mouse Y") * _mouseSensitivity;
                transform.Rotate(0f, mx, 0f);
                _pitch -= my;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            if (_characterController == null)
                return;

            if (_characterController.isGrounded && _velocity.y < 0f)
                _velocity.y = -1f;
            else
                _velocity.y += _gravity * dt;

            var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            var move = transform.TransformDirection(input) * _moveSpeed;
            move.y = _velocity.y;
            _characterController.Move(move * dt);

            TryInteractExitPortal();
        }

        void TryInteractExitPortal()
        {
            if (!Input.GetKeyDown(KeyCode.E))
                return;

            var cam = _cameraTransform != null ? _cameraTransform.GetComponent<Camera>() : null;
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _interactMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return;

            var portal = hit.collider.GetComponentInParent<SuperhotGrabExitPortal>();
            if (portal != null)
                portal.ActivateExit();
        }
    }
}
