using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Grab (or activate) to teleport the rig to the next node and advance flow.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [DisallowMultipleComponent]
    public sealed class SuperhotGrabExitPortal : MonoBehaviour
    {
        [SerializeField] XROrigin _xrOrigin;

        [Tooltip("Where the HMD should end up after teleport (eye position / play space anchor).")]
        [SerializeField] Transform _cameraWorldDestination;

        [SerializeField] SuperhotCombatZone _owningZone;

        [SerializeField] SuperhotNodeFlow _nodeFlow;

        XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _grab.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDestroy()
        {
            if (_grab != null)
                _grab.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs _)
        {
            if (_xrOrigin == null)
                _xrOrigin = FindAnyObjectByType<XROrigin>();
            if (_xrOrigin == null || _cameraWorldDestination == null)
                return;

            _xrOrigin.MoveCameraToWorldLocation(_cameraWorldDestination.position);
            _xrOrigin.MatchOriginUpCameraForward(_cameraWorldDestination.up, _cameraWorldDestination.forward);

            if (_nodeFlow != null && _owningZone != null)
                _nodeFlow.AdvanceFromZone(_owningZone);

            gameObject.SetActive(false);
        }
    }
}
