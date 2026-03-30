using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Disables thumbstick / teleport locomotion so play stays room-scale + node grab only (SUPERHOT VR style).
    /// </summary>
    [DefaultExecutionOrder(-30)]
    [DisallowMultipleComponent]
    public sealed class SuperhotLocomotionDisabler : MonoBehaviour
    {
        [SerializeField] bool _disableOnAwake = true;

        [Tooltip("If set, only providers under this transform are disabled; otherwise the whole loaded scene is scanned.")]
        [SerializeField] Transform _searchRoot;

        void Awake()
        {
            if (!_disableOnAwake)
                return;

            var root = _searchRoot != null ? _searchRoot : null;
            DisableUnder(root);
        }

        public void DisableUnder(Transform root)
        {
            if (root != null)
            {
                foreach (var c in root.GetComponentsInChildren<ContinuousMoveProvider>(true))
                    c.enabled = false;
                foreach (var t in root.GetComponentsInChildren<TeleportationProvider>(true))
                    t.enabled = false;
                foreach (var s in root.GetComponentsInChildren<SnapTurnProvider>(true))
                    s.enabled = false;
                foreach (var ct in root.GetComponentsInChildren<ContinuousTurnProvider>(true))
                    ct.enabled = false;
                return;
            }

            foreach (var c in FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                c.enabled = false;
            foreach (var t in FindObjectsByType<TeleportationProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                t.enabled = false;
            foreach (var s in FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                s.enabled = false;
            foreach (var ct in FindObjectsByType<ContinuousTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ct.enabled = false;
        }
    }
}
