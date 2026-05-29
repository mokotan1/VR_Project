namespace VRProject.Presentation.Common.UI
{
    public struct MobileTouchInputSnapshot
    {
        public bool IsActive;
        public float MoveAxisX;
        public float MoveAxisY;
        public float LookDeltaX;
        public float LookDeltaY;
        public bool FirePressedThisFrame;
        public bool ReloadPressedThisFrame;
        public bool ThrowPressedThisFrame;
        public bool PausePressedThisFrame;
        public bool MeleeSwingActive;
        public float MeleeSwingDeltaX;
        public float MeleeSwingDeltaY;
        public bool FireHeld;

        public static MobileTouchInputSnapshot Inactive => default;
    }
}
