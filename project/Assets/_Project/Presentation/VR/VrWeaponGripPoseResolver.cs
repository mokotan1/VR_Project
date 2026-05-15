using UnityEngine;

namespace VRProject.Presentation.VR
{
    public static class VrWeaponGripPoseResolver
    {
        public static Transform FindByName(Transform root, params string[] preferredNames)
        {
            if (root == null || preferredNames == null || preferredNames.Length == 0)
                return null;

            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var preferredName in preferredNames)
                {
                    if (candidate.name == preferredName)
                        return candidate;
                }
            }

            return null;
        }
    }
}
