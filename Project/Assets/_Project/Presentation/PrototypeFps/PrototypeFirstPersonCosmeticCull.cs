using UnityEngine;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// 1인칭에서 머리 주변 장식 메시만 끄면 시야 클리핑이 줄어듭니다(통짜 body 스킨 메시는 유지).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeFirstPersonCosmeticCull : MonoBehaviour
    {
        static readonly string[] NameSubstringsLower =
        {
            "hair_front",
            "hair_frontside",
            "hairband",
            "hair_acc",
            "head_back",
        };

        void Awake()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                var n = r.gameObject.name.ToLowerInvariant();
                foreach (var sub in NameSubstringsLower)
                {
                    if (!n.Contains(sub))
                        continue;
                    r.enabled = false;
                    break;
                }
            }
        }
    }
}
