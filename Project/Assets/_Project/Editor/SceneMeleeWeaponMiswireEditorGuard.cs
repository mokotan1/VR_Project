#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRProject.Presentation.Combat;
using VRProject.Presentation.Gameplay;

namespace VRProject.EditorTools
{
    /// <summary>
    /// Strips HK416 wire miswires (NavWorld grab, player dynamic RB) before entering Play Mode
    /// so XRGrabInteractable OnEnable does not register child colliders twice.
    /// </summary>
    [InitializeOnLoad]
    static class SceneMeleeWeaponMiswireEditorGuard
    {
        static SceneMeleeWeaponMiswireEditorGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var repaired = SceneMeleeWeaponSetup.RepairMiswiredMeleeStacks(RestorePlaytestPlayerPhysicsIfNeeded);
            if (repaired <= 0)
                return;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[VR Project] Auto-repaired {repaired} miswired HK416 melee stack issue(s) before Play Mode.");
        }

        static void RestorePlaytestPlayerPhysicsIfNeeded(GameObject root)
        {
            if (root == null)
                return;

            if (root.name == "UnityChan_Player" ||
                root.GetComponent<PlaytestPlayerContactVolume>() != null ||
                root.GetComponent<Unity.XR.CoreUtils.XROrigin>() != null)
            {
                PlaytestPlayerContactVolume.Ensure(root);
            }
        }
    }
}
#endif
