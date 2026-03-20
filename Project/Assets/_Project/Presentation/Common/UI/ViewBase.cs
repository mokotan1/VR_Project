using UnityEngine;

namespace VRProject.Presentation.Common.UI
{
    /// <summary>
    /// Base class for all UI views. Handles lifecycle hooks
    /// and provides a clean separation between visual presentation and logic.
    /// </summary>
    public abstract class ViewBase : MonoBehaviour
    {
        private bool _isInitialized;

        protected virtual void OnEnable()
        {
            if (!_isInitialized)
            {
                OnInitialize();
                _isInitialized = true;
            }

            OnShow();
        }

        protected virtual void OnDisable()
        {
            OnHide();
        }

        /// <summary>
        /// Called once when the view is first enabled.
        /// Use for one-time setup like caching component references.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called each time the view becomes visible.
        /// Use for subscribing to events and refreshing displayed data.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called each time the view is hidden.
        /// Use for unsubscribing from events and cleanup.
        /// </summary>
        protected virtual void OnHide() { }
    }
}
