using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using VRProject.Application.Mobile;
using VRProject.Application.Startup;
using VRProject.Presentation.Startup;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace VRProject.Presentation.Common.UI
{
    /// <summary>
    /// Landscape tablet touch HUD: move joystick, look zone, melee swing band, fire/reload/throw/pause.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileTouchControlPanel : MonoBehaviour
    {
        const string PanelRootName = "MobileTouchPanel";

        [SerializeField] bool _showRegionHints = true;
        [SerializeField] float _lookSensitivity = 0.045f;
        [SerializeField] float _joystickRadiusPixels = 88f;

        readonly Dictionary<int, TouchTrack> _tracks = new Dictionary<int, TouchTrack>(4);
        readonly MobileTouchLayoutRects _layout = MobileTouchLayoutRects.LandscapeTabletDefault;

        MobileTouchInputBus _bus;
        Canvas _canvas;
        RectTransform _joystickKnob;
        RectTransform _joystickBackground;
        bool _fireHeld;
        bool _paused;
        bool _panelActive;
        bool _pendingFirePress;
        bool _pendingReloadPress;
        bool _pendingThrowPress;

        struct TouchTrack
        {
            public MobileTouchRegionKind Region;
            public Vector2 LastScreenPosition;
        }

        void Awake()
        {
            _bus = GetComponentInParent<MobileTouchInputBus>();
            if (_bus == null)
                _bus = gameObject.AddComponent<MobileTouchInputBus>();
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            _panelActive = ShouldActivatePanel();
            if (_panelActive)
                BuildPanelUiIfNeeded();
            else
                SetPanelVisible(false);
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            _tracks.Clear();
            _bus?.Clear();
        }

        void Update()
        {
            _panelActive = ShouldActivatePanel();
            if (!_panelActive)
            {
                _bus?.Clear();
                SetPanelVisible(false);
                return;
            }

            SetPanelVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            var snapshot = new MobileTouchInputSnapshot { IsActive = true, FireHeld = _fireHeld };
            if (_pendingFirePress)
            {
                snapshot.FirePressedThisFrame = true;
                _pendingFirePress = false;
            }

            if (_pendingReloadPress)
            {
                snapshot.ReloadPressedThisFrame = true;
                _pendingReloadPress = false;
            }

            if (_pendingThrowPress)
            {
                snapshot.ThrowPressedThisFrame = true;
                _pendingThrowPress = false;
            }

            ProcessTouches(ref snapshot);
            _bus.Publish(snapshot);
        }

        bool ShouldActivatePanel() => MobileTouchInputBus.ShouldUseMobileControls();

        void ProcessTouches(ref MobileTouchInputSnapshot snapshot)
        {
            var activeIds = new HashSet<int>();
            foreach (var touch in Touch.activeTouches)
            {
                activeIds.Add(touch.touchId);
                if (!_tracks.TryGetValue(touch.touchId, out var track))
                {
                    var normalized = ScreenToNormalized(touch.screenPosition);
                    var region = MobileTouchRegionClassifier.Classify(normalized.x, normalized.y, _layout);
                    if (region == MobileTouchRegionKind.None ||
                        region == MobileTouchRegionKind.FireButton ||
                        region == MobileTouchRegionKind.ReloadButton ||
                        region == MobileTouchRegionKind.ThrowButton ||
                        region == MobileTouchRegionKind.PauseButton)
                        continue;

                    track = new TouchTrack { Region = region, LastScreenPosition = touch.screenPosition };
                    _tracks[touch.touchId] = track;
                }

                track = _tracks[touch.touchId];
                var delta = touch.screenPosition - track.LastScreenPosition;
                track.LastScreenPosition = touch.screenPosition;
                _tracks[touch.touchId] = track;

                switch (track.Region)
                {
                    case MobileTouchRegionKind.MoveJoystick:
                        ApplyJoystick(touch.screenPosition, ref snapshot);
                        break;
                    case MobileTouchRegionKind.Look:
                        snapshot.LookDeltaX += delta.x * _lookSensitivity;
                        snapshot.LookDeltaY += delta.y * _lookSensitivity;
                        break;
                    case MobileTouchRegionKind.MeleeSwing:
                        snapshot.MeleeSwingActive = true;
                        snapshot.MeleeSwingDeltaX += delta.x;
                        snapshot.MeleeSwingDeltaY += delta.y;
                        break;
                }
            }

            var stale = new List<int>();
            foreach (var pair in _tracks)
            {
                if (!activeIds.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            foreach (var id in stale)
                _tracks.Remove(id);

            if (!HasActiveTouchInRegion(MobileTouchRegionKind.MoveJoystick))
            {
                snapshot.MoveAxisX = 0f;
                snapshot.MoveAxisY = 0f;
                ResetJoystickKnob();
            }
        }

        bool HasActiveTouchInRegion(MobileTouchRegionKind region)
        {
            foreach (var pair in _tracks)
            {
                if (pair.Value.Region == region)
                    return true;
            }

            return false;
        }

        void ApplyJoystick(Vector2 screenPosition, ref MobileTouchInputSnapshot snapshot)
        {
            if (_joystickBackground == null)
                return;

            var center = GetRectScreenCenter(_joystickBackground);
            VirtualJoystickLogic.ComputeAxes(
                screenPosition.x,
                screenPosition.y,
                center.x,
                center.y,
                _joystickRadiusPixels,
                out snapshot.MoveAxisX,
                out snapshot.MoveAxisY);

            if (_joystickKnob != null)
            {
                var offset = new Vector2(snapshot.MoveAxisX, snapshot.MoveAxisY) * _joystickRadiusPixels;
                _joystickKnob.anchoredPosition = offset;
            }
        }

        void ResetJoystickKnob()
        {
            if (_joystickKnob != null)
                _joystickKnob.anchoredPosition = Vector2.zero;
        }

        static Vector2 ScreenToNormalized(Vector2 screenPosition)
        {
            var w = Mathf.Max(1f, Screen.width);
            var h = Mathf.Max(1f, Screen.height);
            return new Vector2(screenPosition.x / w, screenPosition.y / h);
        }

        static Vector2 GetRectScreenCenter(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) * 0.5f;
            return center;
        }

        void BuildPanelUiIfNeeded()
        {
            var existing = transform.Find(PanelRootName);
            if (existing != null)
            {
                _canvas = existing.GetComponent<Canvas>();
                CacheUiReferences(existing);
                return;
            }

            var root = new GameObject(PanelRootName, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            if (_showRegionHints)
            {
                CreateRegionHint(root.transform, "LookZone", _layout.LookMinX, _layout.LookMinY, _layout.LookMaxX, _layout.LookMaxY, new Color(1f, 1f, 1f, 0.04f));
                CreateRegionHint(root.transform, "MeleeZone", _layout.MeleeMinX, _layout.MeleeMinY, _layout.MeleeMaxX, _layout.MeleeMaxY, new Color(1f, 0.6f, 0.2f, 0.06f));
            }

            _joystickBackground = CreateRegionHint(root.transform, "MoveJoystick", _layout.MoveJoystickMinX, _layout.MoveJoystickMinY, _layout.MoveJoystickMaxX, _layout.MoveJoystickMaxY, new Color(1f, 1f, 1f, 0.08f));
            _joystickKnob = CreateKnob(_joystickBackground, "Knob");

            CreateActionButton(root.transform, "FireButton", _layout.FireMinX, _layout.FireMinY, _layout.FireMaxX, _layout.FireMaxY, "FIRE", OnFireDown, OnFireUp);
            CreateActionButton(root.transform, "ReloadButton", _layout.ReloadMinX, _layout.ReloadMinY, _layout.ReloadMaxX, _layout.ReloadMaxY, "R", OnReloadPressed);
            CreateActionButton(root.transform, "ThrowButton", _layout.ThrowMinX, _layout.ThrowMinY, _layout.ThrowMaxX, _layout.ThrowMaxY, "THROW", OnThrowPressed);
            CreateActionButton(root.transform, "PauseButton", _layout.PauseMinX, _layout.PauseMinY, _layout.PauseMaxX, _layout.PauseMaxY, "II", OnPausePressed);
        }

        void CacheUiReferences(Transform root)
        {
            var joy = root.Find("MoveJoystick");
            if (joy != null)
            {
                _joystickBackground = joy.GetComponent<RectTransform>();
                var knob = joy.Find("Knob");
                if (knob != null)
                    _joystickKnob = knob.GetComponent<RectTransform>();
            }
        }

        void SetPanelVisible(bool visible)
        {
            var panel = transform.Find(PanelRootName);
            if (panel != null)
                panel.gameObject.SetActive(visible);
        }

        static RectTransform CreateRegionHint(Transform parent, string name, float minX, float minY, float maxX, float maxY, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            ApplyNormalizedRect(rt, minX, minY, maxX, maxY);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        static RectTransform CreateKnob(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(56f, 56f);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);
            image.raycastTarget = false;
            return rt;
        }

        void CreateActionButton(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY,
            string label,
            UnityEngine.Events.UnityAction onDown,
            UnityEngine.Events.UnityAction onUp = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            ApplyNormalizedRect(rt, minX, minY, maxX, maxY);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.12f, 0.16f, 0.72f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onDown);

            if (onUp != null)
            {
                var trigger = go.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entry.callback.AddListener(_ => onUp());
                trigger.triggers.Add(entry);
            }

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
        }

        static void ApplyNormalizedRect(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void OnFireDown()
        {
            _fireHeld = true;
            _pendingFirePress = true;
        }

        void OnFireUp()
        {
            _fireHeld = false;
        }

        void OnReloadPressed() => _pendingReloadPress = true;

        void OnThrowPressed() => _pendingThrowPress = true;

        void OnPausePressed()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
        }
    }
}
