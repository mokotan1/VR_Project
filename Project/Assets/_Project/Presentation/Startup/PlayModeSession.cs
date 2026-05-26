using UnityEngine;
using VRProject.Application.Startup;

namespace VRProject.Presentation.Startup
{
    /// <summary>
    /// DontDestroyOnLoad singleton that carries the user's selected play
    /// mode from the startup scene into gameplay scenes. A single instance
    /// is enforced so rig selectors can rely on
    /// <see cref="GetSelectedModeOrFallback"/> without ambiguity.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class PlayModeSession : MonoBehaviour
    {
        static PlayModeSession s_instance;

        [SerializeField] PlayModeKind _selectedMode = PlayModeKind.None;

        public static PlayModeSession Instance => s_instance;
        public PlayModeKind SelectedMode => _selectedMode;
        public bool HasSelection => _selectedMode != PlayModeKind.None;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        public void SetSelectedMode(PlayModeKind mode)
        {
            _selectedMode = mode;
        }

        /// <summary>
        /// Returns the selected mode resolved against availability when one
        /// exists, otherwise a safe fallback. Gameplay scenes should call
        /// this rather than reading <see cref="SelectedMode"/> directly so
        /// direct-scene playtests still get a sensible rig.
        /// </summary>
        public static PlayModeKind GetSelectedModeOrFallback(PlayModeAvailability availability)
        {
            if (s_instance != null && s_instance.HasSelection)
                return PlayModeSelection.ResolveSelectedMode(s_instance.SelectedMode, availability);

            return PlayModeSelection.ChooseFallback(availability);
        }
    }
}
