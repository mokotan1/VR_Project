using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// URP 오버레이 총 카메라가 메인과 FOV·뷰포트를 매 프레임 맞춰 분할/조준 시 깨짐을 줄입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FpsWeaponOverlayCameraSync : MonoBehaviour
    {
        [SerializeField] Camera _mainCamera;
        Camera _overlay;

        void Awake()
        {
            _overlay = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (_mainCamera == null || _overlay == null)
                return;

            _overlay.fieldOfView = _mainCamera.fieldOfView;
            _overlay.rect = _mainCamera.rect;
            _overlay.pixelRect = _mainCamera.pixelRect;
        }
    }
}
