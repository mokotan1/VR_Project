using UnityEngine;
using UnityEngine.InputSystem;
using VRProject.Application.Mobile;
using VRProject.Application.Startup;
using VRProject.Presentation.Startup;

namespace VRProject.Presentation.Common.UI
{
    /// <summary>
    /// Per-player bus updated by <see cref="MobileTouchControlPanel"/> each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileTouchInputBus : MonoBehaviour
    {
        public static MobileTouchInputBus Instance { get; private set; }

        MobileTouchInputSnapshot _snapshot;

        public MobileTouchInputSnapshot Snapshot => _snapshot;

        public bool IsMobileModeActive => _snapshot.IsActive && ShouldUseMobileControls();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Publish(MobileTouchInputSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public void Clear()
        {
            _snapshot = MobileTouchInputSnapshot.Inactive;
        }

        public static bool ShouldUseMobileControls()
        {
            return MobileTouchControlsPolicy.ShouldUseMobileControls(
                HasExplicitMobileSelection(),
                Touchscreen.current != null);
        }

        public static bool IsMobilePlayModeSelected() => ShouldUseMobileControls();

        static bool HasExplicitMobileSelection()
        {
            return PlayModeSession.Instance != null &&
                   PlayModeSession.Instance.HasSelection &&
                   PlayModeSession.Instance.SelectedMode == PlayModeKind.Mobile;
        }
    }
}
