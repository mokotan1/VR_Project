using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VRProject.EditorTools
{
    /// <summary>
    /// URP 카메라 스택: 메인 카메라는 월드만, 자식 오버레이는 <see cref="WeaponLayerName"/>만 그려
    /// 뷰모델이 벽/지형 깊이에 묻히지 않게 합니다.
    /// </summary>
    public static class FpsViewmodelRenderingSetup
    {
        public const string WeaponLayerName = "Weapon";

        public static void Apply(Camera mainCamera, GameObject viewmodelRoot)
        {
            if (mainCamera == null || viewmodelRoot == null)
                return;

            var weaponLayer = LayerMask.NameToLayer(WeaponLayerName);
            if (weaponLayer < 0)
            {
                Debug.LogWarning("[VR Project] Layer '" + WeaponLayerName +
                                 "' is missing (Edit → Project Settings → Tags and Layers). Weapon overlay not applied.");
                return;
            }

            SetLayerRecursively(viewmodelRoot, weaponLayer);

            var maskWeapon = 1 << weaponLayer;
            mainCamera.cullingMask &= ~maskWeapon;

            var overlayTransform = mainCamera.transform.Find("WeaponOverlayCamera");
            Camera overlayCam;
            if (overlayTransform != null)
                overlayCam = overlayTransform.GetComponent<Camera>();
            else
            {
                var go = new GameObject("WeaponOverlayCamera");
                go.transform.SetParent(mainCamera.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                overlayCam = go.AddComponent<Camera>();
            }

            overlayCam.enabled = true;
            overlayCam.clearFlags = CameraClearFlags.Nothing;
            overlayCam.cullingMask = maskWeapon;
            overlayCam.nearClipPlane = 0.01f;
            overlayCam.farClipPlane = 10f;
            overlayCam.fieldOfView = mainCamera.fieldOfView;
            overlayCam.allowHDR = mainCamera.allowHDR;
            overlayCam.allowMSAA = mainCamera.allowMSAA;
            overlayCam.forceIntoRenderTexture = mainCamera.forceIntoRenderTexture;

            var baseData = mainCamera.GetUniversalAdditionalCameraData();
            var overlayData = overlayCam.GetUniversalAdditionalCameraData();
            if (baseData == null || overlayData == null)
            {
                Debug.LogWarning("[VR Project] Universal Additional Camera Data missing. Restore viewmodel on main camera.");
                mainCamera.cullingMask |= maskWeapon;
                return;
            }

            baseData.renderType = CameraRenderType.Base;
            overlayData.renderType = CameraRenderType.Overlay;

            var stack = baseData.cameraStack;
            if (stack == null)
            {
                Debug.LogWarning(
                    "[VR Project] Camera stack not supported on this URP renderer. Viewmodel may clip; check Renderer → Camera Stacking.");
                mainCamera.cullingMask |= maskWeapon;
                return;
            }

            if (!stack.Contains(overlayCam))
                stack.Add(overlayCam);

            var syncType = ResolveWeaponOverlaySyncType();
            if (syncType == null)
            {
                Debug.LogWarning(
                    "[VR Project] FpsWeaponOverlayCameraSync 타입을 로드할 수 없습니다. VRProject.Presentation 어셈블리를 확인하세요.");
                return;
            }

            var syncComp = overlayCam.GetComponent(syncType);
            if (syncComp == null)
                syncComp = overlayCam.gameObject.AddComponent(syncType);
            var syncSo = new SerializedObject(syncComp);
            var mainProp = syncSo.FindProperty("_mainCamera");
            if (mainProp != null)
                mainProp.objectReferenceValue = mainCamera;
            syncSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        static Type ResolveWeaponOverlaySyncType()
        {
            const string qualified =
                "VRProject.Presentation.PrototypeFps.FpsWeaponOverlayCameraSync, VRProject.Presentation";
            var t = Type.GetType(qualified);
            if (t != null)
                return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name != "VRProject.Presentation")
                    continue;
                return asm.GetType("VRProject.Presentation.PrototypeFps.FpsWeaponOverlayCameraSync");
            }

            return null;
        }
    }
}
