namespace VRProject.Application.Mobile
{
    public static class MobileTouchControlsPolicy
    {
        public static bool ShouldUseMobileControls(bool explicitMobilePlaySelected, bool touchscreenPresent)
        {
            return touchscreenPresent || explicitMobilePlaySelected;
        }
    }
}
