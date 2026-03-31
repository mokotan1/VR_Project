using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Flat playtest rig root: moves the CharacterController so the eye/camera matches a world destination (e.g. exit portal).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotFlatPlaytestRig : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;
        [SerializeField] Transform _camera;

        void Awake()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Places the camera at the destination position and aligns view to <paramref name="worldDestination"/> forward (yaw on root, pitch on camera).
        /// </summary>
        public void TeleportToCameraPose(Transform worldDestination)
        {
            if (worldDestination == null || _camera == null)
                return;

            var hadCc = _characterController != null && _characterController.enabled;
            if (hadCc)
                _characterController.enabled = false;

            var delta = worldDestination.position - _camera.position;
            transform.position += delta;

            SuperhotFlatPlaytestRigPose.DecomposeYawPitchDegrees(worldDestination.forward, out var yawDeg, out var pitchDeg);
            transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            _camera.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

            if (hadCc)
                _characterController.enabled = true;

            var fps = GetComponent<SuperhotFlatFpsController>();
            if (fps != null)
                fps.ApplySyncedPitchDegrees(pitchDeg);
        }
    }
}
