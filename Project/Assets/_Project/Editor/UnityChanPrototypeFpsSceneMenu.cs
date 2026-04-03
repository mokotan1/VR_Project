#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.EditorTools
{
    public static class UnityChanPrototypeFpsSceneMenu
    {
        const string ScenePath = "Assets/Scenes/UnityChanPrototypeFps.unity";
        const string UnityChanPrefabPath = "Assets/unity-chan!/Unity-chan! Model/Prefabs/for Locomotion/unitychan_dynamic_locomotion.prefab";

        [MenuItem("VR Project/Scenes/Create Unity-Chan Prototype FPS")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var navRoot = new GameObject("NavWorld");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(navRoot.transform, false);
            floor.transform.localScale = new Vector3(8f, 1f, 8f);
            MarkNavigationStatic(floor);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
                floor.GetComponent<MeshRenderer>().sharedMaterial = new Material(lit) { color = new Color(0.2f, 0.22f, 0.25f) };

            BuildCoverArena(navRoot.transform, lit);
            BuildParkourJump(navRoot.transform, lit);

            var surface = navRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            var systems = new GameObject("Systems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<SuperhotGameplayDriver>();

            var covers = BuildCoverPoints();
            covers.transform.SetParent(navRoot.transform, false);

            var coverList = new List<Transform>();
            foreach (Transform t in covers.GetComponentsInChildren<Transform>(true))
            {
                if (t != covers.transform && t.name.StartsWith("CoverPoint", StringComparison.Ordinal))
                    coverList.Add(t);
            }

            SpawnEnemies(coverList.ToArray(), lit);

            var player = BuildUnityChanPlayer(lit);
            player.transform.position = new Vector3(0f, 0f, -6f);

            WireHud(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            AddToBuildSettings(ScenePath);

            Debug.Log("[VR Project] Unity-Chan prototype FPS saved to " + ScenePath +
                      ". Add Tags Player and Enemy if missing. NavMesh on NavWorld.");
        }

        static void MarkNavigationStatic(GameObject go)
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.NavigationStatic);
        }

        static void BuildCoverArena(Transform parent, Shader litShader)
        {
            void Wall(string wallName, Vector3 pos, Vector3 scale)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = wallName;
                w.transform.SetParent(parent, false);
                w.transform.position = pos;
                w.transform.localScale = scale;
                MarkNavigationStatic(w);
                if (litShader != null)
                    w.GetComponent<MeshRenderer>().sharedMaterial = new Material(litShader) { color = new Color(0.35f, 0.32f, 0.3f) };
            }

            Wall("CoverWall_A", new Vector3(12f, 1.25f, 8f), new Vector3(1f, 2.5f, 6f));
            Wall("CoverWall_B", new Vector3(-10f, 1.25f, 10f), new Vector3(1f, 2.5f, 5f));
            Wall("LowWall_C", new Vector3(4f, 0.6f, 14f), new Vector3(4f, 1.2f, 1f));
        }

        static void BuildParkourJump(Transform parent, Shader litShader)
        {
            var low = GameObject.CreatePrimitive(PrimitiveType.Cube);
            low.name = "ParkourLow";
            low.transform.SetParent(parent, false);
            low.transform.position = new Vector3(-4f, 0.75f, 6f);
            low.transform.localScale = new Vector3(2.2f, 1.5f, 1.2f);
            MarkNavigationStatic(low);
            if (litShader != null)
                low.GetComponent<MeshRenderer>().sharedMaterial = new Material(litShader) { color = new Color(0.28f, 0.4f, 0.32f) };

            var high = GameObject.CreatePrimitive(PrimitiveType.Cube);
            high.name = "ParkourPlatform";
            high.transform.SetParent(parent, false);
            high.transform.position = new Vector3(-4f, 2.1f, 10.5f);
            high.transform.localScale = new Vector3(3f, 0.35f, 3f);
            MarkNavigationStatic(high);
            if (litShader != null)
                high.GetComponent<MeshRenderer>().sharedMaterial = new Material(litShader) { color = new Color(0.25f, 0.35f, 0.45f) };

            var linkGo = new GameObject("NavMeshLink_Parkour");
            linkGo.transform.SetParent(parent, false);
            linkGo.transform.position = new Vector3(-4f, 0f, 7.2f);
            var link = linkGo.AddComponent<NavMeshLink>();
            link.startPoint = new Vector3(0f, 0.5f, 0f);
            link.endPoint = new Vector3(0f, 2.05f, 3.2f);
            link.width = 2f;
            link.bidirectional = true;
        }

        static GameObject BuildCoverPoints()
        {
            var root = new GameObject("CoverPoints");
            void Pt(string n, Vector3 p)
            {
                var go = new GameObject(n);
                go.transform.SetParent(root.transform, false);
                go.transform.position = p;
            }

            Pt("CoverPoint_A", new Vector3(9f, 0f, 6f));
            Pt("CoverPoint_B", new Vector3(9f, 0f, 10f));
            Pt("CoverPoint_C", new Vector3(-8f, 0f, 8f));
            Pt("CoverPoint_D", new Vector3(-8f, 0f, 12f));
            Pt("CoverPoint_E", new Vector3(2f, 0f, 12f));
            return root;
        }

        static void SpawnEnemies(Transform[] covers, Shader litShader)
        {
            var spawns = new[]
            {
                new Vector3(14f, 0f, 4f),
                new Vector3(-12f, 0f, 4f),
                new Vector3(8f, 0f, 16f),
                new Vector3(-6f, 0f, 18f)
            };

            var enemyLayer = LayerMask.NameToLayer("Enemy");
            foreach (var sp in spawns)
            {
                var root = new GameObject("Enemy_Agent");
                root.transform.position = sp + Vector3.up * 0.05f;
                TrySetTag(root, "Enemy");

                var agent = root.AddComponent<NavMeshAgent>();
                agent.height = 1.8f;
                agent.radius = 0.35f;
                agent.speed = 3.6f;
                agent.acceleration = 20f;
                agent.autoTraverseOffMeshLink = true;

                var col = root.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.35f;
                col.center = new Vector3(0f, 0.9f, 0f);

                root.AddComponent<OsFpsInspiredDamageable>();

                var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                vis.name = "Body";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                UnityEngine.Object.DestroyImmediate(vis.GetComponent<CapsuleCollider>());
                if (litShader != null)
                    vis.GetComponent<MeshRenderer>().sharedMaterial = new Material(litShader) { color = new Color(0.72f, 0.12f, 0.1f) };

                var muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 1.45f, 0.35f);

                var ai = root.AddComponent<PrototypeEnemyAi>();
                var so = new SerializedObject(ai);
                so.FindProperty("_muzzle").objectReferenceValue = muzzle.transform;
                so.ApplyModifiedPropertiesWithoutUndo();

                ai.SetCoverPoints(covers);

                if (enemyLayer >= 0)
                    SetLayerRecursively(root, enemyLayer);
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        static GameObject BuildUnityChanPlayer(Shader litShader)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityChanPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[VR Project] Unity-Chan prefab missing at: " + UnityChanPrefabPath);
                return new GameObject("MissingUnityChan");
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "UnityChan_Player";
            TrySetTag(player, "Player");

            foreach (var rb in player.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(rb);

            DestroyUnityChanMotorComponents(player);

            var oldCc = player.GetComponent<CharacterController>();
            if (oldCc != null)
                UnityEngine.Object.DestroyImmediate(oldCc);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.35f;
            cc.radius = 0.22f;
            cc.center = new Vector3(0f, 0.68f, 0f);

            var camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(player.transform, false);
            camPivot.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(camPivot.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var motor = player.AddComponent<PrototypeThirdPersonPlayer>();
            var mSo = new SerializedObject(motor);
            mSo.FindProperty("_camera").objectReferenceValue = cam;
            mSo.FindProperty("_cameraPivot").objectReferenceValue = camPivot.transform;
            mSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeMantleProbe>();
            player.AddComponent<UnityChanLocomotionAnimatorBridge>();
            var aimTwist = player.AddComponent<PrototypeAimSpineTwist>();
            var aimSo = new SerializedObject(aimTwist);
            aimSo.FindProperty("_camera").objectReferenceValue = cam;
            aimSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeFpsPlayerHealth>();
            var weapon = player.AddComponent<OsFpsInspiredWeapon>();
            var wSo = new SerializedObject(weapon);
            wSo.FindProperty("_camera").objectReferenceValue = cam;
            wSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeFpsPlayerDeathHandler>();

            var hand = FindHandBone(player.transform);
            if (hand != null && litShader != null)
            {
                var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gun.name = "ToyGun";
                gun.transform.SetParent(hand, false);
                gun.transform.localPosition = new Vector3(0.05f, 0.02f, 0.02f);
                gun.transform.localScale = new Vector3(0.08f, 0.06f, 0.22f);
                UnityEngine.Object.DestroyImmediate(gun.GetComponent<BoxCollider>());
                gun.GetComponent<MeshRenderer>().sharedMaterial = new Material(litShader) { color = new Color(0.15f, 0.15f, 0.18f) };
            }

            UnityChanUrpMaterialRemapUtility.RemapRenderersUnder(player);

            return player;
        }


        /// <summary>
        /// VRProject.Editor cannot reference Assembly-CSharp (Unity-Chan scripts); strip by type name.
        /// </summary>
        static void DestroyUnityChanMotorComponents(GameObject root)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                    continue;
                if (mb.GetType().Name == "UnityChanControlScriptWithRgidBody")
                    UnityEngine.Object.DestroyImmediate(mb);
            }
        }

        static Transform FindHandBone(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if ((n.Contains("hand") && (n.Contains("r") || n.Contains("right"))) || n.Contains("hand_r") || n.Contains("right_hand"))
                    return t;
            }

            return null;
        }

        static void WireHud(GameObject player)
        {
            var canvasGo = new GameObject("HUD");
            canvasGo.transform.SetParent(player.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("Status");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(-40f, 80f);
            var txt = textGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAnchor.UpperCenter;

            var hud = player.AddComponent<PrototypeFpsHud>();
            var hSo = new SerializedObject(hud);
            hSo.FindProperty("_weapon").objectReferenceValue = player.GetComponent<OsFpsInspiredWeapon>();
            hSo.FindProperty("_health").objectReferenceValue = player.GetComponent<PrototypeFpsPlayerHealth>();
            hSo.FindProperty("_statusText").objectReferenceValue = txt;
            hSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TrySetTag(GameObject go, string tag)
        {
            try
            {
                go.tag = tag;
            }
            catch
            {
                Debug.LogWarning("[VR Project] Tag \"" + tag + "\" is not defined. Add it in Tag Manager.");
            }
        }

        static void AddToBuildSettings(string scenePath)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath)))
                return;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path == scenePath)
                    return;
            }

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
