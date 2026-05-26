namespace VRProject.Application.Startup
{
    /// <summary>
    /// Discrete play modes the user can select at startup. <see cref="None"/> means no decision yet.
    /// </summary>
    public enum PlayModeKind
    {
        None,
        Mobile,
        Vr
    }

    /// <summary>
    /// Immutable snapshot of which play modes are usable on the current device.
    /// </summary>
    public readonly struct PlayModeAvailability
    {
        public PlayModeAvailability(bool mobileAvailable, bool vrAvailable)
        {
            MobileAvailable = mobileAvailable;
            VrAvailable = vrAvailable;
        }

        public bool MobileAvailable { get; }
        public bool VrAvailable { get; }
    }

    /// <summary>
    /// Pure decision helpers that decide whether a play mode is selectable,
    /// pick a safe fallback, and resolve a user-requested mode against the
    /// current device availability. This type is deliberately free of
    /// UnityEngine references so it remains fully unit-testable.
    /// </summary>
    public static class PlayModeSelection
    {
        /// <summary>Returns true when the requested mode is currently usable.</summary>
        public static bool CanSelect(PlayModeKind mode, PlayModeAvailability availability)
        {
            switch (mode)
            {
                case PlayModeKind.Mobile:
                    return availability.MobileAvailable;
                case PlayModeKind.Vr:
                    return availability.VrAvailable;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Picks a safe fallback when no explicit selection has been made:
        /// prefer VR if a headset is present, otherwise mobile/flat play,
        /// otherwise <see cref="PlayModeKind.None"/>.
        /// </summary>
        public static PlayModeKind ChooseFallback(PlayModeAvailability availability)
        {
            if (availability.VrAvailable)
                return PlayModeKind.Vr;
            if (availability.MobileAvailable)
                return PlayModeKind.Mobile;
            return PlayModeKind.None;
        }

        /// <summary>
        /// Returns the requested mode when it is selectable; otherwise returns
        /// the fallback so callers never end up with an unusable selection.
        /// </summary>
        public static PlayModeKind ResolveSelectedMode(PlayModeKind requested, PlayModeAvailability availability)
        {
            return CanSelect(requested, availability)
                ? requested
                : ChooseFallback(availability);
        }
    }
}
