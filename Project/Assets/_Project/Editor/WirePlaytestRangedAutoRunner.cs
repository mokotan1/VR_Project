#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Drop <c>Assets/_Project/Editor/Requests/PendingWirePlaytestRanged.txt</c> to wire all scene enemies once after script reload.
    /// </summary>
    [InitializeOnLoad]
    static class WirePlaytestRangedAutoRunner
    {
        const string RequestPath = "Assets/_Project/Editor/Requests/PendingWirePlaytestRanged.txt";

        static WirePlaytestRangedAutoRunner()
        {
            EditorApplication.delayCall += TryRunPendingRequest;
        }

        static void TryRunPendingRequest()
        {
            if (!File.Exists(RequestPath))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRunPendingRequest;
                return;
            }

            try
            {
                MeleeCombatSceneMenu.WirePlaytestRangedOnAllEnemiesInUnityChanScene();
                Debug.Log("[VR Project] Auto-ran playtest ranged wiring from pending request.");
            }
            finally
            {
                if (File.Exists(RequestPath))
                    File.Delete(RequestPath);

                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
