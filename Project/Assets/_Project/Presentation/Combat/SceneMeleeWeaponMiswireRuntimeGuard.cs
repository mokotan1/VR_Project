using UnityEngine;

namespace VRProject.Presentation.Combat
{
    /// <summary>
    /// Play Mode 진입 시 NavWorld HK416 오배선·깨진 Transform을 정리해
    /// XRI collider 중복 등록과 Invalid AABB를 방지합니다.
    /// </summary>
    public static class SceneMeleeWeaponMiswireRuntimeGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RepairAfterSceneLoad()
        {
            var repaired = SceneMeleeWeaponSetup.RepairMiswiredMeleeStacks();
            if (repaired > 0)
                Debug.Log($"[VR Project] Runtime miswire repair: {repaired} object(s).");
        }
    }
}
