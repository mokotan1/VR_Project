using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Application.Startup;
using VRProject.Presentation.Common.UI;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// Renders the startup device-connection panel: platform/XR status,
    /// the Mobile and VR play-mode buttons, and a Refresh button. Reads the
    /// runtime state from <see cref="DeviceConnectionProbe"/> and writes the
    /// chosen mode into <see cref="PlayModeSession"/> before loading
    /// gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeviceConnectionView : ViewBase
    {
        [Header("Scene")]
        [SerializeField] string _gameplaySceneName = "UnityChanPrototypeFps";

        [Header("Dependencies")]
        [SerializeField] DeviceConnectionProbe _probe;
        [SerializeField] PlayModeSession _session;

        [Header("Status Text")]
        [SerializeField] Text _platformText;
        [SerializeField] Text _bluetoothStatusText;
        [SerializeField] Text _metaQuestStatusText;
        [SerializeField] Text _xrStatusText;
        [SerializeField] Text _mobileStatusText;
        [SerializeField] Text _messageText;

        [Header("Buttons")]
        [SerializeField] Button _refreshButton;
        [SerializeField] Button _mobilePlayButton;
        [SerializeField] Button _vrPlayButton;

        DeviceConnectionStatus _status;

        protected override void OnInitialize()
        {
            if (_probe == null)
                _probe = FindAnyObjectByType<DeviceConnectionProbe>();
            if (_session == null)
                _session = FindAnyObjectByType<PlayModeSession>();

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(Refresh);
            if (_mobilePlayButton != null)
                _mobilePlayButton.onClick.AddListener(() => SelectMode(PlayModeKind.Mobile));
            if (_vrPlayButton != null)
                _vrPlayButton.onClick.AddListener(() => SelectMode(PlayModeKind.Vr));
        }

        protected override void OnShow()
        {
            Refresh();
        }

        void Refresh()
        {
            if (_probe == null)
                return;

            _status = _probe.Refresh();
            Render(_status);
        }

        void Render(DeviceConnectionStatus status)
        {
            if (_platformText != null)
                _platformText.text = "Device: " + status.PlatformLabel;

            if (_bluetoothStatusText != null)
                _bluetoothStatusText.text = status.BluetoothStatusText;

            if (_metaQuestStatusText != null)
                _metaQuestStatusText.text = status.MetaQuestStatusText;

            if (_xrStatusText != null)
            {
                var xrName = string.IsNullOrEmpty(status.XrDeviceName) ? "None" : status.XrDeviceName;
                _xrStatusText.text = status.VrStatusText + " (" + xrName + ")";
            }

            if (_mobileStatusText != null)
                _mobileStatusText.text = status.MobileStatusText;

            if (_mobilePlayButton != null)
                _mobilePlayButton.interactable = PlayModeSelection.CanSelect(PlayModeKind.Mobile, status.Availability);

            if (_vrPlayButton != null)
                _vrPlayButton.interactable = PlayModeSelection.CanSelect(PlayModeKind.Vr, status.Availability);

            if (_messageText != null)
                _messageText.text = ResolveMessage(status.Availability);
        }

        static string ResolveMessage(PlayModeAvailability availability)
        {
            if (availability.VrAvailable && availability.MobileAvailable)
                return "Both play modes are available. Choose how you want to play.";
            if (availability.MobileAvailable)
                return "Mobile Play is available. Pair Meta Quest over Bluetooth or connect XR, then press Refresh to enable VR Play.";
            if (availability.VrAvailable)
                return "VR Play is available. Mobile Play is unavailable on this platform.";
            return "No playable mode is available. Check the connected device and press Refresh.";
        }

        void SelectMode(PlayModeKind requestedMode)
        {
            var resolved = PlayModeSelection.ResolveSelectedMode(requestedMode, _status.Availability);
            if (resolved == PlayModeKind.None)
            {
                if (_messageText != null)
                    _messageText.text = "No playable mode is available. Check the connected device and refresh.";
                return;
            }

            if (_session != null)
                _session.SetSelectedMode(resolved);

            SceneManager.LoadScene(_gameplaySceneName);
        }
    }
}
