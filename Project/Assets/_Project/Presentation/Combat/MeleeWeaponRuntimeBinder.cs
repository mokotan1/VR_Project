using UnityEngine;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Wires camera references for flat/mobile motion sources at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponRuntimeBinder : MonoBehaviour
    {
        void Start()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            foreach (var flat in GetComponentsInChildren<FlatMouseWeaponMotionSource>(true))
                flat.BindCamera(camera.transform);

            foreach (var mobile in GetComponentsInChildren<MobileTouchWeaponMotionSource>(true))
                mobile.BindCamera(camera.transform);
        }
    }
}
