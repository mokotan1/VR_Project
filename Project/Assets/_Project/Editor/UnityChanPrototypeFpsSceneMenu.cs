#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DyrdaDev.FirstPersonController;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Gameplay;
using VRProject.Presentation.OsFpsInspired;
using VRProject.Presentation.PrototypeFps;

namespace VRProject.EditorTools
{
    public static class UnityChanPrototypeFpsSceneMenu
    {
        /// <summary>Inside <c>Assets/Scenes/UnityChanPrototypeFps/</c> so NavMesh and scene stay together (avoid only opening the folder).</summary>
        const string ScenePath = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps.unity";

        /// <summary>HK416 parented under rig hand (<see cref="WeaponSocketTransformName"/>); camera uses over-shoulder so the grip reads on the skeleton.</summary>
        const string ScenePathHandHeld = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps_HandHeld.unity";

        /// <summary>Same layout as <see cref="ScenePath"/> but locomotion uses dyrdadev First-Person Controller + <see cref="DyrdaFirstPersonMotorAdapter"/> (no mantle).</summary>
        const string ScenePathDyrdaFpc = "Assets/Scenes/UnityChanPrototypeFps/UnityChanPrototypeFps_DyrdaFpc.unity";

        const string WeaponSocketTransformName = "WeaponSocket";

        const string LegacyScenePathAtScenesRoot = "Assets/Scenes/UnityChanPrototypeFps.unity";
        const string UnityChanPrefabPath = "Assets/unity-chan!/Unity-chan! Model/Prefabs/for Locomotion/unitychan_dynamic_locomotion.prefab";
        const string Hk416PrefabPath = "Assets/Gece Studio/Free Rifle - HK416/Prefab/Rifle_HK416.prefab";
        const string BulletPackPrefab01Path = "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_01.prefab";
        const string BulletPackPrefab02Path = "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_02.prefab";
        const string BulletPackPrefab03Path = "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_03.prefab";
        const string BulletPackPrefab010Path = "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_010.prefab";

        /// <summary>Bullets Pack meshes are small in source assets; keep scale modest so the pile reads as table props.</summary>
        const float BulletPackDecorationScale = 2.5f;

        [MenuItem("VR Project/Scenes/Create Unity-Chan Prototype FPS")]
        public static void CreateScene()
        {
            EnsureProjectTagsExist("Player", "Enemy");

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

            var lit = ResolveUrpLitShader();
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

            var player = BuildUnityChanPlayer(lit, handHeldWorldGun: false);
            player.transform.position = new Vector3(0f, 0f, -6f);

            SpawnWeaponPickup(navRoot.transform, lit, new Vector3(2.2f, 0f, -5.5f));
            SpawnBulletPackDecorations(navRoot.transform, new Vector3(2.55f, 0f, -5.25f));

            WireHud(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureAssetDirectoryExists(ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[VR Project] SaveScene failed — scene was not written. dataPath=" +
                               UnityEngine.Application.dataPath + " path=\"" + ScenePath + "\"");
                return;
            }

            AssetDatabase.Refresh();
            RemoveSceneFromBuildSettingsIfPresent(LegacyScenePathAtScenesRoot);
            AddToBuildSettings(ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("[VR Project] Unity-Chan prototype FPS saved and opened: " + ScenePath +
                      " (project dataPath: " + UnityEngine.Application.dataPath + "). " +
                      "If nothing changed, confirm Unity is using this project folder. Add Tags Player and Enemy if missing.");
        }

        [MenuItem("VR Project/Scenes/Create Unity-Chan Prototype FPS (Hand-held HK416 on rig)")]
        public static void CreateSceneHandHeldRifle()
        {
            EnsureProjectTagsExist("Player", "Enemy");

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

            var lit = ResolveUrpLitShader();
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

            var player = BuildUnityChanPlayer(lit, handHeldWorldGun: true);
            player.transform.position = new Vector3(0f, 0f, -6f);

            SpawnWeaponPickup(navRoot.transform, lit, new Vector3(2.2f, 0f, -5.5f));
            SpawnBulletPackDecorations(navRoot.transform, new Vector3(2.55f, 0f, -5.25f));

            WireHud(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureAssetDirectoryExists(ScenePathHandHeld);
            if (!EditorSceneManager.SaveScene(scene, ScenePathHandHeld))
            {
                Debug.LogError("[VR Project] SaveScene failed — scene was not written. dataPath=" +
                               UnityEngine.Application.dataPath + " path=\"" + ScenePathHandHeld + "\"");
                return;
            }

            AssetDatabase.Refresh();
            RemoveSceneFromBuildSettingsIfPresent(LegacyScenePathAtScenesRoot);
            AddToBuildSettings(ScenePathHandHeld);
            EditorSceneManager.OpenScene(ScenePathHandHeld, OpenSceneMode.Single);

            Debug.Log("[VR Project] Unity-Chan prototype FPS (hand-held HK416) saved and opened: " + ScenePathHandHeld +
                      ". Over-shoulder camera + rifle under " + WeaponSocketTransformName + " on the rig hand.");
        }

        [MenuItem("VR Project/Scenes/Create Unity-Chan Prototype FPS (Dyrda FPC)")]
        public static void CreateSceneDyrdaFpc()
        {
            EnsureProjectTagsExist("Player", "Enemy");

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

            var lit = ResolveUrpLitShader();
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

            var player = BuildUnityChanPlayerDyrdaFpc(lit);
            player.transform.position = new Vector3(0f, 0f, -6f);

            SpawnWeaponPickup(navRoot.transform, lit, new Vector3(2.2f, 0f, -5.5f));
            SpawnBulletPackDecorations(navRoot.transform, new Vector3(2.55f, 0f, -5.25f));

            WireHud(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureAssetDirectoryExists(ScenePathDyrdaFpc);
            if (!EditorSceneManager.SaveScene(scene, ScenePathDyrdaFpc))
            {
                Debug.LogError("[VR Project] SaveScene failed — scene was not written. dataPath=" +
                               UnityEngine.Application.dataPath + " path=\"" + ScenePathDyrdaFpc + "\"");
                return;
            }

            AssetDatabase.Refresh();
            RemoveSceneFromBuildSettingsIfPresent(LegacyScenePathAtScenesRoot);
            AddToBuildSettings(ScenePathDyrdaFpc);
            EditorSceneManager.OpenScene(ScenePathDyrdaFpc, OpenSceneMode.Single);

            Debug.Log("[VR Project] Unity-Chan prototype FPS (Dyrda FPC) saved and opened: " + ScenePathDyrdaFpc +
                      ". Locomotion: dyrdadev First-Person Controller; mantle probe omitted. Requires Input System + UniRx per package README.");
        }

        static void EnsureAssetDirectoryExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return;
            var relativeUnderAssets = assetPath.Substring("Assets/".Length);
            var directoryUnderAssets = Path.GetDirectoryName(relativeUnderAssets);
            if (string.IsNullOrEmpty(directoryUnderAssets))
                return;
            var absDir = Path.Combine(UnityEngine.Application.dataPath, directoryUnderAssets);
            if (!Directory.Exists(absDir))
                Directory.CreateDirectory(absDir);
        }

        static void RemoveSceneFromBuildSettingsIfPresent(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            var changed = false;
            var list = new List<EditorBuildSettingsScene>();
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    changed = true;
                    continue;
                }

                list.Add(s);
            }

            if (changed)
                EditorBuildSettings.scenes = list.ToArray();
        }

        static Shader ResolveUrpLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                   ?? Shader.Find("HDRP/Lit")
                   ?? Shader.Find("Standard");
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

                root.SetActive(false);
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        static GameObject BuildUnityChanPlayer(Shader litShader, bool handHeldWorldGun)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityChanPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[VR Project] Unity-Chan prefab missing at: " + UnityChanPrefabPath);
                return new GameObject("MissingUnityChan");
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "UnityChan_Player";
            // Unpack so SkinnedMeshRenderer material overrides persist when the scene is saved.
            // Otherwise nested prefab instances often drop non-asset Material instances on SaveScene.
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            TrySetTag(player, "Player");

            foreach (var rb in player.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(rb);

            DestroyUnityChanMotorComponents(player);
            // Prefab was previously saved with CameraPivot/HUD/FPS scripts; strip so we add a single clean rig.
            StripBakedPrototypeRigFromPlayerRoot(player);

            var oldCc = player.GetComponent<CharacterController>();
            if (oldCc != null)
                UnityEngine.Object.DestroyImmediate(oldCc);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.35f;
            cc.radius = 0.22f;
            cc.center = new Vector3(0f, 0.68f, 0f);

            var camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(player.transform, false);
            camPivot.transform.localPosition = new Vector3(0f, 1.58f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(camPivot.transform, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            player.AddComponent<PrototypeFirstPersonCosmeticCull>();

            var motor = player.AddComponent<PrototypeThirdPersonPlayer>();
            var mSo = new SerializedObject(motor);
            mSo.FindProperty("_camera").objectReferenceValue = cam;
            mSo.FindProperty("_cameraPivot").objectReferenceValue = camPivot.transform;
            var osc = mSo.FindProperty("_overShoulderCamera");
            if (osc != null)
                osc.boolValue = handHeldWorldGun;
            var slo = mSo.FindProperty("_shoulderCameraLocalOffset");
            if (slo != null)
                slo.vector3Value = new Vector3(0.38f, 1.42f, -2.35f);
            mSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeMantleProbe>();
            AttachSharedUnityChanFpsGameplay(player, cam, litShader, fpsViewmodelUnderCamera: !handHeldWorldGun);

            return player;
        }

        static GameObject BuildUnityChanPlayerDyrdaFpc(Shader litShader)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityChanPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[VR Project] Unity-Chan prefab missing at: " + UnityChanPrefabPath);
                return new GameObject("MissingUnityChan");
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "UnityChan_Player";
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            TrySetTag(player, "Player");

            foreach (var rb in player.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(rb);

            DestroyUnityChanMotorComponents(player);
            StripBakedPrototypeRigFromPlayerRoot(player);

            var oldCc = player.GetComponent<CharacterController>();
            if (oldCc != null)
                UnityEngine.Object.DestroyImmediate(oldCc);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.35f;
            cc.radius = 0.22f;
            cc.center = new Vector3(0f, 0.68f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.58f, 0f);
            camGo.transform.localRotation = Quaternion.identity;
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            player.AddComponent<PrototypeFirstPersonCosmeticCull>();

            var dyrdaInput = player.AddComponent<InputActionBasedFirstPersonControllerInput>();
            var dyrdaFpc = player.AddComponent<FirstPersonController>();
            var fpcSo = new SerializedObject(dyrdaFpc);
            var inputProp = fpcSo.FindProperty("firstPersonControllerInput")
                          ?? fpcSo.FindProperty("_firstPersonControllerInput");
            if (inputProp != null)
                inputProp.objectReferenceValue = dyrdaInput;
            else
                Debug.LogWarning("[VR Project] Could not bind FirstPersonController input field via SerializedObject; assign in Inspector if needed.");
            fpcSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<DyrdaFirstPersonMotorAdapter>();
            AttachSharedUnityChanFpsGameplay(player, cam, litShader, fpsViewmodelUnderCamera: true);

            return player;
        }

        static void AttachSharedUnityChanFpsGameplay(GameObject player, Camera cam, Shader litShader, bool fpsViewmodelUnderCamera)
        {
            player.AddComponent<UnityChanLocomotionAnimatorBridge>();
            var aimTwist = player.AddComponent<PrototypeAimSpineTwist>();
            var aimSo = new SerializedObject(aimTwist);
            aimSo.FindProperty("_camera").objectReferenceValue = cam;
            aimSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeFpsPlayerHealth>();
            var weapon = player.AddComponent<OsFpsInspiredWeapon>();
            var wSo = new SerializedObject(weapon);
            wSo.FindProperty("_camera").objectReferenceValue = cam;
            // false면 WeaponUpper(Kevin 라이플 상체)가 안 켜져 베이스 로코만 보임 → 변경이 안 보인 것처럼 느껴짐. 픽업 플로우만 쓰면 false로.
            wSo.FindProperty("_startEquipped").boolValue = true;
            wSo.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PrototypeFpsPlayerDeathHandler>();

            GameObject gunVisual = null;
            Transform muzzleTransform = null;
            GameObject viewmodelRootGo = null;

            Transform gunParent;
            Vector3 gunLocalPos;
            Quaternion gunLocalRot;
            if (fpsViewmodelUnderCamera)
            {
                viewmodelRootGo = new GameObject("FpsWeaponViewmodel");
                viewmodelRootGo.transform.SetParent(cam.transform, false);
                viewmodelRootGo.transform.localPosition = Vector3.zero;
                viewmodelRootGo.transform.localRotation = Quaternion.identity;
                viewmodelRootGo.transform.localScale = Vector3.one;
                gunParent = viewmodelRootGo.transform;
                ComputeFpsViewmodelGunLayout(player, out gunLocalPos, out gunLocalRot);
            }
            else
            {
                var hand = FindHandBone(player.transform);
                gunParent = hand != null ? GetOrCreateWeaponSocket(hand) : player.transform;
                if (hand != null)
                    ComputeHandSocketGunLayout(player, out gunLocalPos, out gunLocalRot);
                else
                {
                    gunLocalPos = new Vector3(0.2f, 1.15f, 0.25f);
                    gunLocalRot = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            gunVisual = TryInstantiatePrefabUnder(Hk416PrefabPath, gunParent);
            if (gunVisual != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(gunVisual))
                    PrefabUtility.UnpackPrefabInstance(gunVisual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                gunVisual.name = "HandGun_HK416";
                gunVisual.transform.localPosition = gunLocalPos;
                gunVisual.transform.localRotation = gunLocalRot;
                gunVisual.transform.localScale = Vector3.one;
                StripCollidersUnder(gunVisual.transform);
                UnityChanUrpMaterialRemapUtility.RemapRenderersUnder(gunVisual);
                gunVisual.SetActive(false);
                muzzleTransform = CreateWeaponFirePointUnderGun(gunVisual);
            }
            else if (litShader != null)
            {
                gunVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gunVisual.name = "ToyGun";
                gunVisual.transform.SetParent(gunParent, false);
                gunVisual.transform.localPosition = gunLocalPos;
                gunVisual.transform.localRotation = gunLocalRot;
                var toyMul = GunScaleMulFromCharacter(player);
                gunVisual.transform.localScale = fpsViewmodelUnderCamera
                    ? new Vector3(0.07f, 0.05f, 0.28f) * toyMul
                    : new Vector3(0.08f, 0.06f, 0.22f) * toyMul;
                UnityEngine.Object.DestroyImmediate(gunVisual.GetComponent<BoxCollider>());
                gunVisual.GetComponent<MeshRenderer>().sharedMaterial =
                    new Material(litShader) { color = new Color(0.15f, 0.15f, 0.18f) };
                gunVisual.SetActive(false);
                muzzleTransform = CreateToyGunFirePoint(gunVisual.transform);
            }

            wSo = new SerializedObject(weapon);
            wSo.FindProperty("_handGunVisual").objectReferenceValue = gunVisual;
            if (muzzleTransform != null)
                wSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzleTransform;
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPackPrefab01Path);
            if (bulletPrefab != null)
                wSo.FindProperty("_bulletVisualPrefab").objectReferenceValue = bulletPrefab;
            var bulletScale = wSo.FindProperty("_bulletVisualScale");
            if (bulletScale != null)
                bulletScale.floatValue = 56f;
            var bulletEuler = wSo.FindProperty("_bulletVisualEulerOffset");
            if (bulletEuler != null)
                bulletEuler.vector3Value = new Vector3(90f, 0f, 0f);
            wSo.ApplyModifiedPropertiesWithoutUndo();

            if (fpsViewmodelUnderCamera && viewmodelRootGo != null)
                FpsViewmodelRenderingSetup.Apply(cam, viewmodelRootGo);

            UnityChanUrpMaterialRemapUtility.RemapRenderersUnder(player);
        }

        /// <summary>프로토타입 씬의 CC 높이(기본 1.35m) 대비 비율로 뷰모델/손 총 스케일을 맞춥니다.</summary>
        const float ReferenceCharacterControllerHeight = 1.35f;

        static float GunScaleMulFromCharacter(GameObject player)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc == null)
                return 1f;
            return Mathf.Clamp(cc.height / ReferenceCharacterControllerHeight, 0.82f, 1.28f);
        }

        static void ComputeFpsViewmodelGunLayout(GameObject player, out Vector3 localPos, out Quaternion localRot)
        {
            var mul = GunScaleMulFromCharacter(player);
            localPos = new Vector3(0.33f, -0.23f, 0.52f) * mul;
            localRot = Quaternion.Euler(2f, 94f, -1.5f);
        }

        /// <summary>
        /// Local pose of HK416 under <see cref="WeaponSocketTransformName"/>.
        /// HK416 루트는 통상 총신 +Z; 손 본 ARpose와 맞추기 위해 추가 Y=90°는 총이 옆(+X)을 보므로 쓰지 않음. 미세 조정은 씬의 <c>WeaponSocket</c>.
        /// </summary>
        static void ComputeHandSocketGunLayout(GameObject player, out Vector3 localPos, out Quaternion localRot)
        {
            var mul = GunScaleMulFromCharacter(player);
            localPos = new Vector3(0.03f, 0.038f, 0.018f) * mul;
            localRot = Quaternion.identity;
        }

        static Transform GetOrCreateWeaponSocket(Transform handBone)
        {
            var existing = handBone.Find(WeaponSocketTransformName);
            if (existing != null)
                return existing;

            var go = new GameObject(WeaponSocketTransformName);
            go.transform.SetParent(handBone, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        static void StripBakedPrototypeRigFromPlayerRoot(GameObject player)
        {
            var t = player.transform;
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                var ch = t.GetChild(i);
                if (ch.name == "CameraPivot" || ch.name == "HUD" || ch.name == "Main Camera" ||
                    ch.name == "FpsWeaponViewmodel")
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
            }

            foreach (var tr in player.GetComponentsInChildren<Transform>(true))
            {
                if (tr == t)
                    continue;
                var n = tr.name;
                if (n == "HandGun_HK416" || n == "ToyGun" || n == WeaponSocketTransformName)
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
            }

            foreach (var compType in PrototypeComponentsOnPlayerRoot)
            {
                foreach (var c in player.GetComponents(compType))
                {
                    if (c != null)
                        UnityEngine.Object.DestroyImmediate(c);
                }
            }
        }

        /// <summary>Destroy order: dependents first; <see cref="PrototypeThirdPersonPlayer"/> and CC are removed after this list.</summary>
        static readonly Type[] PrototypeComponentsOnPlayerRoot =
        {
            typeof(PrototypeFpsHud),
            typeof(PrototypeFpsPlayerDeathHandler),
            typeof(OsFpsInspiredWeapon),
            typeof(PrototypeFpsPlayerHealth),
            typeof(PrototypeAimSpineTwist),
            typeof(PrototypeMantleProbe),
            typeof(UnityChanLocomotionAnimatorBridge),
            typeof(PrototypeFirstPersonCosmeticCull),
            typeof(DyrdaFirstPersonMotorAdapter),
            typeof(FirstPersonController),
            typeof(InputActionBasedFirstPersonControllerInput),
            typeof(PrototypeThirdPersonPlayer),
        };

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

        static Transform CreateWeaponFirePointUnderGun(GameObject gunVisual)
        {
            var gunRoot = gunVisual.transform;
            foreach (var tr in gunRoot.GetComponentsInChildren<Transform>(true))
            {
                var n = tr.name.ToLowerInvariant();
                if (n.Contains("muzzle") || n.Contains("firepoint") || n.Contains("fire_point") || n.Contains("barrel_tip"))
                    return tr;
            }

            var renderers = gunVisual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                var fp = new GameObject("WeaponFirePoint");
                fp.transform.SetParent(gunRoot, false);
                fp.transform.localPosition = new Vector3(0f, 0f, 0.35f);
                fp.transform.localRotation = Quaternion.identity;
                return fp.transform;
            }

            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            var forward = gunRoot.forward;
            var c = b.center;
            var e = b.extents;
            var corners = new Vector3[8];
            var idx = 0;
            for (var x = -1f; x <= 1f; x += 2f)
            for (var y = -1f; y <= 1f; y += 2f)
            for (var z = -1f; z <= 1f; z += 2f)
                corners[idx++] = c + new Vector3(e.x * x, e.y * y, e.z * z);

            var best = corners[0];
            var bestDot = float.MinValue;
            foreach (var p in corners)
            {
                var d = Vector3.Dot(p - b.center, forward);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = p;
                }
            }

            var go = new GameObject("WeaponFirePoint");
            go.transform.position = best;
            go.transform.rotation = Quaternion.LookRotation(forward, gunRoot.up);
            go.transform.SetParent(gunRoot, true);
            return go.transform;
        }

        static Transform CreateToyGunFirePoint(Transform gunRoot)
        {
            var fp = new GameObject("WeaponFirePoint");
            fp.transform.SetParent(gunRoot, false);
            fp.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            fp.transform.localRotation = Quaternion.identity;
            return fp.transform;
        }

        static readonly string[] PreferredRightHandBoneNames =
        {
            "Character1_RightHand",
            "RightHand",
            "hand_r",
            "Hand_R",
            "mixamorig:RightHand",
        };

        static Transform FindHandBone(Transform root)
        {
            foreach (var preferred in PreferredRightHandBoneNames)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == preferred)
                        return t;
                }
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n == "right_hand" || n.Contains("hand_r") || n.EndsWith("_r_hand", StringComparison.Ordinal))
                    return t;
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (!n.Contains("hand"))
                    continue;
                if (n.Contains("finger") || n.Contains("thumb") || n.Contains("index") || n.Contains("middle") ||
                    n.Contains("ring") || n.Contains("pinky") || n.Contains("little"))
                    continue;
                if (n.Contains("r") || n.Contains("right"))
                    return t;
            }

            return null;
        }

        static (RectTransform rt, Image img) CreateBrCrossLine(
            Transform parent,
            string name,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lineRt = go.AddComponent<RectTransform>();
            lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.pivot = pivot;
            lineRt.anchoredPosition = anchoredPosition;
            lineRt.sizeDelta = sizeDelta;
            var lineImg = go.AddComponent<Image>();
            lineImg.color = Color.white;
            lineImg.raycastTarget = false;
            return (lineRt, lineImg);
        }

        /// <summary>PUBG-style dynamic crosshair (dot + four bars); spread driven at runtime by <see cref="BattleRoyaleStyleCrosshair"/>.</summary>
        static GameObject BuildBattleRoyaleCrosshair(Transform canvasTransform, GameObject player)
        {
            var root = new GameObject("Crosshair_BattleRoyale");
            root.transform.SetParent(canvasTransform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = new Vector2(280f, 280f);

            var br = root.AddComponent<BattleRoyaleStyleCrosshair>();

            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(root.transform, false);
            var dotRt = dotGo.AddComponent<RectTransform>();
            dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = Vector2.zero;
            dotRt.sizeDelta = new Vector2(4f, 4f);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = Color.white;
            dotImg.raycastTarget = false;

            var (lineTopRt, lineTopImg) = CreateBrCrossLine(
                root.transform, "LineTop", new Vector2(0.5f, 0f), new Vector2(2f, 9f), Vector2.zero);
            var (lineBottomRt, lineBottomImg) = CreateBrCrossLine(
                root.transform, "LineBottom", new Vector2(0.5f, 1f), new Vector2(2f, 9f), Vector2.zero);
            var (lineLeftRt, lineLeftImg) = CreateBrCrossLine(
                root.transform, "LineLeft", new Vector2(1f, 0.5f), new Vector2(9f, 2f), Vector2.zero);
            var (lineRightRt, lineRightImg) = CreateBrCrossLine(
                root.transform, "LineRight", new Vector2(0f, 0.5f), new Vector2(9f, 2f), Vector2.zero);

            var so = new SerializedObject(br);
            so.FindProperty("_locomotionMotor").objectReferenceValue =
                UnityChanLocomotionMotorResolver.ResolveMotorBehaviourOn(player);
            so.FindProperty("_weapon").objectReferenceValue = player.GetComponent<OsFpsInspiredWeapon>();
            so.FindProperty("_dot").objectReferenceValue = dotRt;
            so.FindProperty("_lineTop").objectReferenceValue = lineTopRt;
            so.FindProperty("_lineBottom").objectReferenceValue = lineBottomRt;
            so.FindProperty("_lineLeft").objectReferenceValue = lineLeftRt;
            so.FindProperty("_lineRight").objectReferenceValue = lineRightRt;
            so.FindProperty("_dotImage").objectReferenceValue = dotImg;
            so.FindProperty("_lineTopImage").objectReferenceValue = lineTopImg;
            so.FindProperty("_lineBottomImage").objectReferenceValue = lineBottomImg;
            so.FindProperty("_lineLeftImage").objectReferenceValue = lineLeftImg;
            so.FindProperty("_lineRightImage").objectReferenceValue = lineRightImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        static GameObject TryInstantiatePrefabUnder(string assetPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                return null;
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        static void StripCollidersUnder(Transform root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);
        }

        static void SpawnWeaponPickup(Transform parent, Shader litShader, Vector3 worldPosition)
        {
            var pickupRoot = new GameObject("WeaponPickup_HK416");
            pickupRoot.transform.SetParent(parent, false);
            pickupRoot.transform.position = worldPosition;
            var col = pickupRoot.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.95f;
            pickupRoot.AddComponent<PrototypeFpsWeaponPickup>();

            var hk416 = TryInstantiatePrefabUnder(Hk416PrefabPath, pickupRoot.transform);
            if (hk416 != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(hk416))
                    PrefabUtility.UnpackPrefabInstance(hk416, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                hk416.name = "PickupVisual_HK416";
                // Author demo uses ~0.1m lift and identity rotation; X=90 buried the mesh under the floor.
                hk416.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                hk416.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                hk416.transform.localScale = Vector3.one;
                StripCollidersUnder(hk416.transform);
            }
            else if (litShader != null)
            {
                Debug.LogWarning("[VR Project] HK416 prefab missing at: " + Hk416PrefabPath + ". Using placeholder cube.");
                var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vis.name = "PickupVisual";
                vis.transform.SetParent(pickupRoot.transform, false);
                vis.transform.localPosition = Vector3.up * 0.12f;
                vis.transform.localScale = new Vector3(0.22f, 0.08f, 0.42f);
                UnityEngine.Object.DestroyImmediate(vis.GetComponent<BoxCollider>());
                vis.GetComponent<MeshRenderer>().sharedMaterial =
                    new Material(litShader) { color = new Color(0.22f, 0.55f, 0.28f) };
            }
        }

        static void SpawnBulletPackDecorations(Transform parent, Vector3 clusterOrigin)
        {
            var root = new GameObject("Props_BulletsPack");
            root.transform.SetParent(parent, false);
            root.transform.position = clusterOrigin;

            void PlaceBullet(string prefabPath, Vector3 localPos, Vector3 euler)
            {
                var go = TryInstantiatePrefabUnder(prefabPath, root.transform);
                if (go == null)
                    return;
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(euler);
                go.transform.localScale = Vector3.one * BulletPackDecorationScale;
                StripCollidersUnder(go.transform);
            }

            PlaceBullet(BulletPackPrefab010Path, new Vector3(0f, 0.04f, 0f), new Vector3(0f, 35f, 0f));
            PlaceBullet(BulletPackPrefab01Path, new Vector3(0.08f, 0.03f, 0.06f), new Vector3(72f, 10f, 15f));
            PlaceBullet(BulletPackPrefab02Path, new Vector3(-0.08f, 0.025f, 0.04f), new Vector3(15f, 80f, 5f));
            PlaceBullet(BulletPackPrefab03Path, new Vector3(0.03f, 0.028f, -0.09f), new Vector3(60f, -20f, 90f));
            PlaceBullet(BulletPackPrefab01Path, new Vector3(-0.05f, 0.032f, -0.06f), new Vector3(40f, 120f, 20f));
        }

        static Text CreateHudText(Transform parent, string name, TextAnchor align, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = align;
            return txt;
        }

        static void WireHud(GameObject player)
        {
            var canvasGo = new GameObject("HUD");
            canvasGo.transform.SetParent(player.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var crosshairGo = BuildBattleRoyaleCrosshair(canvasGo.transform, player);

            var ammoTxt = CreateHudText(
                canvasGo.transform,
                "Ammo_BR",
                TextAnchor.LowerRight,
                36,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 20f),
                new Vector2(200f, 64f));

            var healthTxt = CreateHudText(
                canvasGo.transform,
                "Health_BR",
                TextAnchor.LowerLeft,
                22,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 20f),
                new Vector2(200f, 40f));

            var hintTxt = CreateHudText(
                canvasGo.transform,
                "Hint_BR",
                TextAnchor.LowerCenter,
                14,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 6f),
                new Vector2(880f, 52f));

            var hud = player.AddComponent<PrototypeFpsHud>();
            var hSo = new SerializedObject(hud);
            hSo.FindProperty("_weapon").objectReferenceValue = player.GetComponent<OsFpsInspiredWeapon>();
            hSo.FindProperty("_health").objectReferenceValue = player.GetComponent<PrototypeFpsPlayerHealth>();
            hSo.FindProperty("_ammoText").objectReferenceValue = ammoTxt;
            hSo.FindProperty("_healthText").objectReferenceValue = healthTxt;
            hSo.FindProperty("_hintText").objectReferenceValue = hintTxt;
            hSo.FindProperty("_crosshairRoot").objectReferenceValue = crosshairGo;
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

        static void EnsureProjectTagsExist(params string[] tags)
        {
            var so = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = so.FindProperty("tags");
            if (tagsProp == null || !tagsProp.isArray)
                return;

            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag))
                    continue;
                var found = false;
                for (var i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }

            so.ApplyModifiedProperties();
        }

        static void AddToBuildSettings(string scenePath)
        {
            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[VR Project] Build Settings: no GUID for \"" + scenePath +
                                 "\" yet. Re-run menu after Unity imports the scene.");
                return;
            }

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
