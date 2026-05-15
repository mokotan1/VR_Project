using UnityEngine;
using UnityEngine.Events;

namespace VRProject.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class GazeInteractable : MonoBehaviour
    {
        [SerializeField] UnityEvent _gazeEntered = new();
        [SerializeField] UnityEvent _gazeExited = new();
        [SerializeField] UnityEvent _clicked = new();

        public UnityEvent GazeEntered => _gazeEntered;
        public UnityEvent GazeExited => _gazeExited;
        public UnityEvent Clicked => _clicked;

        public void NotifyGazeEnter() => _gazeEntered.Invoke();

        public void NotifyGazeExit() => _gazeExited.Invoke();

        public void NotifyClick() => _clicked.Invoke();
    }
}
