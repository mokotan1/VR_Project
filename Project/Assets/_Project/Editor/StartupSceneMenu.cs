#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Startup;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Editor menu that generates the startup device-selection scene
    /// (Assets/Scenes/Startup.unity) with a screen-space canvas, status
    /// labels, refresh/mobile/VR buttons, and the persistent
    /// PlayModeSession + DeviceConnectionProbe components wired up.
    /// </summary>
    public static class StartupSceneMenu
    {
        const string StartupScenePath = "Assets/Scenes/Startup.unity";
        const string GameplaySceneName = "CrystalDefensePrototype";

        [MenuItem("VR Project/Scenes/Create Startup Device Selection Scene")]
        public static void CreateStartupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("StartupSystems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<PlayModeSession>();
            systems.AddComponent<DeviceConnectionProbe>();

            var canvasGo = new GameObject("StartupCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = CreatePanel(canvasGo.transform, "Root", new Vector2(0.5f, 0.5f), new Vector2(780f, 560f));
            var title = CreateText(root.transform, "Title", "Device Connection", 32, new Vector2(0f, 210f), new Vector2(700f, 60f));
            title.alignment = TextAnchor.MiddleCenter;

            var platformText = CreateText(root.transform, "PlatformText", "Device: Checking...", 22, new Vector2(0f, 140f), new Vector2(700f, 40f));
            var xrText = CreateText(root.transform, "XrStatusText", "VR Headset: Checking...", 22, new Vector2(0f, 90f), new Vector2(700f, 40f));
            var mobileText = CreateText(root.transform, "MobileStatusText", "Mobile Play: Checking...", 22, new Vector2(0f, 40f), new Vector2(700f, 40f));
            var messageText = CreateText(root.transform, "MessageText", "Choose a play mode after device status is checked.", 20, new Vector2(0f, -25f), new Vector2(700f, 60f));
            messageText.alignment = TextAnchor.MiddleCenter;

            var refreshButton = CreateButton(root.transform, "RefreshButton", "Refresh", new Vector2(-240f, -125f), new Vector2(160f, 54f));
            var mobileButton = CreateButton(root.transform, "MobilePlayButton", "Mobile Play", new Vector2(0f, -125f), new Vector2(180f, 54f));
            var vrButton = CreateButton(root.transform, "VrPlayButton", "VR Play", new Vector2(240f, -125f), new Vector2(180f, 54f));

            var helpText = CreateText(
                root.transform,
                "HelpText",
                "Android tablet: use Mobile Play. Headset: connect/enable XR, then press Refresh.",
                16,
                new Vector2(0f, -200f),
                new Vector2(720f, 40f));
            helpText.alignment = TextAnchor.MiddleCenter;
            helpText.color = new Color(0.78f, 0.83f, 0.9f, 1f);

            var view = root.gameObject.AddComponent<DeviceConnectionView>();
            var so = new SerializedObject(view);
            so.FindProperty("_gameplaySceneName").stringValue = GameplaySceneName;
            so.FindProperty("_probe").objectReferenceValue = systems.GetComponent<DeviceConnectionProbe>();
            so.FindProperty("_session").objectReferenceValue = systems.GetComponent<PlayModeSession>();
            so.FindProperty("_platformText").objectReferenceValue = platformText;
            so.FindProperty("_xrStatusText").objectReferenceValue = xrText;
            so.FindProperty("_mobileStatusText").objectReferenceValue = mobileText;
            so.FindProperty("_messageText").objectReferenceValue = messageText;
            so.FindProperty("_refreshButton").objectReferenceValue = refreshButton;
            so.FindProperty("_mobilePlayButton").objectReferenceValue = mobileButton;
            so.FindProperty("_vrPlayButton").objectReferenceValue = vrButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StartupScenePath);
            AddSceneToBuildSettingsIfNeeded(StartupScenePath, first: true);
            AssetDatabase.Refresh();

            Debug.Log("[VR Project] Saved startup device selection scene: " + StartupScenePath);
        }

        static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.92f);
            return rect;
        }

        static Text CreateText(Transform parent, string name, string text, int size, Vector2 position, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            return label;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.42f, 0.75f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go.transform, "Text", label, 20, Vector2.zero, rectSize);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static void AddSceneToBuildSettingsIfNeeded(string scenePath, bool first)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (first)
                scenes.Insert(0, entry);
            else
                scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
