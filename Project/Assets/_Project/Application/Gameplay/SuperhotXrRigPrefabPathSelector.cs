using System;
using System.Collections.Generic;

namespace VRProject.Application.Gameplay
{
    /// <summary>
    /// Picks the VR Starter Assets XR rig prefab path among Unity asset search results.
    /// </summary>
    public static class SuperhotXrRigPrefabPathSelector
    {
        public const string ExpectedPrefabFileName = "XR Origin (XR Rig).prefab";

        public static string SelectPreferredPath(IReadOnlyList<string> candidateAssetPaths)
        {
            if (candidateAssetPaths == null || candidateAssetPaths.Count == 0)
                return null;

            var fromStarter = Pick(candidateAssetPaths, p =>
                p.IndexOf("/Starter Assets/", StringComparison.OrdinalIgnoreCase) >= 0);
            if (fromStarter != null)
                return fromStarter;

            var anyVr = Pick(candidateAssetPaths, _ => true);
            return anyVr;
        }

        static string Pick(IReadOnlyList<string> candidateAssetPaths, Func<string, bool> extraFilter)
        {
            for (var i = 0; i < candidateAssetPaths.Count; i++)
            {
                var raw = candidateAssetPaths[i];
                var n = NormalizeSlashes(raw);
                if (n == null || !n.EndsWith(ExpectedPrefabFileName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (IsExcludedPath(n))
                    continue;
                if (!extraFilter(n))
                    continue;
                return raw;
            }

            return null;
        }

        internal static string NormalizeSlashes(string path) =>
            path?.Replace('\\', '/');

        internal static bool IsExcludedPath(string normalizedPath) =>
            normalizedPath != null &&
            normalizedPath.IndexOf("/AR Starter Assets/", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
