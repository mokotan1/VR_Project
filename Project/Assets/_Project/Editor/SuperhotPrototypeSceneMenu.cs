#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Presentation.Common.Managers;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    public static class SuperhotPrototypeSceneMenu
    {
        const string ScenePath = "Assets/Scenes/SuperhotPrototype.unity";
        const string ProjectilePrefabPath = "Assets/_Project/Presentation/Gameplay/Prefabs/SuperhotProjectile.prefab";

        [MenuItem("VR Project/Scenes/Create Superhot Prototype Scene")]
        public static void CreateSuperhotPrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(6f, 1f, 6f);

            var systems = new GameObject("Systems");
            systems.AddComponent<GameBootstrapper>();
            systems.AddComponent<XRInteractionManager>();
            systems.AddComponent<SuperhotLocomotionDisabler>();
            systems.AddComponent<SuperhotGameplayDriver>();

            var flowGo = new GameObject("NodeFlow");
            var flow = flowGo.AddComponent<SuperhotNodeFlow>();

            var zonesParent = new GameObject("CombatZones");
            var zoneA = BuildZone("Zone_A", zonesParent.transform, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.25f, 3f), flow, isFirst: true);
            var zoneB = BuildZone("Zone_B", zonesParent.transform, new Vector3(0f, 0f, 14f), new Vector3(0f, 0.25f, 3f), flow, isFirst: false);

            var entryPoseB = new GameObject("EntryCameraPose");
            entryPoseB.transform.SetParent(zoneB.transform, false);
            entryPoseB.transform.localPosition = new Vector3(0f, 1.6f, -2f);
            entryPoseB.transform.localRotation = Quaternion.identity;

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("_zonesInOrder").arraySize = 2;
            flowSo.FindProperty("_zonesInOrder").GetArrayElementAtIndex(0).objectReferenceValue = zoneA;
            flowSo.FindProperty("_zonesInOrder").GetArrayElementAtIndex(1).objectReferenceValue = zoneB;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            var projectilePrefab = EnsureProjectilePrefab();
            AssignProjectileToZones(zoneA, zoneB, projectilePrefab);

            WirePortalDestination(zoneA, entryPoseB.transform);
            WirePortalDestinationFromLocal(zoneB, zoneB.transform, new Vector3(0f, 1.6f, 0f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log(
                $"[VR Project] Saved {ScenePath}. Add XR Origin (XR Rig) from Package Manager → XR Interaction Toolkit → Starter Assets sample, then press Play. Locomotion providers on the rig are disabled at runtime by SuperhotLocomotionDisabler.");
        }

        static SuperhotCombatZone BuildZone(
            string name,
            Transform parent,
            Vector3 rootWorldPosition,
            Vector3 enemyLocalBase,
            SuperhotNodeFlow flow,
            bool isFirst)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = rootWorldPosition;
            if (!isFirst)
                root.SetActive(false);

            var zone = root.AddComponent<SuperhotCombatZone>();

            var exitRoot = new GameObject("ExitPyramid");
            exitRoot.transform.SetParent(root.transform, false);
            exitRoot.transform.localPosition = new Vector3(1.2f, 1f, 0f);
            exitRoot.SetActive(false);

            var pyramid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pyramid.name = "PyramidMesh";
            pyramid.transform.SetParent(exitRoot.transform, false);
            pyramid.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            pyramid.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var rb = pyramid.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            pyramid.AddComponent<XRGrabInteractable>();

            var portal = pyramid.AddComponent<SuperhotGrabExitPortal>();
            var portalSo = new SerializedObject(portal);
            portalSo.FindProperty("_owningZone").objectReferenceValue = zone;
            portalSo.FindProperty("_nodeFlow").objectReferenceValue = flow;
            portalSo.ApplyModifiedPropertiesWithoutUndo();

            var zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("_exitInteractableRoot").objectReferenceValue = exitRoot;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < 2; i++)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = $"Enemy_{i}";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = enemyLocalBase + new Vector3(i * 1.2f - 0.6f, 0.75f, 0f);
                cap.AddComponent<SuperhotEnemy>();
                cap.AddComponent<SuperhotEnemyMover>();
                cap.AddComponent<SuperhotEnemyShooter>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.85f, 0.15f, 0.12f);
                cap.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            return zone;
        }

        static void WirePortalDestination(SuperhotCombatZone fromZone, Transform cameraDestination)
        {
            var exitRoot = fromZone.transform.Find("ExitPyramid");
            var pyramid = exitRoot.GetChild(0);
            var portal = pyramid.GetComponent<SuperhotGrabExitPortal>();

            var so = new SerializedObject(portal);
            so.FindProperty("_cameraWorldDestination").objectReferenceValue = cameraDestination;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WirePortalDestinationFromLocal(SuperhotCombatZone fromZone, Transform parent, Vector3 localPosition)
        {
            var exitRoot = fromZone.transform.Find("ExitPyramid");
            var pyramid = exitRoot.GetChild(0);
            var portal = pyramid.GetComponent<SuperhotGrabExitPortal>();

            var destGo = new GameObject("EndCameraPose");
            destGo.transform.SetParent(parent, false);
            destGo.transform.localPosition = localPosition;
            destGo.transform.localRotation = Quaternion.identity;

            var so = new SerializedObject(portal);
            so.FindProperty("_cameraWorldDestination").objectReferenceValue = destGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SuperhotProjectile EnsureProjectilePrefab()
        {
            var dir = Path.GetDirectoryName(ProjectilePrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var existing = AssetDatabase.LoadAssetAtPath<SuperhotProjectile>(ProjectilePrefabPath);
            if (existing != null)
                return existing;

            var projGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projGo.name = "SuperhotProjectile";
            projGo.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(projGo.GetComponent<Collider>());
            var sphere = projGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            var rb = projGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            projGo.AddComponent<SuperhotProjectile>();
            var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pMat.color = new Color(0.9f, 0.85f, 0.2f);
            projGo.GetComponent<MeshRenderer>().sharedMaterial = pMat;

            var prefab = PrefabUtility.SaveAsPrefabAsset(projGo, ProjectilePrefabPath);
            Object.DestroyImmediate(projGo);
            return prefab.GetComponent<SuperhotProjectile>();
        }

        static void AssignProjectileToZones(SuperhotCombatZone a, SuperhotCombatZone b, SuperhotProjectile prefab)
        {
            foreach (var shooter in a.GetComponentsInChildren<SuperhotEnemyShooter>(true))
            {
                var so = new SerializedObject(shooter);
                so.FindProperty("_projectilePrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var shooter in b.GetComponentsInChildren<SuperhotEnemyShooter>(true))
            {
                var so = new SerializedObject(shooter);
                so.FindProperty("_projectilePrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
