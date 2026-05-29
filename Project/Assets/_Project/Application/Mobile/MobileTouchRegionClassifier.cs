namespace VRProject.Application.Mobile
{
    public static class MobileTouchRegionClassifier
    {
        public static MobileTouchRegionKind Classify(float normalizedX, float normalizedY, MobileTouchLayoutRects layout)
        {
            if (Contains(normalizedX, normalizedY, layout.FireMinX, layout.FireMinY, layout.FireMaxX, layout.FireMaxY))
                return MobileTouchRegionKind.FireButton;
            if (Contains(normalizedX, normalizedY, layout.ReloadMinX, layout.ReloadMinY, layout.ReloadMaxX, layout.ReloadMaxY))
                return MobileTouchRegionKind.ReloadButton;
            if (Contains(normalizedX, normalizedY, layout.ThrowMinX, layout.ThrowMinY, layout.ThrowMaxX, layout.ThrowMaxY))
                return MobileTouchRegionKind.ThrowButton;
            if (Contains(normalizedX, normalizedY, layout.PauseMinX, layout.PauseMinY, layout.PauseMaxX, layout.PauseMaxY))
                return MobileTouchRegionKind.PauseButton;
            if (Contains(normalizedX, normalizedY, layout.MoveJoystickMinX, layout.MoveJoystickMinY, layout.MoveJoystickMaxX, layout.MoveJoystickMaxY))
                return MobileTouchRegionKind.MoveJoystick;
            if (Contains(normalizedX, normalizedY, layout.MeleeMinX, layout.MeleeMinY, layout.MeleeMaxX, layout.MeleeMaxY))
                return MobileTouchRegionKind.MeleeSwing;
            if (Contains(normalizedX, normalizedY, layout.LookMinX, layout.LookMinY, layout.LookMaxX, layout.LookMaxY))
                return MobileTouchRegionKind.Look;

            return MobileTouchRegionKind.None;
        }

        static bool Contains(float x, float y, float minX, float minY, float maxX, float maxY)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }
    }
}
