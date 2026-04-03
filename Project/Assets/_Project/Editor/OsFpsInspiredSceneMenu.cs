#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.EditorTools
{
    public static class OsFpsInspiredSceneMenu
    {
        const string ScenePath = "Assets/Scenes/OsFpsInspiredDesktop.unity";

        [MenuItem("VR Project/Scenes/Create OsFPS-Inspired Desktop Sandbox")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(15f, 1f, 15f);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
                floor.GetComponent<MeshRenderer>().sharedMaterial = new Material(lit) { color = new Color(0.22f, 0.24f, 0.26f) };

            var systems = new GameObject("Systems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<SuperhotGameplayDriver>();

            var player = new GameObject("OsFps_Player");
            player.transform.position = new Vector3(0f, 0f, -4f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.875f, 0f);

            var motor = player.AddComponent<OsFpsInspiredPlayerMotor>();
            var weapon = player.AddComponent<OsFpsInspiredWeapon>();

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("_cameraTransform").objectReferenceValue = camGo.transform;
            motorSo.ApplyModifiedPropertiesWithoutUndo();

            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("_camera").objectReferenceValue = cam;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("AmmoText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 24f);
            rt.sizeDelta = new Vector2(280f, 48f);
            var ammoText = textGo.AddComponent<Text>();
            ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (ammoText.font == null)
                ammoText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            ammoText.fontSize = 20;
            ammoText.color = Color.white;
            ammoText.alignment = TextAnchor.LowerRight;
            ammoText.text = "24 / 24";

            canvasGo.transform.SetParent(player.transform, false);

            var hud = player.AddComponent<OsFpsInspiredHud>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_weapon").objectReferenceValue = weapon;
            hudSo.FindProperty("_ammoText").objectReferenceValue = ammoText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            BuildTargets(lit);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            AddSceneToBuildSettingsIfNeeded(ScenePath);
            Debug.Log("[VR Project] OsFPS-inspired desktop sandbox saved to " + ScenePath +
                      ". OsFPS source is under Assets/ThirdParty (Unity 2018; see ThirdParty/README-OsFPS.md). Controls: WASD, mouse, Space, LMB fire, R reload, Esc cursor.");
        }

        static void BuildTargets(Shader lit)
        {
            var root = new GameObject("Targets");
            var positions = new[]
            {
                new Vector3(4f, 0.9f, 6f),
                new Vector3(-3f, 0.9f, 8f),
                new Vector3(0f, 0.9f, 12f),
                new Vector3(6f, 0.9f, 3f)
            };
            for (var i = 0; i < positions.Length; i++)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = "Target_" + (i + 1);
                cap.transform.SetParent(root.transform, false);
                cap.transform.position = positions[i];
                cap.AddComponent<OsFpsInspiredDamageable>();
                if (lit != null)
                    cap.GetComponent<MeshRenderer>().sharedMaterial = new Material(lit) { color = new Color(0.75f, 0.18f, 0.15f) };
            }
        }

        static void AddSceneToBuildSettingsIfNeeded(string scenePath)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath)))
                return;
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
            {
                if (s.path == scenePath)
                    return;
            }
            var list = new List<EditorBuildSettingsScene>(existing) { new EditorBuildSettingsScene(scenePath, true) };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
