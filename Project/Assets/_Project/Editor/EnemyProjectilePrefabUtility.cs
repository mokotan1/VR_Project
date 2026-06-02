#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    public static class EnemyProjectilePrefabUtility
    {
        public const string PrefabPath = "Assets/_Project/Presentation/Gameplay/Prefabs/SuperhotProjectile.prefab";
        public const string MaterialPath = "Assets/_Project/Presentation/Gameplay/Prefabs/SuperhotProjectile.mat";

        static readonly Color ProjectileColor = new Color(0.9f, 0.85f, 0.2f);

        public static SuperhotProjectile EnsurePrefab()
        {
            var material = EnsureMaterialAsset();
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                RepairPrefabMaterial(existing, material);
                return existing.GetComponent<SuperhotProjectile>();
            }

            var projGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projGo.name = "SuperhotProjectile";
            projGo.transform.localScale = Vector3.one * 0.12f;
            UnityEngine.Object.DestroyImmediate(projGo.GetComponent<Collider>());
            var sphere = projGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            var rb = projGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            projGo.AddComponent<SuperhotProjectile>();
            projGo.GetComponent<MeshRenderer>().sharedMaterial = material;

            var prefab = PrefabUtility.SaveAsPrefabAsset(projGo, PrefabPath);
            UnityEngine.Object.DestroyImmediate(projGo);
            AssetDatabase.SaveAssets();
            return prefab.GetComponent<SuperhotProjectile>();
        }

        static Material EnsureMaterialAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                ApplyMaterialColor(existing, ProjectileColor);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var dir = Path.GetDirectoryName(MaterialPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var shader = ResolveLitShader();
            if (shader == null)
                throw new InvalidOperationException("[VR Project] No compatible Lit shader found for SuperhotProjectile material.");

            var material = new Material(shader);
            ApplyMaterialColor(material, ProjectileColor);
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        static void RepairPrefabMaterial(GameObject prefabRoot, Material material)
        {
            if (prefabRoot == null || material == null)
                return;

            var renderer = prefabRoot.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            if (renderer.sharedMaterial == material)
                return;

            if (!NeedsMaterialRepair(renderer.sharedMaterial))
                return;

            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(prefabRoot);
            PrefabUtility.SavePrefabAsset(prefabRoot);
            AssetDatabase.SaveAssets();
        }

        static bool NeedsMaterialRepair(Material material)
        {
            if (material == null)
                return true;

            if (material.shader == null)
                return true;

            return material.shader.name == "Hidden/InternalErrorShader";
        }

        static Shader ResolveLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
        }

        static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            material.color = color;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
#endif
